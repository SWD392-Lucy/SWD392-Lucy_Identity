# Bản mô tả chi tiết dự án phần mềm LUCY

## 1. Tổng quan dự án

**Tên dự án:** LUCY (*Language Unity & Collaborative Youth*)

**Mô hình:** Mạng xã hội âm thanh kết hợp EdTech (Học tập qua giao tiếp).

**Mục tiêu:** Xây dựng môi trường giao tiếp ngôn ngữ (Anh - Trung - Nhật) real-time, giảm áp lực tâm lý qua cơ chế ẩn danh và chuẩn hóa lộ trình học tập qua hệ thống LMS.

---

## 2. Phân tầng người dùng và tính năng

### 2.1. LUCY (Người dùng Ẩn danh)

- **Đối tượng:** Gen Z, người mới bắt đầu học ngôn ngữ, người ngại giao tiếp.
- **Cơ chế ẩn danh:** Sử dụng Avatar Persona ảo, không hiển thị danh tính thật.
- **Tính năng:**
  - Tham gia phòng theo Level.
  - Giơ tay phát biểu.
  - Tặng quà ảo cho Mentor.

### 2.2. LUCY Pro (Mentor/Hiện danh)

- **Đối tượng:** Chuyên gia, giáo viên, người có trình độ ngôn ngữ cao.
- **Tính năng:**
  - Tạo phòng dạy học dựa trên giáo trình (LISA, Chinese, Japanese).
  - Ghim tài liệu học tập.
  - Quản lý lộ trình học.
- **Monetization:**
  - Nhận quà từ học viên.
  - Xây dựng uy tín cá nhân trên bảng xếp hạng.

### 2.3. LUCY Super (Content Creator)

- **Đối tượng:** Influencer, người sáng tạo nội dung chuyên sâu.
- **Tính năng:**
  - Bao gồm quyền Pro.
  - Ghi âm buổi Live thành Podcast.
  - Tạo chuỗi nội dung thu phí (Premium Content).

---

## 3. Kiến trúc kỹ thuật (Microservices)

| Thành phần | Công nghệ | Vai trò |
|---|---|---|
| Real-time Service | Node.js (NJS) | Xử lý Audio (Agora SDK), Socket.io điều phối phòng |
| Content & LMS | Java (Spring) | Số hóa 100 level ngôn ngữ, quản lý giáo trình và logic học tập |
| User & Payment | .NET Core | Quản lý Identity, ví điện tử, quà tặng và bảo mật danh tính |
| Mobile App | Flutter | Ứng dụng đa nền tảng (iOS/Android) với trải nghiệm mượt mà |

---

## 4. Kế hoạch triển khai 10 tuần (Team 5 Dev)

- **Tuần 1-2:** Thiết lập hạ tầng, số hóa tài liệu từ 8 file Word (LISA/Chinese/Japanese) vào Database Java.
- **Tuần 3-5:** Xây dựng core Real-time Audio (NJS + Agora). Mobile tích hợp tính năng phòng cơ bản.
- **Tuần 6-7:** Phát triển công cụ LMS cho Pro (Java) và tính năng Record Podcast cho Super (NJS).
- **Tuần 8-9:** Tích hợp hệ thống thanh toán, quà tặng (.NET) và hiệu ứng tương tác Mobile.
- **Tuần 10:** Stress test hệ thống, fix bug và chuẩn bị bản Beta Launch.

---

## 5. Cấu trúc nội dung (Dữ liệu cốt lõi)

Hệ thống tự động hóa việc đưa nội dung từ các file tài liệu vào phòng Live:

- **Cấp độ:** Chia làm 3 Stage:
  - Sơ cấp
  - Trung cấp
  - Cao cấp
- **Cấu trúc phòng:** Mỗi phòng 60-120 phút, chia nhỏ thành các chặng 10-20 phút (Sub-levels).
- **AI Support:** Gợi ý câu hỏi thảo luận lên màn hình của Moderator dựa trên tài liệu LISA/Chinese/Japanese đã upload.

---

## 6. Kế hoạch triển khai 10 tuần (Roadmap)

### Tuần 1-2: Thiết lập nền tảng & Data

- **Java:** Import 100 level từ file `.docx` vào Database. Thiết kế API lấy nội dung theo Level.
- **.NET:** Xây dựng hệ thống Login/Register và phân quyền 3 loại tài khoản.
- **Mobile:** Hoàn thiện bộ UI Kit và luồng luân chuyển màn hình.

### Tuần 3-5: MVP Real-time Audio

- **Node.js:** Tích hợp Agora SDK để xử lý luồng âm thanh đa người dùng.
- **Mobile:** Kết nối âm thanh, tính năng "Giơ tay" và "Bật/tắt mic".
- **Test:** Chạy thử phòng ẩn danh đầu tiên cho Level 1-5 (Survival Speaking).

### Tuần 6-7: Công cụ LMS cho Pro

- **Java:** Hoàn thiện tính năng ghim Slide/Tài liệu vào phòng.
- **Node.js:** Logic chuyển đổi Stage (kết thúc 10 phút tự động nhảy sang chủ đề tiếp theo).
- **Mobile:** Giao diện Dashboard cho Pro để quản lý học viên trong phòng.

### Tuần 8-9: Monetization & Podcast

- **.NET:** Hệ thống ví, cổng nạp tiền và tính năng tặng quà Real-time.
- **Node.js:** Tính năng lưu trữ Audio (Record) cho tài khoản Super.
- **Mobile:** Trang nghe lại Podcast và hiệu ứng quà tặng.

### Tuần 10: Tối ưu & Launch

- Stress test hệ thống (xử lý 500-1000 users đồng thời).
- Fix bug UI/UX và độ trễ âm thanh.
- Release bản Beta.

---

## 7. Quản lý rủi ro

| Rủi ro | Giải pháp |
|---|---|
| Độ trễ âm thanh | Sử dụng Agora thay vì tự xây dựng Server để đảm bảo ổn định quốc tế |
| Rò rỉ danh tính | Hệ thống định danh được cô lập hoàn toàn trong .NET Service, chỉ trả về Token ẩn danh cho Node.js |
| Xung đột API | Dùng Swagger làm tài liệu chung cho 5 Dev, cập nhật hàng ngày |
