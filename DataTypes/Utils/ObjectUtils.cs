namespace YuchiGames.POM.Shared.Utils
{
    public static class ObjectUtils
    {
        public static T Apply<T>(this T value, Action<T> action)
        {
            action(value);
            return value;
        }

        public static T Apply<T>(this T value, Action action)
        {
            action();
            return value;
        }

        public static R Let<T, R>(this T value, Func<T, R> action) => 
            action(value);
        public static R Let<T, R>(this T value, Func<R> action) =>
            action();
    }
}
