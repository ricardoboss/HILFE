namespace StepLang.Utils;

public static class ResultExtensions
{
	public static Result<T> ToResult<T>(this T value) => Result<T>.Ok(value);

	public static Result<T> ToErr<T>(this Exception e) => Result<T>.Err(e);

	public static Result<TNew> Map<TOld, TNew>(this Result<TOld> result, Func<TOld, TNew>? mapper = null)
	{
		switch (result)
		{
			case Ok<TOld> ok:
				if (mapper is null)
					throw new InvalidOperationException("Cannot map an Ok result without a mapper");

				return Result<TNew>.Ok(mapper(ok.Value));
			case Err<TOld> err:
				return Result<TNew>.Err(err.Exception);
			default:
				throw new InvalidOperationException("Unexpected result type");
		}
	}
}
