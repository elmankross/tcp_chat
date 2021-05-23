using System.Linq;

namespace Common
{
    public static class ArrayExtensions
    {
        public static byte[] Join(byte[][] parts)
        {
            var buffer = new byte[parts.Sum(x => x.Length)];

            var index = 0;
            for (var i = 0; i < parts.Length; i++)
            {
                for (var j = 0; j < parts[i].Length; j++)
                {
                    buffer[index++] = parts[i][j];
                }
            }

            return buffer;
        }
    }
}
