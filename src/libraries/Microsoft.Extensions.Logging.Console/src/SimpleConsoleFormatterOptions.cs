// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Options for the built-in default console log formatter.
    /// </summary>
    public class SimpleConsoleFormatterOptions : ConsoleFormatterOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleConsoleFormatterOptions"/> class.
        /// </summary>
        public SimpleConsoleFormatterOptions() { }

        /// <summary>
        /// Gets or sets the behavior that describes when to use color when logging messages.
        /// </summary>
        public LoggerColorBehavior ColorBehavior { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the entire message is logged in a single line.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the entire message is logged in a single line.
        /// </value>
        public bool SingleLine { get; set; }

        internal override void Configure(IConfiguration configuration) => configuration.Bind(this);
    }
}
