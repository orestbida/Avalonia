namespace Avalonia.LinuxFramebuffer
{
    /// <summary>
    /// Platform-specific options which apply to the Linux framebuffer.
    /// </summary>
    public class LinuxFramebufferPlatformOptions
    {
        /// <summary>
        /// Gets or sets the number of frames per second at which the renderer should run.
        /// Default 60.
        /// </summary>
        public int Fps { get; set; } = 60;
        
        /// <summary>
        /// Render directly on the UI thread instead of using a dedicated render thread.
        /// This can be usable if your device don't have multiple cores to begin with.
        /// This setting is false by default.
        /// </summary>
        public bool ShouldRenderOnUIThread { get; set; }

        /// <summary>
        /// Tick the renderer on the vertical blanks of the DRM output instead of at a fixed <see cref="Fps"/>.
        /// Has no effect with the framebuffer device output or when <see cref="ShouldRenderOnUIThread"/> is set.
        /// This setting is false by default.
        /// </summary>
        public bool UseVBlankRenderTimer { get; set; }
    }
}
