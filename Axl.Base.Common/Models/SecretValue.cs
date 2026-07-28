using System;
using System.Text;

namespace Axl.Base.Models
{
    /// <summary>
    /// Envoltorio seguro para secretos que almacena los datos en un array de bytes
    /// y los sobrescribe con ceros al destruirse o ser liberado.
    /// </summary>
    public class SecretValue : IDisposable
    {
        private byte[] _value;

        /// <summary>
        /// Crea una instancia a partir de un array de bytes UTF-8 existente.
        /// </summary>
        public SecretValue(byte[] utf8Bytes)
        {
            if (utf8Bytes == null) throw new ArgumentNullException(nameof(utf8Bytes));
            _value = new byte[utf8Bytes.Length];
            Buffer.BlockCopy(utf8Bytes, 0, _value, 0, utf8Bytes.Length);
        }

        /// <summary>
        /// Crea una instancia a partir de un texto plano (string).
        /// </summary>
        public SecretValue(string secretText)
        {
            if (secretText == null) throw new ArgumentNullException(nameof(secretText));
            _value = Encoding.UTF8.GetBytes(secretText);
        }

        /// <summary>
        /// Convierte el secreto a string en texto plano.
        /// </summary>
        public string GetString()
        {
            if (_value == null) throw new ObjectDisposedException(nameof(SecretValue));
            return Encoding.UTF8.GetString(_value);
        }

        /// <summary>
        /// Retorna una copia del array de bytes subyacente.
        /// </summary>
        public byte[] GetBytes()
        {
            if (_value == null) throw new ObjectDisposedException(nameof(SecretValue));
            byte[] copy = new byte[_value.Length];
            Buffer.BlockCopy(_value, 0, copy, 0, _value.Length);
            return copy;
        }

        /// <summary>
        /// Libera y limpia la memoria asociada al secreto.
        /// </summary>
        public void Dispose()
        {
            Clean();
            GC.SuppressFinalize(this);
        }

        private void Clean()
        {
            if (_value != null)
            {
                Array.Clear(_value, 0, _value.Length);
                _value = null;
            }
        }

        /// <summary>
        /// Destructor (finalizador) para asegurar el borrado con ceros en la recolección de basura.
        /// </summary>
        ~SecretValue()
        {
            Clean();
        }
    }
}
