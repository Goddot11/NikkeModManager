using System;

namespace NikkeModManagerCore.Exceptions;

internal class NotSkinException : Exception
{
    public NotSkinException() { }
    public NotSkinException(string message) : base(message) { }
}