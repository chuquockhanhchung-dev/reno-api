using RenoApi.Domain;

namespace RenoApi.Data;

public static class Seeder
{
    public static void Run(AppDb db)
    {
        if (db.Users.Any()) return;   // đã seed rồi thì thôi

        var store = new Store { Name = "RENO CAFÉ – Cơ sở 1" };
        db.Stores.Add(store);

        // ---------- TÀI KHOẢN (đổi mật khẩu sau khi chạy thật!) ----------
        string H(string p) => BCrypt.Net.BCrypt.HashPassword(p);
        db.Users.AddRange(
            new User { Username = "truong",  PasswordHash = H("123456"), FullName = "Cửa hàng trưởng", Role = StaffRole.Manager, Store = store },
            new User { Username = "phache",  PasswordHash = H("123456"), FullName = "Châu",     Role = StaffRole.Barista, Store = store },
            new User { Username = "phucvu",  PasswordHash = H("123456"), FullName = "Thu Hà",   Role = StaffRole.Service, Store = store },
            new User { Username = "phucvu2", PasswordHash = H("123456"), FullName = "Minh Đức", Role = StaffRole.Service, Store = store });

        // ---------- CHECKLIST theo file Vận_hành.xlsx ----------
        var R = StaffRole.Barista; var S = StaffRole.Service;
        var O = TaskPhase.Opening; var C = TaskPhase.Closing;
        var bb = TaskPriority.Required; var qt = TaskPriority.Important; var tx = TaskPriority.Routine;
        int sort = 0;
        TaskTemplate T(StaffRole role, TaskPhase phase, string title, string std, int min, TaskPriority pr, bool photo = false)
            => new() { Role = role, Phase = phase, Title = title, Standard = std, Minutes = min, Priority = pr, RequiresPhoto = photo, SortOrder = ++sort };

        db.TaskTemplates.AddRange(
            // PHA CHẾ – THU NGÂN · MỞ CA
            T(R, O, "Bật thiết bị điện – đèn trang trí, chiếu sáng", "Hoạt động bình thường", 5, bb),
            T(R, O, "Khởi động máy pha trà", "Máy lên nguồn, test làm sạch", 5, bb),
            T(R, O, "Khởi động cây nước nóng", "Máy lên nguồn, có nước vào", 1, bb),
            T(R, O, "Khởi động máy định lượng đường", "Máy lên nguồn, bổ sung đường đầy máy", 1, bb),
            T(R, O, "Khởi động máy định lượng sữa đặc", "Máy lên nguồn, bổ sung sữa đặc", 1, bb),
            T(R, O, "Chuẩn bị nguyên liệu pha chế", "Nấu trân châu, hãm cafe, triết trà, sơ chế décor, chuẩn bị trà chờ", 30, bb, true),
            T(R, O, "Khởi động máy dập cốc", "Nhiệt độ đủ 175°C – test dập", 10, bb),
            T(R, O, "Setup syrup · sữa · topping", "Đúng vị trí – đậy nắp – trong tủ mát – bổ sung thiếu", 5, qt, true),
            T(R, O, "Máy làm đá", "Kiểm tra lượng đá đủ trong ngày", 1, qt),
            T(R, O, "Kiểm tra khu vực thu ngân", "Sạch · POS, máy in, máy QR sẵn sàng", 5, qt),
            T(R, O, "Nhận bàn giao quỹ đầu ca 1.000.000đ", "Đếm theo mệnh giá · ký biên bản", 10, bb),
            T(R, O, "Kiểm tra giá menu & voucher hiệu lực", "Đối chiếu POS với menu", 15, bb),
            // PHA CHẾ – THU NGÂN · ĐÓNG CA
            T(R, C, "Đổ bỏ nguyên liệu hết hạn trong ngày", "Không để nguyên liệu quá hạn", 5, bb),
            T(R, C, "Vệ sinh máy pha trà, máy café", "Lau sạch máy, đổ khay nước, vệ sinh họng triết trà", 10, bb),
            T(R, C, "Vệ sinh máy định lượng đường", "Lau sạch máy, đậy kín nắp", 5, bb),
            T(R, C, "Vệ sinh máy định lượng sữa đặc", "Cất sữa đặc vào tủ mát, bơm nước rửa sạch bình chứa & vòi", 10, bb),
            T(R, C, "Vệ sinh máy xay sinh tố", "Lau máy, rửa sạch cối xay, úp khô", 10, bb),
            T(R, C, "Vệ sinh máy dập cốc", "Lau miệng, rãnh, khay đẩy", 5, bb),
            T(R, C, "Vệ sinh tủ lạnh, quầy pha", "Lau sạch bên trong & bên ngoài", 10, bb, true),
            T(R, C, "Đậy nắp syrup · sữa · topping", "Cất tủ mát · dán nhãn HSD", 10, bb),
            T(R, C, "Vệ sinh toàn bộ dụng cụ pha chế", "Rửa sạch – úp phơi khô", 10, bb),
            T(R, C, "Kiểm kê & bổ sung nguyên liệu cho ca sáng", "NVL khô, ly cốc, ống hút, túi mang về, giấy ăn, topping tươi, café phin", 15, bb),
            T(R, C, "Đổ rác khu vực pha chế", "Rửa sạch thùng rác – thay túi mới", 10, bb),
            T(R, C, "Kiểm két cuối ca · đối soát hóa đơn POS", "Quỹ đầu ca đủ 1.000.000đ · nộp doanh thu · không hóa đơn treo", 15, bb, true),
            T(R, C, "Báo cáo hàng hủy / thất thoát · ghi nhật ký ca", "Báo cáo group chat", 5, bb),
            // PHỤC VỤ · MỞ CA
            T(S, O, "Khu tập kết rác + cổng vào + sân", "Quét dọn + xịt rửa", 10, bb, true),
            T(S, O, "Kiểm tra & lau bàn ghế khu khách", "Sạch – đúng vị trí – giấy ăn đầy đủ", 15, bb, true),
            T(S, O, "QR code bàn ngay ngắn", "Logo quay ra – không bị che – menu ngay ngắn", 5, bb),
            T(S, O, "Setup khu takeaway", "Ly S/M/L · ống hút · túi giấy", 10, bb),
            T(S, O, "Setup quầy order phía khách", "Menu · POS · promotion standee", 10, qt),
            T(S, O, "Bật loa nhạc · WiFi · điều hoà 24°C", "Playlist morning · WiFi free OK", 5, tx),
            T(S, O, "Nhà vệ sinh – kho", "Kiểm tra lại theo checklist WC tối hôm trước", 5, tx, true),
            // PHỤC VỤ · ĐÓNG CA
            T(S, C, "Quét & lau bàn ghế khu khách trong nhà", "Không vết bẩn · setup gọn", 15, bb, true),
            T(S, C, "Lau toàn bộ mặt quầy đá thu ngân – ra đồ", "Mặt đá sạch · không vân tay", 10, qt),
            T(S, C, "Vệ sinh nhà vệ sinh (bồn cầu, gương, sàn)", "Bổ sung giấy, xà phòng", 15, bb, true),
            T(S, C, "Lau sàn · đổ rác toàn quán", "Sàn khô · túi rác buộc kín", 15, bb, true),
            T(S, C, "Thu dọn bàn ghế ngoài trời", "Gọn gàng – đúng nơi quy định", 15, bb),
            T(S, C, "Quét & nhặt rác khu ngoài trời – đường đi", "Sạch sẽ", 10, bb),
            T(S, C, "Tắt điều hoà, đèn, loa", "Theo thứ tự đúng quy trình", 5, bb),
            T(S, C, "Tưới cây", "Đẫm gốc", 10, bb));

        // ---------- NVL (giá TẠM — cửa hàng trưởng sửa trong app) ----------
        var ing = new Dictionary<string, Ingredient>();
        void I(string key, string name, string unit, double qty, double price)
            => ing[key] = new Ingredient { Name = name, Unit = unit, PackQty = qty, PackPrice = price };

        I("duong", "Đường (syrup)", "ml", 5000, 120000);
        I("tranhai", "Trà nhài (triết sẵn)", "ml", 10000, 350000);
        I("hongtrasua", "Hồng trà sữa (pha sẵn)", "ml", 10000, 700000);
        I("olongnhaisua", "Ô long nhài sữa (pha sẵn)", "ml", 10000, 700000);
        I("olongnuongsua", "Ô long nướng than sữa", "ml", 10000, 750000);
        I("olonggao", "Ô long gạo Wow Tea2", "g", 500, 400000);
        I("olongnhaitea2", "Ô long nhài Tea2", "g", 500, 400000);
        I("cameliatea2", "Camelia Tea2", "g", 500, 450000);
        I("nhaibup", "Trà nhài búp", "g", 500, 380000);
        I("olongtui", "Ô long túi lọc", "gói", 50, 150000);
        I("botA95", "Bột sữa A95", "g", 1000, 180000);
        I("davinciOi", "Davinci ổi", "ml", 750, 185000);
        I("pureOi", "Purée ổi", "ml", 1000, 160000);
        I("chunkyKhe", "Chunky khế", "ml", 1000, 170000);
        I("chunkyXoiXoai", "Chunky xôi xoài", "ml", 1000, 175000);
        I("matong", "Mật ong", "ml", 1000, 150000);
        I("comnon", "Cốm non", "g", 500, 120000);
        I("vaiFruitmix", "Vải fruit mix", "ml", 1000, 165000);
        I("vaiSanh", "Vải sành (syrup)", "ml", 750, 140000);
        I("quytLemao", "Quýt Lemao", "ml", 1000, 190000);
        I("mutNhai", "Mứt nhài", "ml", 1000, 180000);
        I("luu", "Lựu (syrup/mứt)", "ml", 1000, 170000);
        I("wingsDao", "Wings đào", "ml", 1000, 160000);
        I("camSensi", "Cam Sensi", "ml", 750, 150000);
        I("saTuoi", "Sả tươi (nước sả)", "ml", 1000, 40000);
        I("kemCheese", "Kem cheese", "g", 1000, 150000);
        I("kemMan", "Kem mặn", "g", 1000, 140000);
        I("kemTrung", "Kem trứng", "g", 1000, 160000);
        I("kemMuoi", "Kem muối", "g", 1000, 150000);
        I("muoiHong", "Muối hồng", "g", 500, 60000);
        I("vunOreo", "Vụn Oreo", "g", 1000, 130000);
        I("chanh", "Chanh", "quả", 30, 25000);
        I("quat", "Quất", "quả", 100, 30000);
        I("suaDac", "Sữa đặc", "ml", 1250, 62000);
        I("suaTuoi", "Sữa tươi", "ml", 1000, 33000);
        I("suaChua", "Sữa chua", "hộp", 48, 260000);
        I("kemRich", "Kem Rich lùn", "ml", 907, 90000);
        I("cotDua", "Cốt dừa", "ml", 1000, 55000);
        I("vunDua", "Vụn dừa", "thìa", 100, 60000);
        I("bo", "Bơ (quả)", "g", 1000, 60000);
        I("xoai", "Xoài", "g", 1000, 45000);
        I("cafe", "Café (pha sẵn)", "ml", 1000, 120000);
        I("nuocCam", "Nước cốt cam", "ml", 1000, 80000);
        I("botMatcha", "Bột matcha", "g", 500, 350000);
        I("nuocLoc", "Nước lọc", "ml", 20000, 15000);
        db.Ingredients.AddRange(ing.Values);

        // ---------- 33 MÓN từ file công_thức_pha_chế.xlsx ----------
        var thq = "Trà hoa quả"; var ts = "Trà sữa"; var cf = "Cafe – Sinh tố";
        Recipe Rc(string name, string cat, string method, params (string key, double qty)[] lines)
            => new()
            {
                Name = name, Category = cat, Method = method,
                Lines = lines.Select(l => new RecipeLine { Ingredient = ing[l.key], Qty = l.qty }).ToList()
            };

        db.Recipes.AddRange(
            Rc("Ổi hồng", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, décor 1/2 muôi ổi hồng ngâm + 1 muôi thạch quế hoa + 1 lá trà",
                ("duong", 15), ("davinciOi", 20), ("pureOi", 10), ("tranhai", 150)),
            Rc("Khế ngọt", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, décor 2 lát khế tươi + 1 muôi thạch quế hoa + 1 lá trà",
                ("duong", 15), ("chunkyKhe", 30), ("matong", 10), ("tranhai", 150)),
            Rc("Nếp xoài", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, thêm 3Q trắng",
                ("duong", 10), ("chunkyXoiXoai", 15), ("hongtrasua", 150)),
            Rc("Cốm non", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, thêm trân châu ô long",
                ("duong", 20), ("comnon", 30), ("olongnhaisua", 150)),
            Rc("Nếp rang", thq, "Triết 8g ô long gạo Wow Tea2. Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, thêm trân châu ô long",
                ("olonggao", 8), ("duong", 20), ("botA95", 20)),
            Rc("Vải thiều", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, décor 1 quả vải lon + 1 muôi thạch quế hoa + 1 lá trà",
                ("duong", 15), ("vaiFruitmix", 40), ("vaiSanh", 10), ("tranhai", 150)),
            Rc("Trà sen vàng", thq, "Ô long túi lọc 1 gói / 5 phút. Cho trà và đường vào cốc khuấy đều, thêm đá gần miệng cốc, thêm 1/2 muôi thạch củ năng + 7 hạt sen + kem cheese",
                ("olongtui", 1), ("duong", 25), ("kemCheese", 50)),
            Rc("Quýt mọng", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, thêm 1/2 muôi thạch củ năng + 1 lá trà",
                ("duong", 5), ("quytLemao", 20), ("mutNhai", 20), ("tranhai", 150)),
            Rc("Lựu đỏ", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, thêm 1/2 muôi thạch củ năng + 1 lá trà",
                ("duong", 15), ("luu", 25), ("tranhai", 150)),
            Rc("Trà đào", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, thêm 3 lát đào + 1 lá trà",
                ("duong", 15), ("wingsDao", 20), ("tranhai", 150)),
            Rc("Đào cam sả", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, thêm 3 lát đào + 1 lát cam + 1 lát sả + 1 lá trà",
                ("duong", 10), ("wingsDao", 20), ("camSensi", 5), ("saTuoi", 50), ("tranhai", 100)),
            Rc("Trà lá kem mặn", thq, "Triết 8g ô long nhài Tea2. Cho trà + đường vào cốc khuấy đều, thêm đá gần miệng cốc, thêm kem mặn",
                ("olongnhaitea2", 8), ("duong", 15), ("kemMan", 50)),
            Rc("Trà chanh", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, thêm 1 lát chanh",
                ("duong", 25), ("matong", 10), ("chanh", 1), ("tranhai", 150)),
            Rc("Trà quất", thq, "Cho lần lượt các nguyên liệu vào cốc rồi khuấy đều, thêm đá gần miệng cốc, thêm 1 vỏ quất",
                ("duong", 25), ("matong", 10), ("quat", 3), ("tranhai", 150)),
            Rc("Reno kem trứng", ts, "Cho đường + trà vào cốc khuấy đều, thêm đá gần miệng cốc, thêm 1 muôi 3Q trắng, thêm kem trứng + khò",
                ("duong", 15), ("olongnuongsua", 150), ("kemTrung", 50)),
            Rc("Lúa mới", ts, "Cho tất cả nguyên liệu vào cốc khuấy đều, thêm đá gần miệng cốc, thêm 1 muôi 3Q trắng",
                ("duong", 20), ("olongnhaisua", 150)),
            Rc("Reno nguyên bản", ts, "Cho tất cả nguyên liệu vào cốc khuấy đều, thêm đá gần miệng cốc, thêm 1 muôi 3Q trắng",
                ("duong", 20), ("hongtrasua", 150)),
            Rc("Nhài búp trắng", ts, "Triết nhài 6g Tea2. Cho tất cả nguyên liệu vào cốc khuấy đều, thêm đá gần miệng cốc, thêm 1 muôi 3Q trắng",
                ("nhaibup", 6), ("duong", 20), ("botA95", 20)),
            Rc("Reno muối biển", ts, "Cho tất cả nguyên liệu vào cốc khuấy đều, thêm đá gần miệng cốc, thêm 1 muôi 3Q trắng",
                ("duong", 20), ("muoiHong", 0.8), ("olongnuongsua", 150)),
            Rc("Camelia kem mặn", ts, "Triết 8g Camelia Tea2. Cho trà + đường vào khuấy đều, thêm đá gần miệng cốc, thêm 1 muôi 3Q trắng, thêm kem mặn",
                ("cameliatea2", 8), ("duong", 20), ("kemMan", 50)),
            Rc("Camelia sữa", ts, "Triết 8g Camelia Tea2. Cho tất cả nguyên liệu vào cốc khuấy đều, thêm đá gần miệng cốc, thêm 1 muôi 3Q trắng",
                ("cameliatea2", 8), ("duong", 20), ("botA95", 20)),
            Rc("Oreo kem trứng", ts, "Cho tất cả nguyên liệu vào cốc khuấy đều, thêm 1 muôi 3Q trắng, thêm đá gần miệng cốc, thêm kem trứng, 1 thìa vụn oreo",
                ("duong", 15), ("hongtrasua", 150), ("kemTrung", 50), ("vunOreo", 10)),
            Rc("Oreo kem cheese", ts, "Cho tất cả nguyên liệu vào cốc khuấy đều, thêm 1 muôi 3Q trắng, thêm đá gần miệng cốc, thêm kem cheese, 1 thìa vụn oreo",
                ("duong", 15), ("hongtrasua", 150), ("kemCheese", 50), ("vunOreo", 10)),
            Rc("Bơ già dừa non", cf, "Xay bơ + sữa đặc + đường + đá + nước lọc. Cho sữa tươi + cốt dừa + rich vào cốc quấy đều, thêm ít đá rồi đổ hỗn hợp xay vào, thêm 1 thìa vụn dừa",
                ("duong", 15), ("suaDac", 20), ("kemRich", 20), ("cotDua", 20), ("suaTuoi", 50), ("bo", 100), ("nuocLoc", 50), ("vunDua", 1)),
            Rc("Matcha đá xay", cf, "Cho lần lượt nguyên liệu + 1 cốc đá vào cối xay, đổ hỗn hợp ra cốc và thêm kem bóp",
                ("suaDac", 25), ("suaTuoi", 70), ("botMatcha", 5)),
            Rc("Sinh tố xoài", cf, "Cho lần lượt nguyên liệu vào cối xay đều, đổ hỗn hợp vào cốc",
                ("xoai", 100), ("suaChua", 0.33), ("suaTuoi", 70), ("suaDac", 15), ("duong", 10)),
            Rc("Xoài dừa", cf, "Xay xoài + sữa đặc + đường + đá + nước lọc. Cho sữa tươi + cốt dừa + rich vào cốc quấy đều, thêm ít đá rồi đổ hỗn hợp xay vào, thêm 1 thìa vụn dừa",
                ("duong", 10), ("suaDac", 15), ("kemRich", 20), ("cotDua", 20), ("suaTuoi", 50), ("xoai", 100), ("nuocLoc", 50), ("vunDua", 1)),
            Rc("Sữa chua đánh đá", cf, "Cho lần lượt nguyên liệu + 1 cốc đá vào cối xay đều, đổ hỗn hợp vào cốc, thêm 3Q trắng + 1 lát chanh",
                ("suaChua", 1.5), ("suaDac", 30), ("duong", 10), ("quat", 1)),
            Rc("Đen đá", cf, "Cho lần lượt nguyên liệu vào cốc đánh bọt + thêm đá",
                ("duong", 5), ("cafe", 50)),
            Rc("Nâu đá", cf, "Cho lần lượt nguyên liệu vào cốc đánh bọt + thêm đá",
                ("suaDac", 20), ("cafe", 50)),
            Rc("Bạc xỉu", cf, "Cho lần lượt sữa đặc + sữa tươi + rich vào cốc quấy đều, đánh bọt café đổ vào",
                ("suaDac", 40), ("suaTuoi", 50), ("cafe", 30), ("kemRich", 10)),
            Rc("Café muối", cf, "Cho sữa đặc + café vào quấy đều, cho đá + thêm kem muối",
                ("suaDac", 20), ("cafe", 40), ("kemMuoi", 30)),
            Rc("Ép cam", cf, "Cho lần lượt nguyên liệu vào quấy đều, thêm đá, décor 1 lát cam",
                ("nuocCam", 200), ("duong", 15)));

        db.SaveChanges();
    }
}
