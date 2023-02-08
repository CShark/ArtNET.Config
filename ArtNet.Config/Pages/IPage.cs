using System.Collections.Concurrent;

namespace ArtNet.Config.Pages {
    internal abstract class IPage {
        private static ConcurrentQueue<(Func<object?, Task> render, TaskCompletionSource feedback, object? state)> _renderQueue = new();
        private static TaskCompletionSource<string>? _inputTask = null;

        public static IPage ActivePage { get; set; }

        private readonly IPage _parent;
        protected IPage Parent { get { return _parent; } }
        protected string LastErr { get; set; }

        public IPage(IPage parent) {
            _parent = parent;
        }

        static IPage() {
            Task.Run(async () => {
                while (true) {
                    Thread.Sleep(50);
                    if (_renderQueue.TryDequeue(out var renderCall)) {
                        await renderCall.render(renderCall.state);
                        renderCall.feedback.SetResult();
                    }
                }
            });
        }

        public abstract bool AllowRefresh { get; }

        public IPage? HandleInput() {
            var input = Console.ReadLine();

            if (_inputTask != null && !_inputTask.Task.IsCompleted) {
                Task.Run(() => {
                    _inputTask.SetResult(input ?? "");
                });
                return HandleInput();
            } else {
                return HandleInputInternal(input);
            }
        }

        protected async Task<string> ReadInput() {
            _inputTask = new TaskCompletionSource<string>();
            return await _inputTask.Task;
        }

        protected abstract IPage? HandleInputInternal(string? input);

        public virtual void EnterPage() { }
        public virtual void ExitPage() { }

        public async Task Render(object? state = null) {
            var notification = new TaskCompletionSource();

            _renderQueue.Enqueue((RenderSurrogate, notification, state));

            await notification.Task;
        }

        private async Task RenderSurrogate(object? state) {
            Console.Clear();
            WriteError(LastErr);
            await RenderInternal(state);
            Console.Write("> ");
        }

        public void Refresh(object? state = null, bool force = false) {
            if (ActivePage == this) {
                if (force || AllowRefresh) {
                    Render(state);
                }
            }
        }

        public abstract Task RenderInternal(object? state);

        protected void WriteError(string? message) {
            if (message != null && !string.IsNullOrWhiteSpace(message)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        protected void WriteCentered(string text, ConsoleColor? color = null) {
            if (color != null) {
                Console.ForegroundColor = color.Value;
            }

            Console.WriteLine(text.PadLeft(50 + text.Length / 2));
            Console.ResetColor();
        }

        protected void WriteColored(string text, ConsoleColor? color = null) {
            if (color != null) {
                Console.ForegroundColor = color.Value;
            }

            Console.Write(text);
            Console.ResetColor();
        }

        protected void WriteColoredLine(string text, ConsoleColor? color = null) {
            if (color != null) {
                Console.ForegroundColor = color.Value;
            }

            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}
