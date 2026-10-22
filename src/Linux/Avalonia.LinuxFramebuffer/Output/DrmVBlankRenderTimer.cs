using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Rendering;
using static Avalonia.LinuxFramebuffer.Output.LibDrm;

namespace Avalonia.LinuxFramebuffer.Output
{
    /// <summary>
    /// A render timer that ticks once per vertical blank of a CRTC.
    /// </summary>
    internal sealed unsafe class DrmVBlankRenderTimer : IRenderTimer
    {
        private readonly int _fd;
        private readonly uint _pipeFlags;
        private readonly AutoResetEvent _wakeEvent = new(false);
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private volatile Action<TimeSpan>? _tick;
        private Thread? _thread;

        private DrmVBlankRenderTimer(int fd, uint pipeFlags)
        {
            _fd = fd;
            _pipeFlags = pipeFlags;
        }

        public static DrmVBlankRenderTimer? TryCreate(int fd, int pipe)
        {
            var pipeFlags = ((uint)pipe << DRM_VBLANK_HIGH_CRTC_SHIFT) & DRM_VBLANK_HIGH_CRTC_MASK;

            // A relative wait for 0 vblanks returns right away, so it only checks that vblank waits work
            var vbl = new drmVBlank { type = DRM_VBLANK_RELATIVE | pipeFlags };
            return drmWaitVBlank(fd, &vbl) == 0 ? new DrmVBlankRenderTimer(fd, pipeFlags) : null;
        }

        public bool RunsInBackground => true;

        public Action<TimeSpan>? Tick
        {
            get => _tick;
            set
            {
                _tick = value;
                if (value == null)
                    return;
                if (_thread == null)
                {
                    _thread = new Thread(RunLoop) { IsBackground = true, Name = "DRM VBlank" };
                    _thread.Start();
                }
                _wakeEvent.Set();
            }
        }

        private void RunLoop()
        {
            uint? lastSequence = null;
            while (true)
            {
                var tick = _tick;
                if (tick == null)
                {
                    _wakeEvent.WaitOne();
                    lastSequence = null;
                    continue;
                }

                // Wait for the vblank after the last tick rather than the next one,
                // so a tick that ran late is followed right away instead of skipping a frame
                var vbl = lastSequence is { } last
                    ? new drmVBlank { type = DRM_VBLANK_ABSOLUTE | _pipeFlags, sequence = last + 1 }
                    : new drmVBlank { type = DRM_VBLANK_RELATIVE | _pipeFlags, sequence = 1 };
                if (drmWaitVBlank(_fd, &vbl) == 0)
                {
                    lastSequence = vbl.sequence;
                }
                else
                {
                    // E.g. the CRTC is off, keep ticking at roughly 60 Hz
                    lastSequence = null;
                    Thread.Sleep(16);
                }

                tick(_stopwatch.Elapsed);
            }
        }
    }
}
