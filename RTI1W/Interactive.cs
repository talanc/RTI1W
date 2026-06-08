using Raylib_cs;

namespace RTI1W;

public static class Interactive
{
    public static void RunInteractive(int imageWidth, int imageHeight, Camera camera, Hittable world, int maxDepth, int numThreads, int samplesPerPixel)
    {
        var texturePixels = new int[imageWidth * imageHeight];

        var cameraPos = camera.LookFrom;
        var forward = UnitVector(camera.LookAt - camera.LookFrom);
        var yaw = Atan2(forward.Z, forward.X);
        var pitch = Asin(forward.Y);

        const int targetTileCount = 64;
        var tilesX = Math.Max(1, (int)Ceiling(Sqrt(targetTileCount * ((float)imageWidth / imageHeight))));
        var tilesY = Math.Max(1, (int)Ceiling((float)targetTileCount / tilesX));
        var tileWidth = Math.Max(1, (imageWidth + tilesX - 1) / tilesX);
        var tileHeight = Math.Max(1, (imageHeight + tilesY - 1) / tilesY);
        var tileCount = tilesX * tilesY;

        var workItems = new (int sample, int tile)[tileCount];
        for (var i = 0; i < workItems.Length; i++)
        {
            workItems[i] = (sample: 1, tile: i);
        }

        var workerStartSignal = new ManualResetEventSlim(false);
        var workerGeneration = 0;
        var workerCompletedItems = 0;
        var workerNextItem = 0;

        var workerScratch = new Vector3[texturePixels.Length];

        var workerSample = 1;

        void UpdateDirectionAndCamera()
        {
            var dir = UnitVector(V3(
                x: Cos(pitch) * Cos(yaw),
                y: Sin(pitch),
                z: Cos(pitch) * Sin(yaw)
            ));

            camera.SetPosition(cameraPos, cameraPos + dir, camera.VUp, camera.VFov, camera.AspectRatio, camera.Aperture, camera.FocusDist);
        }

        void Worker()
        {
        start:
            if (Volatile.Read(ref workerGeneration) == -1) return;
            workerStartSignal.Wait();

            var generation = Volatile.Read(ref workerGeneration);
            if (generation == -1) return;

        tile:
            if (generation != Volatile.Read(ref workerGeneration))
            {
                goto start;
            }

            var workItemIndex = Interlocked.Increment(ref workerNextItem) - 1;
            if (workItemIndex >= workItems.Length)
            {
                if (generation == Volatile.Read(ref workerGeneration)) workerStartSignal.Reset();
                goto start;
            }

            var (sample, tile) = workItems[workItemIndex];

            var (tileY, tileX) = Math.DivRem(tile, tilesX);

            var widthStart = tileX * tileWidth;
            var widthEnd = Math.Min(imageWidth, widthStart + tileWidth);
            var widthStep = 1;

            var heightStart = tileY * tileHeight;
            var heightEnd = Math.Min(imageHeight, heightStart + tileHeight);
            var heightStep = 1;

            var sInv = 1f / sample;

            for (var j = heightStart; j < heightEnd; j += heightStep)
            {
                var pixY = imageHeight - 1 - j;
                var dataY = j;

                for (var i = widthStart; i < widthEnd; i += widthStep)
                {
                    var pixX = i;
                    var dataX = i;

                    var u = (pixX + RandomValue()) / (imageWidth - 1);
                    var v = (pixY + RandomValue()) / (imageHeight - 1);

                    var pixelIndex = RayTracer.GetIndex(imageWidth, dataX, dataY);
                    var pixelColor = sample == 1
                        ? workerScratch[pixelIndex] = ColorBlack
                        : workerScratch[pixelIndex];

                    var ray = camera.GetRay(u, v);
                    var color = RayTracer.RayColor(world, ray, maxDepth);
                    pixelColor += color;

                    // update calculated color on texture
                    RayTracer.GetPixel(pixelColor, sInv, out var r, out var g, out var b);
                    texturePixels[pixelIndex] = r | (g << 8) | (b << 16) | (0xFF << 24); // R8G8B8A8

                    // update accumulated color
                    workerScratch[pixelIndex] = pixelColor;
                }
            }

            Interlocked.Increment(ref workerCompletedItems);
            goto tile;
        }

        void StartRender()
        {
            for (var i = 0; i < workItems.Length; i++)
            {
                workItems[i].sample = workerSample;
            }
            Random.Shared.Shuffle(workItems);
            Interlocked.Exchange(ref workerCompletedItems, 0);
            Interlocked.Exchange(ref workerNextItem, 0);
            if (workerGeneration > 1_000_000) Volatile.Write(ref workerGeneration, 0);
            else Interlocked.Increment(ref workerGeneration);
            workerStartSignal.Set();
        }

        UpdateDirectionAndCamera();

        var workerTasks = new Task[numThreads];
        for (var i = 0; i < workerTasks.Length; i++)
        {
            workerTasks[i] = Task.Run(() => Worker());
        }

        var windowSizeWidth = 900;
        var windowSizeHeight = (int)Round(windowSizeWidth / camera.AspectRatio);
        var windowSizeIncrements = 150;

        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(windowSizeWidth, windowSizeHeight, "RTI1W - Interactive");
        Raylib.SetTargetFPS(60);

        var blankImage = Raylib.GenImageColor(imageWidth, imageHeight, Color.Black);
        var frameTexture = Raylib.LoadTextureFromImage(blankImage);
        Raylib.UnloadImage(blankImage);

        StartRender();

        var uploadTimer = Stopwatch.StartNew();
        var uploadedGeneration = -1;
        var uploadedItems = -1;

        while (!Raylib.WindowShouldClose())
        {
            var rerender = false;

            if (Raylib.IsWindowResized())
            {
                rerender = true;
            }

            var dir = UnitVector(V3(
                Cos(pitch) * Cos(yaw),
                Sin(pitch),
                Cos(pitch) * Sin(yaw)
            ));
            var right = UnitVector(Cross(dir, camera.VUp));

            var move = V3(0, 0, 0);
            if (Raylib.IsKeyDown(KeyboardKey.W)) { move += dir; }
            if (Raylib.IsKeyDown(KeyboardKey.S)) { move -= dir; }
            if (Raylib.IsKeyDown(KeyboardKey.A)) { move -= right; }
            if (Raylib.IsKeyDown(KeyboardKey.D)) { move += right; }
            if (Raylib.IsKeyDown(KeyboardKey.E)) { move -= camera.VUp; }
            if (Raylib.IsKeyDown(KeyboardKey.Q)) { move += camera.VUp; }

            if (move.LengthSquared() != 0)
            {
                var moveSpeed = 0.2f;

                cameraPos += move * moveSpeed;
                rerender = true;
            }

            var oldWindowSizeWidth = windowSizeWidth;

            if (Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract))
            {
                if (windowSizeWidth >= windowSizeIncrements * 2)
                {
                    windowSizeWidth -= windowSizeIncrements;
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd))
            {
                windowSizeWidth += windowSizeIncrements;
            }

            if (windowSizeWidth != oldWindowSizeWidth)
            {
                var oldWindowSizeHeight = windowSizeHeight;

                windowSizeHeight = (int)Round(windowSizeWidth / camera.AspectRatio);

                var windowPos = Raylib.GetWindowPosition();
                var newX = (int)windowPos.X + (oldWindowSizeWidth - windowSizeWidth) / 2;
                var newY = (int)windowPos.Y + (oldWindowSizeHeight - windowSizeHeight) / 2;
                Raylib.SetWindowPosition(newX, newY);
                Raylib.SetWindowSize(windowSizeWidth, windowSizeHeight);
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Left) || Raylib.IsMouseButtonDown(MouseButton.Right))
            {
                var mouseDelta = Raylib.GetMouseDelta();
                var mouseSensitivity = 0.004f;

                if (mouseDelta.X != 0 || mouseDelta.Y != 0)
                {
                    yaw += mouseDelta.X * mouseSensitivity;
                    pitch -= mouseDelta.Y * mouseSensitivity;
                    pitch = Math.Clamp(pitch, -1.4f, 1.4f);
                    rerender = true;
                }
            }

