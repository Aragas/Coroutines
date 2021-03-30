using Coroutines;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Example
{
    // Here's a silly example animating a little roguelike-style character moving.
    public static class ProgramAsync
    {
        public static async Task Main(string[] args)
        {
            //Timer variables to run the update loop at 10 fps
            var watch = Stopwatch.StartNew();
            const float updateRate = 1f / 10f;
            var prevTime = watch.ElapsedMilliseconds / 1000f;
            var accumulator = 0f;

            //The little @ character's position
            var px = 0;
            var py = 0;

            //Routine to move horizontally
            async IAsyncEnumerator<object?> MoveXAsync(int amount, float stepTime)
            {
                var dir = amount > 0 ? 1 : -1;
                while (amount != 0)
                {
                    yield return stepTime;
                    px += dir;
                    amount -= dir;
                }
            }

            //Routine to move vertically
            async IAsyncEnumerator<object?> MoveYAsync(int amount, float stepTime)
            {
                var dir = amount > 0 ? 1 : -1;
                while (amount != 0)
                {
                    yield return stepTime;
                    py += dir;
                    amount -= dir;
                }
            }

            //Walk the little @ character on a path
            async IAsyncEnumerator<object?> MovementAsync()
            {
                //Walk normally
                yield return MoveXAsync(5, 0.25f);
                yield return MoveYAsync(5, 0.25f);

                //Walk slowly
                yield return MoveXAsync(2, 0.5f);
                yield return MoveYAsync(2, 0.5f);
                yield return MoveXAsync(-2, 0.5f);
                yield return MoveYAsync(-2, 0.5f);

                //Run fast
                yield return MoveXAsync(5, 0.1f);
                yield return MoveYAsync(5, 0.1f);
            }

            //Render a little map with the @ character in the console
            void DrawMap()
            {
                Console.Clear();
                for (var y = 0; y < 16; ++y)
                {
                    for (var x = 0; x < 16; ++x)
                    {
                        if (x == px && y == py)
                            Console.Write('@');
                        else
                            Console.Write('.');
                    }
                    Console.WriteLine();
                }
            }

            //Run the coroutine
            var runner = new AsyncCoroutineRunner();
            var moving = await runner.RunAsync(MovementAsync());

            //Run the update loop until we've finished moving
            while (await moving.IsRunningAsync)
            {
                //Track time
                var currTime = watch.ElapsedMilliseconds / 1000f;
                accumulator += currTime - prevTime;
                prevTime = currTime;

                //Update at our requested rate (10 fps)
                if (accumulator > updateRate)
                {
                    accumulator -= updateRate;
                    await runner.UpdateAsync(updateRate);
                    DrawMap();
                }
            }
        }
    }
}