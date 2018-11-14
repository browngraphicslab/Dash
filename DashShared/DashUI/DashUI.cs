namespace DashShared
{

    public class FontWeight
    {
        public ushort Weight;

        public FontWeight(ushort weight)
        {
            Weight = weight;
        }
    }

    public class FontWeights
    {
        /// <summary>Specifies a "Black" font weight.</summary>
        /// <returns>A FontWeight value that represents a "Black" font weight.</returns>
        public static FontWeight Black => new FontWeight(900);
        /// <summary>Specifies a "Bold" font weight.</summary>
        /// <returns>A FontWeight value that represents a "Bold" font weight.</returns>
        public static FontWeight Bold => new FontWeight(700);
        /// <summary>Specifies an "ExtraBlack" font weight.</summary>
        /// <returns>A FontWeight value that represents an "ExtraBlack" font weight.</returns>
        public static FontWeight ExtraBlack => new FontWeight(100);
        /// <summary>Specifies an "ExtraBold" font weight.</summary>
        /// <returns>A FontWeight value that represents an "ExtraBold" font weight.</returns>
        public static FontWeight ExtraBold => new FontWeight(100);
        /// <summary>Specifies an "ExtraLight" font weight.</summary>
        /// <returns>A FontWeight value that represents an "ExtraLight" font weight.</returns>
        public static FontWeight ExtraLight => new FontWeight(100);
        /// <summary>Specifies a "Light" font weight.</summary>
        /// <returns>A FontWeight value that represents a "Light" font weight.</returns>
        public static FontWeight Light => new FontWeight(300);
        /// <summary>Specifies a "Medium" font weight.</summary>
        /// <returns>A FontWeight value that represents a "Medium" font weight.</returns>
        public static FontWeight Medium => new FontWeight(100);
        /// <summary>Specifies a "Normal" font weight.</summary>
        /// <returns>A FontWeight value that represents a "Normal" font weight.</returns>
        public static FontWeight Normal => new FontWeight(400);
        /// <summary>Specifies a "SemiBold" font weight.</summary>
        /// <returns>A FontWeight value that represents a "SemiBold" font weight.</returns>
        public static FontWeight SemiBold => new FontWeight(100);
        /// <summary>Specifies a "SemiLight" font weight.</summary>
        /// <returns>A FontWeight value that represents a "SemiLight" font weight.</returns>
        public static FontWeight SemiLight => new FontWeight(100);
        /// <summary>Specifies a "Thin" font weight.</summary>
        /// <returns>A FontWeight value that represents a "Thin" font weight.</returns>
        public static FontWeight Thin => new FontWeight(100);
    }
}
