using MemoryPack;
using MTS.Game.BaseSkill;

namespace MTS.Game.Snapshot
{
    [MemoryPackable]
    public partial class NpcData
    {
        public CharacterStatsSecureSnapshot CharStats { get; set; }
        public ETypeCombo ComboType { get; set; }
        [MemoryPackIgnore]
        private ComboWithType comboChar;
        [MemoryPackIgnore]
        public ComboWithType ComboChar
        {
            get
            {
                if (comboChar == null)
                {
                    comboChar = ComboSkillResource.Instance
                        .ComboNpc.First(x => x.TypeCombo == ComboType);
                }
                return comboChar;
            }
        }
        public (List<int>, List<int>, List<int>) GetCategorizeThinhGiaoThanThong()
        {
            // Ép kiểu danh sách ThanThong về List<BaseSkill> bằng .Cast<BaseSkill>().ToList()
            return GetCategorizeThinhGiaoWithId(ComboChar.ComboWithDaiCanhGioi[TT.ThanThong.GetDaiCanhGioi(CharStats.Progression.Level) - 1].ThanThongs.Cast<BaseSkill.BaseSkill>().ToList());
        }

        public (List<int>, List<int>, List<int>) GetCategorizeThinhGiaoCongPhap()
        {
            // Ép kiểu danh sách CongPhap về List<BaseSkill> tương tự
            return GetCategorizeThinhGiaoWithId(ComboChar.ComboWithDaiCanhGioi[TT.ThanThong.GetDaiCanhGioi(CharStats.Progression.Level) - 1].CongPhaps.Cast<BaseSkill.BaseSkill>().ToList());
        }
        private (List<int>, List<int>, List<int>) GetCategorizeThinhGiaoWithId(List<BaseSkill.BaseSkill> baseSkills)
        {

            List<int> list1 = new List<int>();
            List<int> list2 = new List<int>();
            List<int> list3 = new List<int>();
            if (baseSkills == null || baseSkills.Count == 0)
                return (list1, list2, list3);
            var validItems = baseSkills.Where(x => x != null).ToList();
            if (validItems.Count == 0)
                return (list1, list2, list3);

            var maxTier = validItems.Max(x => x.eTierSkill);

            foreach (var item in validItems)
            {
                var tier = item.eTierSkill;

                // TRƯỜNG HỢP 1: Có Thiên Giai (Bất kể Hạ/Trung/Thượng)
                if (maxTier >= BaseSkill.ETierSkill.ThienGiaiHa)
                {
                    if (tier >= BaseSkill.ETierSkill.ThienGiaiHa) list3.Add(item.Id);
                    else if (tier >= BaseSkill.ETierSkill.DiaGiaiHa) list2.Add(item.Id);
                    else if (tier >= BaseSkill.ETierSkill.NhanGiaiHa) list1.Add(item.Id);
                }
                // TRƯỜNG HỢP 2: Cao nhất là Địa Giai
                else if (maxTier >= BaseSkill.ETierSkill.DiaGiaiHa)
                {
                    if (tier >= BaseSkill.ETierSkill.DiaGiaiTrung) list3.Add(item.Id); // Địa Thượng + Trung
                    else if (tier == BaseSkill.ETierSkill.DiaGiaiHa) list2.Add(item.Id); // Địa Hạ
                    else if (tier >= BaseSkill.ETierSkill.NhanGiaiHa) list1.Add(item.Id); // Nhân Giai
                }
                // TRƯỜNG HỢP 3: Cao nhất chỉ có Nhân Giai
                else if (maxTier >= BaseSkill.ETierSkill.NhanGiaiHa)
                {
                    if (tier == BaseSkill.ETierSkill.NhanGiaiThuong) list3.Add(item.Id);
                    else if (tier == BaseSkill.ETierSkill.NhanGiaiTrung) list2.Add(item.Id);
                    else if (tier == BaseSkill.ETierSkill.NhanGiaiHa) list1.Add(item.Id);
                }
            }

            return (list1, list2, list3);
        }
    }
}