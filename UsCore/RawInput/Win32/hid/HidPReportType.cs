namespace UsCore.RawInput.Win32
{
    /// <summary>
    /// The HIDP_REPORT_TYPE enumeration type is used to specify a HID report type.
    /// </summary>
    internal enum HidPReportType
    {
        /// <summary>
        /// Indicates an input report.
        /// </summary>
        Input = 0,
        /// <summary>
        /// Indicates an output report.
        /// </summary>
        Output = 1,
        /// <summary>
        /// Indicates a feature report.
        /// </summary>
        Feature = 2,
    }
}
