﻿// Copyright (c) 2016 The btcsuite developers
// Licensed under the ISC license.  See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Paymetheus.Rpc
{
    public static class TransportSecurity
    {
        private const string CertificateFile = "rpc.cert";

        // Need a non-void type to use for the TaskCompletionSource below.  Name is
        // borrowed from functional programming.
        private struct Unit { }

        public static async Task<string> ReadModifiedCertificateAsync(string directory)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));

            // Asynchronously wait until the certificate file has been written by the wallet
            // process before reading.
            var tcs = new TaskCompletionSource<Unit>();
            var fsw = new FileSystemWatcher
            {
                Path = directory,
                EnableRaisingEvents = true,
            };
            using (fsw)
            {
                fsw.Changed += (sender, args) =>
                {
                    if (args.Name == CertificateFile)
                    {
                        tcs.SetResult(new Unit());
                    }
                };
                await tcs.Task;
            }

            var certificatePath = Path.Combine(directory, CertificateFile);

            // On Windows, process file locks may prevent reading the file even after the
            // filesystem notification was received.  Polling for the unlocked file is icky
            // but I'm unsure of any better way to do this in portable C#, nevermind through
            // the use of native winapi calls.
            while (true)
            {
                try
                {
                    using (var r = new StreamReader(certificatePath, Encoding.UTF8))
                    {
                        return await r.ReadToEndAsync();
                    }
                }
                catch (IOException)
                {
                    await Task.Delay(10);
                }
            }
        }
    }
}
