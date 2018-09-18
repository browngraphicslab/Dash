namespace Dash
{
    public class SnapshotView
    {
        public string Title { get; set; }
        public string Image { get; }

        public int Index { get; set; }

        public SnapshotView(string t, string i, int n)
        {
            Title = t;
            Image = i;
            Index = n;
        }
    }
}
