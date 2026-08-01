using System.Diagnostics.CodeAnalysis;

namespace MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError
{
    [ExcludeFromCodeCoverage]
    public class DefaultMessage
    {
        //Strings
        public const string StringIsNullOrEmptyMessage = "O valor da propiedade não pode ser nulo, conter apenas espaços em brancos ou vazio";
        public const string AddressNotFoundForZipCodeMessage = "Endereço não encontrado para o CEP informado.";
        public const string ZipCodeCannotBeNullOrEmpty = "O CEP não pode ser nulo ou vazio.";
        public const string ZipCodeMustHaveEightDigits = "O CEP deve conter exatamente 8 dígitos.";
        public const string ErrorFetchingAddress = "Erro ao buscar o endereço.";
        public const string NoContentFromViaCep = "Nenhum conteúdo retornado da API ViaCEP para o CEP fornecido.";
        public static string FormatViaCepApiErrorMessage(string statusCode) => $"Falha ao consumir a API ViaCEP. Status code: {statusCode}";



        //Números
        public const string ValueLessThenZero = "O valor informado não pode ser menor que zero";
        public const string ValueLessThenZeroOrEqual = "O valor informado não pode ser igual ou menor que zero";

        //Ids
        public static readonly string GuidEmptyMessage = $"O valor não pode ser {Guid.Empty}";

        // Datetime
        public const string InvalidDatetime = "A data infomada é invalida";

        //Entitys
        public const string EntityIsNull = "A entidade se encontra nula";
        public const string CanNotCreateEntity = "Não foi possivel criar a entidade";

        //Database - Commit
        public static string UnitOfWorkErrorCommit = "Houve um problema no commit da transação";

        // Regra de negocios
        public static string EntityWasDelete(in Guid id, string name) => $"A entidade {name} de id {id} se encontra deletado";
    }
}
