using System.Windows.Threading;

namespace System.Windows.Controls
{
    public static class ControlExtensions
    {
        public static void InvokeIfRequired<T>(this T source, Action<T> action)
            where T : Control
        {
            if (source.Dispatcher.CheckAccess())
                action(source);
            else
                source.Dispatcher.Invoke(new Action(() => action(source)));
        }

        public static void BeginInvokeIfRequired<T>(this T source, Action<T> action)
            where T : Control
        {
            if (source.Dispatcher.CheckAccess())
                action(source);
            else
                source.Dispatcher.BeginInvoke(new Action(() => action(source)));
        }
    }
}
