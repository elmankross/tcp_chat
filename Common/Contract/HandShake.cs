using System;
using System.Linq;
using System.Text;

namespace Common.Contract
{
    public struct HandShake
    {
        /// <summary>
        /// SOH
        /// </summary>
        public const byte HEADER = 0x01;

        public string Username { get; set; }
        public byte[] Buffer { get; private set; }


        public HandShake(string username)
        {
            Username = username;


            var buffer = Encoding.ASCII.GetBytes(Username).ToList();
            buffer.Insert(0, HEADER);
            Buffer = buffer.ToArray();
        }
    }
}
