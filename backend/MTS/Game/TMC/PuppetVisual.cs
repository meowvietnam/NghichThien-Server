using MTS.Models;

namespace MTS.Game.TMC;

/// <summary>
/// Random ngoại hình (giới tính + skin + màu) cho rối từ bộ skin tạo nhân vật (GameConfig.SkinResources),
/// theo đúng cấu trúc mà CreateCharacterService kiểm tra nên client hiển thị được như nhân vật thật.
/// Chỉ gọi một lần khi tạo rối; kết quả lưu vào Puppet.Gender/AppearanceJson.
/// </summary>
public static class PuppetVisual
{
    public static void Randomize(Puppet puppet, Random? random = null)
    {
        random ??= Random.Shared;
        var appearance = new Appearance();
        var gender = Gender.Male;
        var resources = GameConfig.SkinResources;
        var groups = resources?.GenderSkinGroups?.Where(x => x.SkinGroups is { Count: > 0 }).ToList();
        if (groups is { Count: > 0 })
        {
            var genderGroup = groups[random.Next(groups.Count)];
            gender = genderGroup.Gender;
            var palette = resources!.Colors ?? new List<Color>();
            foreach (var skinGroup in genderGroup.SkinGroups)
            {
                foreach (var category in skinGroup.Categories ?? [])
                {
                    if (category.Skins is not { Count: > 0 }) continue;
                    appearance.Skins[category.Id] = category.Skins[random.Next(category.Skins.Count)].Name;
                }

                foreach (var colorGroup in skinGroup.ColorGroups ?? [])
                {
                    // Cùng một nhóm màu dùng chung một màu để phối đồ không bị loạn.
                    var color = palette.Count > 0 ? palette[random.Next(palette.Count)] : colorGroup.DefaultColor;
                    color.A = 1;
                    foreach (var colorId in colorGroup.CategoryIds ?? [])
                    {
                        appearance.Colors[colorId] = color;
                    }
                }
            }
        }

        puppet.Gender = gender;
        puppet.Appearance = appearance;
    }
}
