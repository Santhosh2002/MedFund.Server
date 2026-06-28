namespace MedFund.Application.Common;

public class MedFundException : Exception
{
    public MedFundException(string message)
        : base(message)
    {
    }
}

public sealed class NotFoundException : MedFundException
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class ForbiddenException : MedFundException
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}

public sealed class ConflictException : MedFundException
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
