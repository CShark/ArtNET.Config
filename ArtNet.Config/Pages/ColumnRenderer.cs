namespace ArtNet.Config.Pages {
    internal class ColumnRenderer {
        private struct Entry {
            public string Name { get; set; }
            public ConsoleColor? Color { get; set; }
            public int Column { get; set; }

            public Entry(string name, ConsoleColor? color, int column) {
                Name = name;
                Color = color;
                Column = column;
            }
        }

        private List<Entry> _items = new List<Entry>();

        public void AddEntry(string name, ConsoleColor? color = null, int column = 0) {
            _items.Add(new Entry(name, color, column));
        }

        public void Render() {
            int maxCols = _items.Max(x => x.Column);
            int colWidth = 100 / (maxCols + 1);

            var items = _items.GroupBy(x => x.Column).ToDictionary(x => x.Key, x => x.AsEnumerable());

            var maxLength = items.Max(x => x.Value.Count());
            foreach (var item in items) {
                items[item.Key] =
                    item.Value.Concat(Enumerable.Repeat(default(Entry), maxLength - item.Value.Count()));
            }

            var organized = items[0].Select(x => new Dictionary<int, Entry>() { [x.Column] = x });
            for (int i = 1; i < items.Count; i++) {
                organized = organized.Zip(items[i], (a, b) => {
                    a[i] = b;
                    return a;
                });
            }

            foreach (var line in organized) {
                foreach (var part in line.Values) {
                    if (string.IsNullOrEmpty(part.Name)) {
                        Console.Write(new string(' ', colWidth));
                        continue;
                    }

                    if (part.Color == null) {
                        Console.ResetColor();
                    } else {
                        Console.ForegroundColor = part.Color.Value;
                    }

                    Console.Write(part.Name.PadRight(colWidth));

                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }
    }
}
