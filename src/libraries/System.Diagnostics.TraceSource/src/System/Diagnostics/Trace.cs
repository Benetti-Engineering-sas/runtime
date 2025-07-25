// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define TRACE
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>Provides a set of properties and methods to trace the execution of your code.</para>
    /// </devdoc>
    public sealed class Trace
    {
        // not creatable...
        //
        private Trace()
        {
        }

        public static CorrelationManager CorrelationManager =>
            Volatile.Read(ref field) ??
            Interlocked.CompareExchange(ref field, new(), null) ??
            field;

        /// <devdoc>
        ///    <para>Gets the collection of listeners that is monitoring the trace output.</para>
        /// </devdoc>
        public static TraceListenerCollection Listeners
        {
            get
            {
                return TraceInternal.Listeners;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets whether <see cref='Flush'/> should be called on the <see cref='Listeners'/> after every write.
        ///    </para>
        /// </devdoc>
        public static bool AutoFlush
        {
            get
            {
                return TraceInternal.AutoFlush;
            }
            set
            {
                TraceInternal.AutoFlush = value;
            }
        }

        public static bool UseGlobalLock
        {
            get
            {
                return TraceInternal.UseGlobalLock;
            }
            set
            {
                TraceInternal.UseGlobalLock = value;
            }
        }

        /// <devdoc>
        ///    <para>Gets or sets the indent level.</para>
        /// </devdoc>
        public static int IndentLevel
        {
            get { return TraceInternal.IndentLevel; }

            set { TraceInternal.IndentLevel = value; }
        }


        /// <devdoc>
        ///    <para>
        ///       Gets or sets the number of spaces in an indent.
        ///    </para>
        /// </devdoc>
        public static int IndentSize
        {
            get { return TraceInternal.IndentSize; }

            set { TraceInternal.IndentSize = value; }
        }

        /// <devdoc>
        ///    <para>Clears the output buffer, and causes buffered data to
        ///       be written to the <see cref='Listeners'/>.</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void Flush()
        {
            TraceInternal.Flush();
        }

        /// <devdoc>
        /// <para>Clears the output buffer, and then closes the <see cref='Listeners'/> so that they no
        ///    longer receive debugging output.</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void Close()
        {
            TraceInternal.Close();
        }

        /// <devdoc>
        ///    <para>Checks for a condition, and outputs the callstack if the
        ///       condition
        ///       is <see langword='false'/>.</para>
        /// </devdoc>
        [Conditional("TRACE")]
        [OverloadResolutionPriority(-1)] // lower priority than (bool, string) overload so that the compiler prefers using CallerArgumentExpression
        public static void Assert([DoesNotReturnIf(false)] bool condition)
        {
            TraceInternal.Assert(condition);
        }

        /// <devdoc>
        ///    <para>Checks for a condition, and displays a message if the condition is
        ///    <see langword='false'/>. </para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] string? message = null)
        {
            TraceInternal.Assert(condition, message);
        }

        /// <devdoc>
        ///    <para>Checks for a condition, and displays both messages if the condition
        ///       is <see langword='false'/>. </para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, string? message, string? detailMessage)
        {
            TraceInternal.Assert(condition, message, detailMessage);
        }

        /// <devdoc>
        ///    <para>Emits or displays a message for an assertion that always fails.</para>
        /// </devdoc>
        [Conditional("TRACE")]
        [DoesNotReturn]
        public static void Fail(string? message)
        {
            TraceInternal.Fail(message);
        }

        /// <devdoc>
        ///    <para>Emits or displays both messages for an assertion that always fails.</para>
        /// </devdoc>
        [Conditional("TRACE")]
        [DoesNotReturn]
        public static void Fail(string? message, string? detailMessage)
        {
            TraceInternal.Fail(message, detailMessage);
        }

        /// <summary>
        ///  Occurs when a <see cref="TraceSource"/> needs to be refreshed from configuration.
        /// </summary>
        public static event EventHandler? Refreshing;

        internal static void OnRefreshing()
        {
            Refreshing?.Invoke(null, EventArgs.Empty);
        }

        public static void Refresh()
        {
            OnRefreshing();
            Switch.RefreshAll();
            TraceSource.RefreshAll();
            TraceInternal.Refresh();
        }

        [Conditional("TRACE")]
        public static void TraceInformation(string? message)
        {
            TraceInternal.TraceEvent(TraceEventType.Information, 0, message, null);
        }

        [Conditional("TRACE")]
        public static void TraceInformation([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[]? args)
        {
            TraceInternal.TraceEvent(TraceEventType.Information, 0, format, args);
        }

        [Conditional("TRACE")]
        public static void TraceWarning(string? message)
        {
            TraceInternal.TraceEvent(TraceEventType.Warning, 0, message, null);
        }

        [Conditional("TRACE")]
        public static void TraceWarning([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[]? args)
        {
            TraceInternal.TraceEvent(TraceEventType.Warning, 0, format, args);
        }

        [Conditional("TRACE")]
        public static void TraceError(string? message)
        {
            TraceInternal.TraceEvent(TraceEventType.Error, 0, message, null);
        }

        [Conditional("TRACE")]
        public static void TraceError([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[]? args)
        {
            TraceInternal.TraceEvent(TraceEventType.Error, 0, format, args);
        }

        /// <devdoc>
        /// <para>Writes a message to the trace listeners in the <see cref='Listeners'/>
        /// collection.</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void Write(string? message)
        {
            TraceInternal.Write(message);
        }

        /// <devdoc>
        /// <para>Writes the name of the <paramref name="value "/>
        /// parameter to the trace listeners in the <see cref='Listeners'/> collection.</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void Write(object? value)
        {
            TraceInternal.Write(value);
        }

        /// <devdoc>
        ///    <para>Writes a category name and message to the trace listeners
        ///       in the <see cref='Listeners'/> collection.</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void Write(string? message, string? category)
        {
            TraceInternal.Write(message, category);
        }

        /// <devdoc>
        ///    <para>Writes a category name and the name of the value parameter to the trace listeners
        ///       in the <see cref='Listeners'/> collection.</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void Write(object? value, string? category)
        {
            TraceInternal.Write(value, category);
        }

        /// <devdoc>
        ///    <para>Writes a message followed by a line terminator to the
        ///       trace listeners in the <see cref='Listeners'/> collection.
        ///       The default line terminator is a carriage return followed by a line feed (\r\n).</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteLine(string? message)
        {
            TraceInternal.WriteLine(message);
        }

        /// <devdoc>
        /// <para>Writes the name of the <paramref name="value "/> parameter followed by a line terminator to the trace listeners in the <see cref='Listeners'/> collection. The default line
        ///    terminator is a carriage return followed by a line feed (\r\n).</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteLine(object? value)
        {
            TraceInternal.WriteLine(value);
        }

        /// <devdoc>
        ///    <para>Writes a category name and message followed by a line terminator to the trace
        ///       listeners in the <see cref='Listeners'/>
        ///       collection. The default line terminator is a carriage return followed by a line
        ///       feed (\r\n).</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteLine(string? message, string? category)
        {
            TraceInternal.WriteLine(message, category);
        }

        /// <devdoc>
        /// <para>Writes a <paramref name="category "/>name and the name of the <paramref name="value "/> parameter followed by a line
        ///    terminator to the trace listeners in the <see cref='Listeners'/> collection. The default line
        ///    terminator is a carriage return followed by a line feed (\r\n).</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteLine(object? value, string? category)
        {
            TraceInternal.WriteLine(value, category);
        }

        /// <devdoc>
        /// <para>Writes a message to the trace listeners in the <see cref='Listeners'/> collection
        ///    if a condition is <see langword='true'/>.</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteIf(bool condition, string? message)
        {
            TraceInternal.WriteIf(condition, message);
        }

        /// <devdoc>
        /// <para>Writes the name of the <paramref name="value "/>
        /// parameter to the trace listeners in the <see cref='Listeners'/> collection if a condition is
        /// <see langword='true'/>. </para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteIf(bool condition, object? value)
        {
            TraceInternal.WriteIf(condition, value);
        }

        /// <devdoc>
        /// <para>Writes a category name and message to the trace listeners in the <see cref='Listeners'/>
        /// collection if a condition is <see langword='true'/>. </para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteIf(bool condition, string? message, string? category)
        {
            TraceInternal.WriteIf(condition, message, category);
        }

        /// <devdoc>
        /// <para>Writes a category name and the name of the <paramref name="value"/> parameter to the trace
        ///    listeners in the <see cref='Listeners'/> collection
        ///    if a condition is <see langword='true'/>. </para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteIf(bool condition, object? value, string? category)
        {
            TraceInternal.WriteIf(condition, value, category);
        }

        /// <devdoc>
        ///    <para>Writes a message followed by a line terminator to the trace listeners in the
        ///    <see cref='Listeners'/> collection if a condition is
        ///    <see langword='true'/>. The default line terminator is a carriage return followed
        ///       by a line feed (\r\n).</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteLineIf(bool condition, string? message)
        {
            TraceInternal.WriteLineIf(condition, message);
        }

        /// <devdoc>
        /// <para>Writes the name of the <paramref name="value"/> parameter followed by a line terminator to the
        ///    trace listeners in the <see cref='Listeners'/> collection
        ///    if a condition is
        /// <see langword='true'/>. The default line
        ///    terminator is a carriage return followed by a line feed (\r\n).</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteLineIf(bool condition, object? value)
        {
            TraceInternal.WriteLineIf(condition, value);
        }

        /// <devdoc>
        ///    <para>Writes a category name and message followed by a line terminator to the trace
        ///       listeners in the <see cref='Listeners'/> collection if a condition is
        ///    <see langword='true'/>. The default line terminator is a carriage return followed by a line feed (\r\n).</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteLineIf(bool condition, string? message, string? category)
        {
            TraceInternal.WriteLineIf(condition, message, category);
        }

        /// <devdoc>
        /// <para>Writes a category name and the name of the <paramref name="value "/> parameter followed by a line
        ///    terminator to the trace listeners in the <see cref='Listeners'/> collection
        ///    if a <paramref name="condition"/> is <see langword='true'/>. The
        ///    default line terminator is a carriage return followed by a line feed (\r\n).</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void WriteLineIf(bool condition, object? value, string? category)
        {
            TraceInternal.WriteLineIf(condition, value, category);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void Indent()
        {
            TraceInternal.Indent();
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [Conditional("TRACE")]
        public static void Unindent()
        {
            TraceInternal.Unindent();
        }
    }
}
