/*

MIT License

Copyright (c) 2017 Chevy Ray Johnston, Aragas

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

namespace Coroutines
{
    /// <summary>
    /// A container for running multiple routines in parallel. Coroutines can be nested.
    /// </summary>
    public class CoroutineRunner
    {
        private readonly List<IEnumerator<object?>> _running = new();
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
        public CoroutineHandle Run(float delay, IEnumerator<object?> routine)
        {
            _running.Add(routine);
            _delays.Add(delay);
            return new CoroutineHandle(this, routine);
        }

        /// <summary>
        /// Run a coroutine.
        /// </summary>
        /// <returns>A handle to the new coroutine.</returns>
        /// <param name="routine">The routine to run.</param>
        public CoroutineHandle Run(IEnumerator<object?> routine) => Run(0f, routine);

        /// <summary>
        /// Stop the specified routine.
        /// </summary>
        /// <returns>True if the routine was actually stopped.</returns>
        /// <param name="routine">The routine to stop.</param>
        public bool Stop(IEnumerator<object?> routine)
        {
            var i = _running.IndexOf(routine);
            if (i < 0)
                return false;
            _running[i] = Enumerable.Empty<object>().GetEnumerator();
            _delays[i] = 0f;
            return true;
        }

        /// <summary>
        /// Stop the specified routine.
        /// </summary>
        /// <returns>True if the routine was actually stopped.</returns>
        /// <param name="handle">The routine to stop.</param>
        public bool Stop(in CoroutineHandle handle) => Stop(handle.Enumerator);

        /// <summary>
        /// Stop all running routines.
        /// </summary>
        public void StopAll()
        {
            _running.Clear();
            _delays.Clear();
        }

        /// <summary>
        /// Check if the routine is currently running.
        /// </summary>
        /// <returns>True if the routine is running.</returns>
        /// <param name="routine">The routine to check.</param>
        public bool IsRunning(IEnumerator<object?> routine) => _running.Contains(routine);

        /// <summary>
        /// Check if the routine is currently running.
        /// </summary>
        /// <returns>True if the routine is running.</returns>
        /// <param name="handle">The routine to check.</param>
        public bool IsRunning(in CoroutineHandle handle) => IsRunning(handle.Enumerator);

        /// <summary>
        /// Update all running coroutines.
        /// </summary>
        /// <returns>True if any routines were updated.</returns>
        /// <param name="deltaTime">How many seconds have passed sinced the last update.</param>
        public bool Update(float deltaTime)
        {
            if (_running.Count > 0)
            {
                for (var i = 0; i < _running.Count; i++)
                {
                    if (_delays[i] > 0f)
                        _delays[i] -= deltaTime;
                    else if (_running[i] == null || !MoveNext(_running[i], i))
                    {
                        _running.RemoveAt(i);
                        _delays.RemoveAt(i--);
                    }
                }
                return true;
            }
            return false;
        }

        private bool MoveNext(IEnumerator<object?> routine, int index)
        {
            if (routine.Current is IEnumerator<object?> current)
            {
                if (MoveNext(current, index))
                    return true;

                _delays[index] = 0f;
            }

            var result = routine.MoveNext();

            if (routine.Current is float routineCurrent)
                _delays[index] = routineCurrent;

            return result;
        }
    }

    /// <summary>
    /// A handle to a (potentially running) coroutine.
    /// </summary>
    public readonly struct CoroutineHandle
    {
        /// <summary>
        /// Reference to the routine's runner.
        /// </summary>
        internal readonly CoroutineRunner Runner;

        /// <summary>
        /// Reference to the routine's enumerator.
        /// </summary>
        internal readonly IEnumerator<object?> Enumerator;

        /// <summary>
        /// True if the enumerator is currently running.
        /// </summary>
        public bool IsRunning => Runner.IsRunning(in this);

        /// <summary>
        /// Construct a coroutine. Never call this manually, only use return values from Coroutines.Run().
        /// </summary>
        /// <param name="runner">The routine's runner.</param>
        /// <param name="enumerator">The routine's enumerator.</param>
        internal CoroutineHandle(CoroutineRunner runner, IEnumerator<object?> enumerator)
        {
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));
            Enumerator = enumerator ?? throw new ArgumentNullException(nameof(runner));
        }

        /// <summary>
        /// Stop this coroutine if it is running.
        /// </summary>
        /// <returns>True if the coroutine was stopped.</returns>
        public bool Stop() => IsRunning && Runner.Stop(in this);

        /// <summary>
        /// A routine to wait until this coroutine has finished running.
        /// </summary>
        /// <returns>The wait enumerator.</returns>
        public IEnumerator<object?> Wait()
        {
            while (Runner.IsRunning(in this))
                yield return null;
        }
    }
}
