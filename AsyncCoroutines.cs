/*

MIT License

Copyright (c) 2017 Chevy Ray Johnston

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Coroutines
{
    /// <summary>
    /// A container for running multiple routines in parallel. Coroutines can be nested.
    /// </summary>
    public class AsyncCoroutineRunner
    {
        private readonly List<IAsyncEnumerator<object?>> _running = new();
        private readonly List<float> _delays = new();

        /// <summary>
        /// How many coroutines are currently running.
        /// </summary>
        public int Count => _running.Count;

        /// <summary>
        /// Run a coroutine.
        /// </summary>
        /// <returns>A handle to the new coroutine.</returns>
        /// <param name="delay">How many seconds to delay before starting.</param>
        /// <param name="routine">The routine to run.</param>
        public ValueTask<AsyncCoroutineHandle> RunAsync(float delay, IAsyncEnumerator<object?> routine)
        {
            _running.Add(routine);
            _delays.Add(delay);
            return ValueTask.FromResult(new AsyncCoroutineHandle(this, routine));
        }

        /// <summary>
        /// Run a coroutine.
        /// </summary>
        /// <returns>A handle to the new coroutine.</returns>
        /// <param name="routine">The routine to run.</param>
        public ValueTask<AsyncCoroutineHandle> RunAsync(IAsyncEnumerator<object?> routine) => RunAsync(0f, routine);

        /// <summary>
        /// Stop the specified routine.
        /// </summary>
        /// <returns>True if the routine was actually stopped.</returns>
        /// <param name="routine">The routine to stop.</param>
        public ValueTask<bool> StopAsync(IAsyncEnumerator<object?> routine)
        {
            var i = _running.IndexOf(routine);
            if (i < 0)
                return ValueTask.FromResult(false);
            _running[i] = AsyncEnumerable.Empty<object?>().GetAsyncEnumerator();
            _delays[i] = 0f;
            return ValueTask.FromResult(true);
        }

        /// <summary>
        /// Stop the specified routine.
        /// </summary>
        /// <returns>True if the routine was actually stopped.</returns>
        /// <param name="routine">The routine to stop.</param>
        public ValueTask<bool> StopAsync(in AsyncCoroutineHandle routine) => routine.StopAsync();

        /// <summary>
        /// Stop all running routines.
        /// </summary>
        public ValueTask StopAllAsync()
        {
            _running.Clear();
            _delays.Clear();

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Check if the routine is currently running.
        /// </summary>
        /// <returns>True if the routine is running.</returns>
        /// <param name="routine">The routine to check.</param>
        public ValueTask<bool> IsRunningAsync(IAsyncEnumerator<object?> routine) => ValueTask.FromResult(_running.Contains(routine));

        /// <summary>
        /// Check if the routine is currently running.
        /// </summary>
        /// <returns>True if the routine is running.</returns>
        /// <param name="routine">The routine to check.</param>
        public ValueTask<bool> IsRunningAsync(in AsyncCoroutineHandle routine) => routine.IsRunningAsync;

        /// <summary>
        /// Update all running coroutines.
        /// </summary>
        /// <returns>True if any routines were updated.</returns>
        /// <param name="deltaTime">How many seconds have passed sinced the last update.</param>
        public async ValueTask<bool> UpdateAsync(float deltaTime)
        {
            if (_running.Count > 0)
            {
                for (var i = 0; i < _running.Count; i++)
                {
                    if (_delays[i] > 0f)
                        _delays[i] -= deltaTime;
                    else if (_running[i] == null || !await MoveNextAsync(_running[i], i))
                    {
                        _running.RemoveAt(i);
                        _delays.RemoveAt(i--);
                    }
                }
                return true;
            }
            return false;
        }

        private async ValueTask<bool> MoveNextAsync(IAsyncEnumerator<object?> routine, int index)
        {
            if (routine.Current is IAsyncEnumerator<object?> current)
            {
                if (await MoveNextAsync(current, index))
                    return true;

                _delays[index] = 0f;
            }

            var result = await routine.MoveNextAsync();

            if (routine.Current is float routineCurrent)
                _delays[index] = routineCurrent;

            return result;
        }
    }

    /// <summary>
    /// A handle to a (potentially running) coroutine.
    /// </summary>
    public readonly struct AsyncCoroutineHandle
    {
        /// <summary>
        /// Reference to the routine's runner.
        /// </summary>
        public readonly AsyncCoroutineRunner Runner;

        /// <summary>
        /// Reference to the routine's enumerator.
        /// </summary>
        public readonly IAsyncEnumerator<object?> Enumerator;

        /// <summary>
        /// True if the enumerator is currently running.
        /// </summary>
        public ValueTask<bool> IsRunningAsync => Runner.IsRunningAsync(Enumerator);

        /// <summary>
        /// Construct a coroutine. Never call this manually, only use return values from Coroutines.RunAsync().
        /// </summary>
        /// <param name="runner">The routine's runner.</param>
        /// <param name="enumerator">The routine's enumerator.</param>
        internal AsyncCoroutineHandle(AsyncCoroutineRunner runner, IAsyncEnumerator<object?> enumerator)
        {
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));
            Enumerator = enumerator ?? throw new ArgumentNullException(nameof(runner));
        }

        /// <summary>
        /// Stop this coroutine if it is running.
        /// </summary>
        /// <returns>True if the coroutine was stopped.</returns>
        public async ValueTask<bool> StopAsync() => await Runner.IsRunningAsync(Enumerator) && await Runner.StopAsync(Enumerator);

        /// <summary>
        /// A routine to wait until this coroutine has finished running.
        /// </summary>
        /// <returns>The wait enumerator.</returns>
        public async IAsyncEnumerator<object?> WaitAsync()
        {
            while (await Runner.IsRunningAsync(Enumerator))
                yield return null;
        }
    }
}