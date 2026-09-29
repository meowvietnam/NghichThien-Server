using System.ComponentModel.DataAnnotations;

namespace MTS.Models;

/// <summary>
/// Quyền sở hữu node thông thường trong Tông môn chiến. Mỗi node có tối đa một bản ghi,
/// <see cref="ClanId"/> null nghĩa là node trung lập. Thành chính (<see cref="Clan.NodeId"/>)
/// không lưu ở đây mà luôn suy ra từ bảng Clans.
/// </summary>
public class ClanNode
{
    [Key]
    public int NodeId { get; set; }
    public int? ClanId { get; set; }
    public long Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}
