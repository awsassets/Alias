using System;

class ErrorException : Exception
{
    public ErrorException(string message)
        : base(message)
    {
    }
}