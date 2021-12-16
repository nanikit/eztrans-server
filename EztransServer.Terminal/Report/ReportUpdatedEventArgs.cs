using System;

namespace EztransServer.Terminal.Report
{
    /// <summary>
    /// The class that store the Report Updated Event arguments.
    /// </summary>
    public class ReportUpdatedEventArgs : EventArgs
    {

        private string _text;

        /// <summary>
        /// Get the report.
        /// </summary>
        public string Text
        {
            get
            {
                return _text;
            }
        }

        /// <summary>
        /// Initialize the instance that has the information in the updated report.
        /// </summary>
        /// <param name="type">Updated report.</param>
        public ReportUpdatedEventArgs(string reportText)
        {
            _text = reportText;
        }
    }
}
