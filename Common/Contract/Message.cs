using System.Linq;
using System.Text;

namespace Common.Contract
{
    public struct Message
    {
        /// <summary>
        /// STX
        /// </summary>
        public const byte HEADER = 0x02;

        public string Text { get; set; }
        public byte[] Buffer { get; private set; }

        public Message(string text)
        {
            Text = text;

            var buffer = Encoding.ASCII.GetBytes(Text).ToList();
            buffer.Insert(0, HEADER);
            Buffer = buffer.ToArray();
        }
    }
}
