using System;

class WarningException : Exception
{
    public WarningException(string message)
        : base(message)
    {
    }
}