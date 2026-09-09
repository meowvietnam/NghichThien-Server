using MemoryPack;
using Network.Models.Clans;
using Xunit;

namespace MTS.Tests.Clans;

public class ClanProtocolTests
{
    [Fact]
    public void Member_visuals_round_trip_without_database_navigation()
    {
        var member = RoundTrip(new ClanMemberDto { CharId = 12, Gender = MTS.Models.Gender.Female,
            Appearance = new MTS.Models.Appearance { CharId = 12,
                Skins = new() { ["hair"] = "hair-2" },
                Colors = new() { ["hair"] = new MTS.Models.Color() } } });
        Assert.Equal(MTS.Models.Gender.Female, member.Gender);
        Assert.Equal("hair-2", member.Appearance.Skins["hair"]);
        Assert.True(member.Appearance.Colors.ContainsKey("hair"));
        Assert.Equal(0, member.Appearance.CharId);
        Assert.Null(member.Appearance.CharacterData);
    }
    private static T RoundTrip<T>(T value) => MemoryPackSerializer.Deserialize<T>(MemoryPackSerializer.Serialize(value))!;

    [Fact]
    public void Requests_preserve_payment_donation_and_query_fields()
    {
        var id = Guid.NewGuid();
        var command = RoundTrip(new ClanCommandRequest { RequestId = id, Command = ClanCommandType.Create,
            Name = "Tông Môn Kim", CreationPaymentMethod = 1, PaymentOrderId = 1234567890123,
            CastleNodeId = 42, DonationCount = 10, FreeDonation = true, InheritanceId = 5 });
        Assert.Equal(id, command.RequestId); Assert.Equal("Tông Môn Kim", command.Name);
        Assert.Equal(1, command.CreationPaymentMethod); Assert.Equal(1234567890123, command.PaymentOrderId);
        Assert.Equal(42, command.CastleNodeId); Assert.Equal(10, command.DonationCount);
        Assert.True(command.FreeDonation); Assert.Equal(5, command.InheritanceId);
        var query = RoundTrip(new ClanQueryRequest { RequestId = id, Query = ClanQueryType.Payments, AfterId = 9876543210123, Limit = 50 });
        Assert.Equal(id, query.RequestId); Assert.Equal(ClanQueryType.Payments, query.Query);
        Assert.Equal(9876543210123, query.AfterId); Assert.Equal(50, query.Limit);
    }

    [Fact]
    public void Response_preserves_inherited_details_nullable_fields_and_nested_arrays()
    {
        var now = DateTime.UtcNow;
        var response = RoundTrip(new ClanResponse { Success = true, MoneyVip = 123, Detail = new ClanDetailDto {
                Id = 42, Name = "Thanh Vân", JadeFunds = 1234567890123, CastleNodeId = 7, ActiveInheritanceId = 1,
                ImageUrl = "https://cdn.example.com/clans/42/image.png" },
            Payments = [new ClanPaymentDto { OrderId = 1234567890123, ExpiresAt = now, ClanId = null }],
            Inheritances = [new ClanInheritanceDto { Id = 1, Learned = true }],
            Clans = [new ClanSummaryDto { Id = 2 }], Members = [new ClanMemberDto { CharId = 3 }],
            Applications = [new ClanApplicationDto { Id = 4 }], Logs = [new ClanLogDto { Delta = -50 }] });
        Assert.True(response.Success); Assert.Equal(123, response.MoneyVip); Assert.Null(response.LinhThach);
        Assert.Equal(42, response.Detail.Id); Assert.Equal("Thanh Vân", response.Detail.Name);
        Assert.Equal("https://cdn.example.com/clans/42/image.png", response.Detail.ImageUrl);
        Assert.Equal(1234567890123, response.Detail.JadeFunds); Assert.Equal(7, response.Detail.CastleNodeId);
        Assert.Equal(1, response.Detail.ActiveInheritanceId);
        Assert.Equal(now, Assert.Single(response.Payments).ExpiresAt); Assert.Null(response.Payments[0].ClanId);
        Assert.True(Assert.Single(response.Inheritances).Learned);
        Assert.Equal(2, Assert.Single(response.Clans).Id); Assert.Equal(3, Assert.Single(response.Members).CharId);
        Assert.Equal(4, Assert.Single(response.Applications).Id); Assert.Equal(-50, Assert.Single(response.Logs).Delta);
        Assert.Null(RoundTrip(new ClanResponse()).Detail);
    }

    [Fact]
    public void Invalidation_preserves_event_and_version()
    {
        var id = Guid.NewGuid();
        var change = RoundTrip(new ClanChangedDto { EventId = id, ClanId = 12, Version = 9876543210 });
        Assert.Equal(id, change.EventId); Assert.Equal(12, change.ClanId); Assert.Equal(9876543210, change.Version);
    }
}