            if (rerender)
            {
                workerSample = 1;
                UpdateDirectionAndCamera();
                StartRender();
            }

            var currentGeneration = Volatile.Read(ref workerGeneration);
            var currentItems = Volatile.Read(ref workerCompletedItems);
            var frameDone = currentItems >= workItems.Length;
            var shouldUpload = currentGeneration != uploadedGeneration || currentItems != uploadedItems;

            if (frameDone && workerSample < samplesPerPixel)
            {
                // and redo the frame with the next sample level
                frameDone = false;
                workerSample++;
                StartRender();
            }

            if (shouldUpload && (frameDone || uploadTimer.ElapsedMilliseconds >= 100))
            {
                Raylib.UpdateTexture(frameTexture, texturePixels);
                uploadedGeneration = currentGeneration;
                uploadedItems = currentItems;
                uploadTimer.Restart();
            }

            // hmm we probably dont need to redraw if the frame is complete

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Raylib.DrawTexturePro(
                frameTexture,
                new Rectangle(0, 0, frameTexture.Width, frameTexture.Height),
                new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight()),
                Vector2.Zero,
                0,
                Color.White);

            // hmm i think strings allocate, try with utf8/sbyte* instead
            Raylib.DrawText("WASD move, Q/E up-down, mouse left-click look", 10, 10, 18, Color.Black);

            var info = "Done";
            if (!frameDone)
            {
                // hmm this may allocate every frame
                info = $"{workerSample}/{samplesPerPixel}";
            }
            var infoMeasure = Raylib.MeasureText(info, 18);
            var infoX = windowSizeWidth - infoMeasure - 10;
            Raylib.DrawText(info, infoX, 10, 18, Color.Black);

            Raylib.EndDrawing();
        }

        Volatile.Write(ref workerGeneration, -1);
        workerStartSignal.Set();
        Task.WaitAll(workerTasks);
        workerStartSignal.Dispose();

        Raylib.UnloadTexture(frameTexture);
        Raylib.CloseWindow();
    }
}
