using System;

namespace NikkeModManagerCore.Exceptions;

internal class NotWindowsException : Exception
{
    public string TargetPlatform;

    public NotWindowsException(string targetPlatform) : base(targetPlatform)
    {
        TargetPlatform = targetPlatform;
    }
}