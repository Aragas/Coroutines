using Coroutines;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Example
{
    // Here's a silly example animating a little roguelike-style character moving.
    public static class Program
    {
        public static void Main(string[] args)
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
            IEnumerator<object?> MoveX(int amount, float stepTime)
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
            IEnumerator<object?> MoveY(int amount, float stepTime)
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
            IEnumerator<object?> Movement()
            {
                //Walk normally
                yield return MoveX(5, 0.25f);
                yield return MoveY(5, 0.25f);

                //Walk slowly
                yield return MoveX(2, 0.5f);
                yield return MoveY(2, 0.5f);
                yield return MoveX(-2, 0.5f);
                yield return MoveY(-2, 0.5f);

                //Run fast
                yield return MoveX(5, 0.1f);
                yield return MoveY(5, 0.1f);
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
            var runner = new CoroutineRunner();
            var moving = runner.Run(Movement());

            //Run the update loop until we've finished moving
            while (moving.IsRunning)
            {
                //Track time
                var currTime = watch.ElapsedMilliseconds / 1000f;
                accumulator += currTime - prevTime;
                prevTime = currTime;

                //Update at our requested rate (10 fps)
                if (accumulator > updateRate)
                {
                    accumulator -= updateRate;
                    runner.Update(updateRate);
                    DrawMap();
                }
            }
        }
    }
}