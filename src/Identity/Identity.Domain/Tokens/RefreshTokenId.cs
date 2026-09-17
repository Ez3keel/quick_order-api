namespace Identity.Domain.Tokens;

public readonly record struct RefreshTokenId(Guid Value)
{
    public static RefreshTokenId New() => new(Guid.NewGuid());

    public static RefreshTokenId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
