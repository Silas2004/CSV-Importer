using ProtoBuf;

namespace CsvImporter.Core.Models;

[ProtoContract]
public class Customer
{
    [ProtoMember(1)] public long Id { get; set; }
    [ProtoMember(2)] public string FirstName { get; set; } = string.Empty;
    [ProtoMember(3)] public string LastName { get; set; } = string.Empty;
    [ProtoMember(4)] public string Email { get; set; } = string.Empty;
    [ProtoMember(5)] public string Phone { get; set; } = string.Empty;
    [ProtoMember(6)] public DateTime? BirthDate { get; set; }
}
