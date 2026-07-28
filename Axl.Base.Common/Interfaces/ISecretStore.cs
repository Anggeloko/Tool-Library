using System;
using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    /// <summary>
    /// Define un contrato común para el almacenamiento seguro de credenciales y secretos.
    /// </summary>
    public interface ISecretStore
    {
        /// <summary>
        /// Guarda un secreto en texto plano asociado a un identificador único (clave).
        /// </summary>
        void StoreSecret(string keyName, string secretText);

        /// <summary>
        /// Guarda un secreto de forma segura usando SecretValue asociado a un identificador único (clave).
        /// </summary>
        void StoreSecret(string keyName, SecretValue secret);

        /// <summary>
        /// Recupera un secreto en texto plano a partir de su identificador único (clave).
        /// </summary>
        string RetrieveSecret(string keyName);

        /// <summary>
        /// Recupera un secreto de forma segura como SecretValue a partir de su identificador único (clave).
        /// </summary>
        SecretValue RetrieveSecretSecure(string keyName);

        /// <summary>
        /// Elimina un secreto guardado a partir de su identificador único (clave).
        /// </summary>
        void DeleteSecret(string keyName);
    }
}
