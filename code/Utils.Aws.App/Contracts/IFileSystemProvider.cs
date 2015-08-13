using System;
using System.Collections.Generic;

namespace Utils.Aws.App.Contracts
{
    public interface IFileSystemProvider<TEnum>
    {
        /// <summary>
        /// Retorna a Uri publica através de sua chave.
        /// </summary>
        /// <param name="key">A chave do arquivo.</param>
        /// <returns>Retorna a Uri do arquivo.</returns>
        Uri GetUri(string key);

        /// <summary>
        /// Faz o upload do arquivo e retorna a sua chave única.
        /// </summary>
        /// <param name="fileType">O tipo do arquivo que será utilizado na geração da chave única.</param>
        /// <param name="fileName">O nome do arquivo que será utilizado na geração da chave única.</param>
        /// <param name="buffer">Os bytes do arquivo que o upload será realizado.</param>
        /// <returns>Retorna a chave do arquivo.</returns>
        string UploadFile(
            TEnum fileType,
            string fileName,
            byte[] buffer,
            bool isPublic = false);

        /// <summary>
        /// Faz a copia de uma arquivo no provedor para outro local e retorna a sua chave única.
        /// </summary>
        /// <param name="destinationFileType">O tipo do arquivo que será utilizado na geração da chave única.</param>
        /// <param name="destinationFileName">O nome do arquivo que será utilizado na geração da chave única.</param>
        /// <param name="sourceKey">A fileKey de um arquivo que já existe no provedor.</param>
        /// <returns>Retorna a chave do arquivo.</returns>
        string CopyFile(
            TEnum destinationFileType,
            string destinationFileName,
            string sourceKey);

        /// <summary>
        /// Faz a exclusão do um arquivo. 
        /// </summary>
        /// <param name="key">A chave única do arquivo.</param>
        void DeleteFile(string key);

        /// <summary>
        /// Retorna objetos em dicionário
        /// </summary>
        /// <param name="searchKey">Busca.</param>
        IDictionary<string, string> GetFiles(string searchKey);

        /// <summary>
        /// Obtém o arquivo e retorna respectivo array de bytes. 
        /// </summary>
        /// <param name="key">A chave única do arquivo.</param>
        /// <param name="path">Caminho do arquivo.</param>
        void DownloadFile(string key,string path);

        /// <summary>
        /// Esvreve a resposta do stream do arquivo
        /// </summary>
        /// <param name="key">A chave única do arquivo.</param>
        byte[] DownloadFile(string key);
    }
}
