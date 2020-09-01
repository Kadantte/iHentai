﻿using System.Diagnostics;
using NiL.JS.Core;

namespace iHentai.Extensions.Runtime
{
    public class Log
    {
        private readonly string _extensionId;

        public Log(string extensionId)
        {
            _extensionId = extensionId;
        }

        public void log(JSValue value)
        {
            Debug.WriteLine(_extensionId + ":" + value);
        }
    }
}