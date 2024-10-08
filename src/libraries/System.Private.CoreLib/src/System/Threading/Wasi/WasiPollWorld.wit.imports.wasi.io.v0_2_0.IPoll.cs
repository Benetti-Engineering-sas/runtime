// Generated by `wit-bindgen` 0.32.0. DO NOT EDIT!
// <auto-generated />
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace WasiPollWorld.wit.imports.wasi.io.v0_2_0;

internal interface IPoll {

    /**
    * `pollable` represents a single I/O event which may be ready, or not.
    */

    internal class Pollable: IDisposable {
        internal int Handle { get; set; }

        internal readonly record struct THandle(int Handle);

        internal Pollable(THandle handle) {
            Handle = handle.Handle;
        }

        public void Dispose() {
            Dispose(true);
        }

        [DllImport("wasi:io/poll@0.2.0", EntryPoint = "[resource-drop]pollable"), WasmImportLinkage]
        private static extern void wasmImportResourceDrop(int p0);

        protected virtual void Dispose(bool disposing) {
            if (disposing && Handle != 0) {
                wasmImportResourceDrop(Handle);
                Handle = 0;
            }
        }

        internal static class ReadyWasmInterop
        {
            [DllImport("wasi:io/poll@0.2.0", EntryPoint = "[method]pollable.ready"), WasmImportLinkage]
            internal static extern int wasmImportReady(int p0);

        }

        internal   unsafe bool Ready()
        {
            var handle = this.Handle;
            var result =  ReadyWasmInterop.wasmImportReady(handle);
            return (result != 0);

            //TODO: free alloc handle (interopString) if exists
        }

        internal static class BlockWasmInterop
        {
            [DllImport("wasi:io/poll@0.2.0", EntryPoint = "[method]pollable.block"), WasmImportLinkage]
            internal static extern void wasmImportBlock(int p0);

        }

        internal   unsafe void Block()
        {
            var handle = this.Handle;
            BlockWasmInterop.wasmImportBlock(handle);

            //TODO: free alloc handle (interopString) if exists
        }

    }

}
