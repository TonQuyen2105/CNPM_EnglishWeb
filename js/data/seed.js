/**
 * Seed Data - Khởi tạo toàn bộ dữ liệu mẫu theo chuẩn Database SQL Server
 * Tuân thủ 100% tên bảng và cấu trúc thuộc tính
 */

import { storage } from '../core/storage.js';

export function getInitialSeedData() {
  const now = new Date('2026-10-24T08:00:00Z').toISOString();
  const pastMonth = new Date('2026-09-15T08:00:00Z').toISOString();
  const pastWeek = new Date('2026-10-18T08:00:00Z').toISOString();
  const futureDate = new Date('2026-11-05T08:00:00Z').toISOString();

  // 1. users: 1 Admin, 6 Giáo viên, 40 Học sinh (vài người is_active = 0)
  const users = [
    // Admin
    {
      id: 1,
      email: 'admin@el.com',
      password: '123456',
      full_name: 'Nguyễn Quản Trị',
      avatar_url: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150',
      role: 'admin',
      is_active: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    // 6 Giáo viên
    {
      id: 2,
      email: 'teacher@el.com',
      password: '123456',
      full_name: 'ThS. Trần Thu Hà',
      avatar_url: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150',
      role: 'teacher',
      is_active: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 3,
      email: 'john.smith@el.com',
      password: '123456',
      full_name: 'John Smith (TESOL)',
      avatar_url: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150',
      role: 'teacher',
      is_active: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 4,
      email: 'le.minh.tri@el.com',
      password: '123456',
      full_name: 'Lê Minh Trí (IELTS 8.5)',
      avatar_url: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150',
      role: 'teacher',
      is_active: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 5,
      email: 'vu.hoang.anh@el.com',
      password: '123456',
      full_name: 'Vũ Hoàng Ánh (TOEIC 990)',
      avatar_url: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150',
      role: 'teacher',
      is_active: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 6,
      email: 'sarah.connor@el.com',
      password: '123456',
      full_name: 'Sarah Connor (CELTA)',
      avatar_url: 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150',
      role: 'teacher',
      is_active: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 7,
      email: 'pham.quang.huy@el.com',
      password: '123456',
      full_name: 'Phạm Quang Huy',
      avatar_url: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150',
      role: 'teacher',
      is_active: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    }
  ];

  // 40 Học sinh (id từ 8 đến 47)
  const studentNames = [
    'Nguyễn Văn An', 'Trần Thị Bích', 'Lê Hoàng Cường', 'Phạm Minh Dũng', 'Đặng Thảo My',
    'Hoàng Quốc Bảo', 'Vũ Mai Linh', 'Bùi Đức Trọng', 'Đỗ Thùy Trang', 'Hồ Tấn Phát',
    'Ngô Bảo Châu', 'Dương Gia Huy', 'Trương Khánh Linh', 'Lý Quốc Tuấn', 'Võ Thị Hồng',
    'Phan Thanh Bình', 'Nguyễn Tiến Đạt', 'Lưu Thị Hạnh', 'Đoàn Nhật Minh', 'Cao Ngọc Ánh',
    'Tô Hải Đăng', 'Chu Đình Trọng', 'Mai Diệu Hương', 'Tạ Minh Khang', 'Đinh Phương Thảo',
    'Lương Thế Vinh', 'Bạch Ngọc Trâm', 'Thái Hoàng Long', 'Trịnh Kim Ngân', 'Quách Tuấn Du',
    'Đậu Thị Lan', 'Kiều Anh Tuấn', 'Nghiêm Xuân Mạnh', 'Ôn Gia Bảo', 'Tăng Thanh Hà',
    'Vương Đình Khang', 'Ân Thị Yến', 'Hứa Vĩ Văn', 'La Thị Bông', 'Cù Minh Tuấn'
  ];

  studentNames.forEach((name, idx) => {
    const studentId = 8 + idx;
    // 2 học sinh bị khóa tài khoản
    const isActive = (studentId === 15 || studentId === 32) ? 0 : 1;
    const email = studentId === 8 ? 'student@el.com' : `student${idx + 1}@el.com`;

    users.push({
      id: studentId,
      email,
      password: '123456',
      full_name: name,
      avatar_url: `https://images.unsplash.com/photo-${1500000000000 + idx * 100000}?w=150`,
      role: 'student',
      is_active: isActive,
      created_at: pastMonth,
      updated_at: pastMonth
    });
  });

  // 2. teachers: 6 giáo viên (có người chưa is_verified)
  const teachers = [
    {
      id: 1,
      user_id: 2,
      bio: 'Thạc sĩ Ngôn ngữ Anh tại ĐH Quốc Gia HN với hơn 8 năm giảng dạy luyện thi IELTS và Giao tiếp chuyên sâu.',
      qualification: 'MA in Applied Linguistics, IELTS 8.5, TESOL',
      is_verified: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 2,
      user_id: 3,
      bio: 'Chuyên gia bản ngữ đến từ Melbourne, Úc, chuyên trách phát âm nâng cao và phản xạ giao tiếp thương mại.',
      qualification: 'Bachelor of Education, CELTA Grade A',
      is_verified: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 3,
      user_id: 4,
      bio: 'Cựu giảng viên khoa tiếng Anh, chuyên gia luyện kỹ năng Writing & Speaking band 8.0+.',
      qualification: 'IELTS 8.5 Overall, Cử nhân Sư phạm Tiếng Anh',
      is_verified: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 4,
      user_id: 5,
      bio: 'Kinh nghiệm luyện thi TOEIC 4 kỹ năng hơn 6 năm với phương pháp tư duy sơ đồ và bẫy đề thi.',
      qualification: 'TOEIC 990/990, Chứng chỉ giảng dạy quốc tế TESOL',
      is_verified: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 5,
      user_id: 6,
      bio: 'Giáo viên bản ngữ Anh Quốc, nhiệt huyết với phương pháp phản xạ tương tác cho học sinh cơ bản và thiếu nhi.',
      qualification: 'BA in English Literature, Trinity CertTESOL',
      is_verified: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 6,
      user_id: 7,
      bio: 'Giáo viên trẻ triển vọng, đang hoàn tất hồ sơ kiểm định chuyên môn tại trung tâm.',
      qualification: 'Cử nhân Ngôn ngữ Anh, IELTS 7.5 (Đang thẩm định hồ sơ)',
      is_verified: 0, // Chưa xác minh
      created_at: pastWeek,
      updated_at: pastWeek
    }
  ];

  // 3. subjects: 6 môn
  const subjects = [
    { id: 1, name: 'Tiếng Anh Giao Tiếp', description: 'Rèn luyện phản xạ nghe nói tự nhiên, phong thái tự tin trong đời sống và công việc.', is_active: 1, created_at: pastMonth, updated_at: pastMonth },
    { id: 2, name: 'Luyện Thi IELTS', description: 'Chiến thuật làm bài và phát triển toàn diện 4 kỹ năng hướng tới band điểm mục tiêu.', is_active: 1, created_at: pastMonth, updated_at: pastMonth },
    { id: 3, name: 'Luyện Thi TOEIC', description: 'Củng cố từ vựng doanh nghiệp, ngữ pháp trọng tâm và bí quyết xử lý bài thi chuẩn xác.', is_active: 1, created_at: pastMonth, updated_at: pastMonth },
    { id: 4, name: 'Ngữ Pháp Chuyên Sâu', description: 'Hệ thống hóa toàn bộ cấu trúc câu, thì và cách ứng dụng linh hoạt trong văn viết.', is_active: 1, created_at: pastMonth, updated_at: pastMonth },
    { id: 5, name: 'Phát Âm Chuẩn Quốc Tế', description: 'Chuẩn hóa 44 âm IPA, trọng âm từ, nối âm và ngữ điệu tự nhiên như người bản xứ.', is_active: 1, created_at: pastMonth, updated_at: pastMonth },
    { id: 6, name: 'Tiếng Anh Trẻ Em & Thiếu Nhi', description: 'Chương trình trực quan qua hình ảnh và trò chơi giúp các bé tiếp thu ngôn ngữ tự nhiên.', is_active: 1, created_at: pastMonth, updated_at: pastMonth }
  ];

  // 4. courses: 12 khoá học đủ 3 level
  const courses = [
    {
      id: 1,
      subject_id: 1,
      title: 'Tiếng Anh Giao Tiếp Cơ Bản (A1 - A2)',
      description: 'Dành cho người mất gốc, làm quen với cách chào hỏi, giới thiệu bản thân và các chủ đề hàng ngày.',
      thumbnail_url: 'https://images.unsplash.com/photo-1522202176988-66273c2fd55f?w=400',
      level: 'beginner',
      price: 2500000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 2,
      subject_id: 1,
      title: 'Tiếng Anh Giao Tiếp Đột Phá (B1 - B2)',
      description: 'Thảo luận các chủ đề xã hội, thuyết trình tự tin và phản biện ý kiến linh hoạt.',
      thumbnail_url: 'https://images.unsplash.com/photo-1523240795612-9a054b0db644?w=400',
      level: 'intermediate',
      price: 3200000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 3,
      subject_id: 1,
      title: 'Tiếng Anh Thương Mại & Đàm Phán Chuyên Nghiệp',
      description: 'Làm chủ giao tiếp trong môi trường công sở đa quốc gia, đàm phán hợp đồng và viết email chuyên nghiệp.',
      thumbnail_url: 'https://images.unsplash.com/photo-1556761175-5973dc0f32e7?w=400',
      level: 'advanced',
      price: 4500000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 4,
      subject_id: 2,
      title: 'IELTS Foundation - Nền Tảng Band 4.5 - 5.5',
      description: 'Xây dựng nền tảng từ vựng học thuật, ngữ pháp cốt lõi và làm quen định dạng bài thi IELTS.',
      thumbnail_url: 'https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=400',
      level: 'beginner',
      price: 3800000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 5,
      subject_id: 2,
      title: 'IELTS Master Target 6.5 - 7.0',
      description: 'Chiến thuật nâng cao cho cả 4 kỹ năng, tối ưu hóa điểm số Task 1 và Task 2 bài thi Writing.',
      thumbnail_url: 'https://images.unsplash.com/photo-1434030216411-0b793f4b4173?w=400',
      level: 'intermediate',
      price: 5200000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 6,
      subject_id: 2,
      title: 'IELTS Advanced Expert 7.5 - 8.5',
      description: 'Chinh phục band điểm xuất sắc với tư duy phản biện cao cấp, từ vựng C1-C2 và phong thái tự nhiên.',
      thumbnail_url: 'https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=400',
      level: 'advanced',
      price: 6800000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 7,
      subject_id: 3,
      title: 'TOEIC Căn Bản Mục Tiêu 450 - 550',
      description: 'Làm quen cấu trúc Listening và Reading, mẹo tránh bẫy thời gian thi trắc nghiệm.',
      thumbnail_url: 'https://images.unsplash.com/photo-1513258496099-48168024aec0?w=400',
      level: 'beginner',
      price: 2200000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 8,
      subject_id: 3,
      title: 'TOEIC Bứt Phá 4 Kỹ Năng 750+',
      description: 'Luyện giải đề thực chiến kết hợp kỹ năng Speaking & Writing chuẩn quốc tế ETS.',
      thumbnail_url: 'https://images.unsplash.com/photo-1427504494785-3a9ca7044f45?w=400',
      level: 'intermediate',
      price: 3600000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 9,
      subject_id: 4,
      title: 'Ngữ Pháp Nền Tảng Cho Người Mới Bắt Đầu',
      description: 'Nắm vững 12 thì cơ bản, cấu trúc câu đơn - câu ghép và các loại từ thông dụng.',
      thumbnail_url: 'https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=400',
      level: 'beginner',
      price: 1800000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 10,
      subject_id: 4,
      title: 'Ngữ Pháp Học Thuật & Viết Luận Nâng Cao',
      description: 'Mệnh đề quan hệ rút gọn, đảo ngữ, câu điều kiện hỗn hợp và phong cách viết học thuật.',
      thumbnail_url: 'https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=400',
      level: 'advanced',
      price: 2900000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 11,
      subject_id: 5,
      title: 'Phát Âm IPA & Ngữ Điệu Tự Nhiên',
      description: 'Triệt tiêu ngọng âm đuôi, làm chủ nguyên âm đôi và cách luyến láy như người bản ngữ.',
      thumbnail_url: 'https://images.unsplash.com/photo-1475721027785-f74eccf877e2?w=400',
      level: 'beginner',
      price: 1900000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 12,
      subject_id: 6,
      title: 'Tiếng Anh Vui Nhộn Cho Trẻ Em (Cambridge Starters)',
      description: 'Khơi gợi niềm say mê tiếng Anh cho bé thông qua bài hát, truyện kể hoạt họa và tương tác.',
      thumbnail_url: 'https://images.unsplash.com/photo-1509062522246-3755977927d7?w=400',
      level: 'beginner',
      price: 3000000,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    }
  ];

  // 5. lessons: 12 khoá học, mỗi khoá 8-10 bài đủ các content_type
  // ['vocabulary'|'grammar'|'listening'|'reading'|'speaking'|'writing'|'quiz']
  const contentTypes = ['vocabulary', 'grammar', 'listening', 'reading', 'speaking', 'writing', 'quiz'];
  const lessons = [];
  let lessonIdCounter = 1;

  courses.forEach(c => {
    const lessonCount = 8;
    for (let i = 1; i <= lessonCount; i++) {
      const type = contentTypes[(i - 1) % contentTypes.length];
      lessons.push({
        id: lessonIdCounter++,
        course_id: c.id,
        title: `Bài ${i}: ${getLessonTitleByType(type, i, c.title)}`,
        order_index: i,
        content_type: type,
        content: `Nội dung học tập chi tiết của bài số ${i} về phần ${type.toUpperCase()}. Tài liệu bao gồm lý thuyết, ví dụ phân tích thực tế và bài tập ứng dụng.`,
        created_at: pastMonth,
        updated_at: pastMonth
      });
    }
  });

  function getLessonTitleByType(type, order, courseTitle) {
    switch (type) {
      case 'vocabulary': return 'Từ vựng trọng tâm & Ngữ cảnh ứng dụng';
      case 'grammar': return 'Cấu trúc ngữ pháp & Quy tắc phân biệt';
      case 'listening': return 'Luyện nghe phản xạ & Bắt từ khóa then chốt';
      case 'reading': return 'Kỹ năng Skimming, Scanning & Đọc hiểu nhanh';
      case 'speaking': return 'Thực hành phát âm & Đối thoại tương tác';
      case 'writing': return 'Kỹ thuật diễn đạt câu & Viết đoạn văn mạch lạc';
      case 'quiz': return 'Bài kiểm tra trắc nghiệm đánh giá tiến độ';
      default: return `Chủ đề bài học ${order}`;
    }
  }

  // 6. classrooms: 5 phòng học
  const classrooms = [
    { id: 1, name: 'Phòng Lab 101', location: 'Tầng 1 - Tòa nhà A', capacity: 25, description: 'Trang bị 25 máy tính tai nghe chuẩn quốc tế', is_active: 1, created_at: pastMonth },
    { id: 2, name: 'Phòng Hội Thảo 202', location: 'Tầng 2 - Tòa nhà A', capacity: 35, description: 'Phòng đa năng có máy chiếu 4K và micro không dây', is_active: 1, created_at: pastMonth },
    { id: 3, name: 'Phòng Giao Tiếp 301', location: 'Tầng 3 - Tòa nhà B', capacity: 18, description: 'Bàn tròn tương tác, điều hòa và bảng thông minh', is_active: 1, created_at: pastMonth },
    { id: 4, name: 'Phòng IELTS VIP 401', location: 'Tầng 4 - Tòa nhà B', capacity: 15, description: 'Không gian yên tĩnh chuyên sâu cho lớp kèm VIP', is_active: 1, created_at: pastMonth },
    { id: 5, name: 'Phòng Kids English 102', location: 'Tầng 1 - Tòa nhà B', capacity: 20, description: 'Thảm trải sàn màu sắc, đồ chơi và sách truyện hình', is_active: 1, created_at: pastMonth }
  ];

  // 7. time_slots: 6 khung giờ
  const time_slots = [
    { id: 1, name: 'Ca Sáng 1', start_time: '08:00', end_time: '09:30', description: 'Thứ 2, 4, 6 sáng', is_active: 1 },
    { id: 2, name: 'Ca Sáng 2', start_time: '09:45', end_time: '11:15', description: 'Thứ 3, 5, 7 sáng', is_active: 1 },
    { id: 3, name: 'Ca Chiều 1', start_time: '14:00', end_time: '15:30', description: 'Chiều các ngày', is_active: 1 },
    { id: 4, name: 'Ca Chiều 2', start_time: '15:45', end_time: '17:15', description: 'Chiều muộn', is_active: 1 },
    { id: 5, name: 'Ca Tối 1', start_time: '17:45', end_time: '19:15', description: 'Tối sớm', is_active: 1 },
    { id: 6, name: 'Ca Tối 2', start_time: '19:30', end_time: '21:00', description: 'Tối muộn', is_active: 1 }
  ];

  // 8. course_classes: 10 lớp đủ 4 trạng thái (upcoming, ongoing, completed, cancelled)
  const course_classes = [
    {
      id: 1,
      course_id: 1,
      class_code: 'COMM-01',
      class_name: 'Giao Tiếp Cơ Bản K01',
      start_date: '2026-09-01',
      end_date: '2026-11-15',
      max_students: 20,
      price: 2500000,
      status: 'ongoing',
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 2,
      course_id: 2,
      class_name: 'Giao Tiếp Đột Phá K02',
      class_code: 'COMM-02',
      start_date: '2026-09-10',
      end_date: '2026-11-20',
      max_students: 18,
      price: 3200000,
      status: 'ongoing',
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 3,
      course_id: 4,
      class_code: 'IELTS-F01',
      class_name: 'IELTS Nền Tảng K01',
      start_date: '2026-11-10',
      end_date: '2027-01-20',
      max_students: 16,
      price: 3800000,
      status: 'upcoming',
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 4,
      course_id: 5,
      class_code: 'IELTS-M01',
      class_name: 'IELTS Master K01',
      start_date: '2026-08-15',
      end_date: '2026-10-20',
      max_students: 15,
      price: 5200000,
      status: 'completed',
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 5,
      course_id: 6,
      class_code: 'IELTS-E01',
      class_name: 'IELTS Expert K01',
      start_date: '2026-10-01',
      end_date: '2026-12-15',
      max_students: 12,
      price: 6800000,
      status: 'ongoing',
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 6,
      course_id: 7,
      class_code: 'TOEIC-B01',
      class_name: 'TOEIC Căn Bản K01',
      start_date: '2026-07-01',
      end_date: '2026-09-15',
      max_students: 22,
      price: 2200000,
      status: 'completed',
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 7,
      course_id: 8,
      class_code: 'TOEIC-A01',
      class_name: 'TOEIC 4 Kỹ Năng K01',
      start_date: '2026-11-01',
      end_date: '2027-01-15',
      max_students: 20,
      price: 3600000,
      status: 'upcoming',
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 8,
      course_id: 9,
      class_code: 'GRAM-01',
      class_name: 'Ngữ Pháp Nền Tảng K01',
      start_date: '2026-09-05',
      end_date: '2026-11-05',
      max_students: 25,
      price: 1800000,
      status: 'ongoing',
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 9,
      course_id: 11,
      class_code: 'PRON-01',
      class_name: 'Phát Âm IPA K01 (Tạm hoãn)',
      start_date: '2026-10-15',
      end_date: '2026-12-01',
      max_students: 15,
      price: 1900000,
      status: 'cancelled',
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 10,
      course_id: 12,
      class_code: 'KIDS-01',
      class_name: 'Tiếng Anh Thiếu Nhi K01',
      start_date: '2026-11-15',
      end_date: '2027-02-15',
      max_students: 15,
      price: 3000000,
      status: 'upcoming',
      created_at: pastMonth,
      updated_at: pastMonth
    }
  ];

  // 9. class_teachers: phân công giáo viên cho lớp
  const class_teachers = [
    { id: 1, class_id: 1, teacher_id: 1, role: 'main', assigned_at: pastMonth },
    { id: 2, class_id: 1, teacher_id: 2, role: 'assistant', assigned_at: pastMonth },
    { id: 3, class_id: 2, teacher_id: 2, role: 'main', assigned_at: pastMonth },
    { id: 4, class_id: 3, teacher_id: 3, role: 'main', assigned_at: pastMonth },
    { id: 5, class_id: 4, teacher_id: 1, role: 'main', assigned_at: pastMonth },
    { id: 6, class_id: 5, teacher_id: 3, role: 'main', assigned_at: pastMonth },
    { id: 7, class_id: 6, teacher_id: 4, role: 'main', assigned_at: pastMonth },
    { id: 8, class_id: 7, teacher_id: 4, role: 'main', assigned_at: pastMonth },
    { id: 9, class_id: 8, teacher_id: 1, role: 'main', assigned_at: pastMonth },
    { id: 10, class_id: 10, teacher_id: 5, role: 'main', assigned_at: pastMonth }
  ];

  // 10. class_schedules: lịch học không trùng phòng, day_of_week 2-7, CN là 1 (hoặc 1-7)
  const class_schedules = [
    { id: 1, class_id: 1, classroom_id: 3, time_slot_id: 5, day_of_week: 2, created_at: pastMonth }, // T2 Ca tối 1 P301
    { id: 2, class_id: 1, classroom_id: 3, time_slot_id: 5, day_of_week: 4, created_at: pastMonth }, // T4 Ca tối 1 P301
    { id: 3, class_id: 2, classroom_id: 2, time_slot_id: 6, day_of_week: 3, created_at: pastMonth }, // T3 Ca tối 2 P202
    { id: 4, class_id: 2, classroom_id: 2, time_slot_id: 6, day_of_week: 5, created_at: pastMonth }, // T5 Ca tối 2 P202
    { id: 5, class_id: 3, classroom_id: 4, time_slot_id: 5, day_of_week: 2, created_at: pastMonth }, // T2 Ca tối 1 P401
    { id: 6, class_id: 3, classroom_id: 4, time_slot_id: 5, day_of_week: 6, created_at: pastMonth }, // T6 Ca tối 1 P401
    { id: 7, class_id: 5, classroom_id: 4, time_slot_id: 6, day_of_week: 3, created_at: pastMonth }, // T3 Ca tối 2 P401
    { id: 8, class_id: 5, classroom_id: 4, time_slot_id: 6, day_of_week: 5, created_at: pastMonth }, // T5 Ca tối 2 P401
    { id: 9, class_id: 8, classroom_id: 1, time_slot_id: 3, day_of_week: 2, created_at: pastMonth }, // T2 Ca chiều 1 P101
    { id: 10, class_id: 8, classroom_id: 1, time_slot_id: 3, day_of_week: 4, created_at: pastMonth }, // T4 Ca chiều 1 P101
    { id: 11, class_id: 10, classroom_id: 5, time_slot_id: 1, day_of_week: 7, created_at: pastMonth } // T7 Ca sáng 1 P102
  ];

  // 11. class_lessons: các buổi học cụ thể của lớp
  const class_lessons = [];
  let classLessonIdCounter = 1;
  // Tạo buổi cho lớp 1 (COMM-01)
  for (let i = 1; i <= 6; i++) {
    class_lessons.push({
      id: classLessonIdCounter++,
      class_id: 1,
      lesson_id: i,
      schedule_id: 1,
      teacher_id: 1,
      schedule_date: `2026-09-${10 + i * 3}`,
      status: i <= 4 ? 'completed' : 'scheduled',
      notes: i <= 4 ? 'Học sinh tiếp thu tốt, phản xạ nhanh' : '',
      created_at: pastMonth
    });
  }

  // Tạo buổi cho lớp 4 (IELTS Foundation - Teacher 1)
  for (let i = 1; i <= 6; i++) {
    class_lessons.push({
      id: classLessonIdCounter++,
      class_id: 4,
      lesson_id: i <= 5 ? i : 1,
      schedule_id: 9,
      teacher_id: 1,
      schedule_date: `2026-10-${12 + i * 2}`,
      status: i <= 2 ? 'completed' : 'scheduled',
      notes: i <= 2 ? 'Lớp làm bài tập ngữ pháp và bài đọc đầy đủ' : '',
      created_at: pastMonth
    });
  }

  // 12. enrollments: đăng ký học (40 học sinh vào các lớp)
  const enrollments = [];
  let enrollId = 1;
  // Lớp 1 có 8 học sinh
  for (let s = 8; s <= 15; s++) {
    enrollments.push({
      id: enrollId++,
      user_id: s,
      class_id: 1,
      enrolled_at: pastMonth,
      status: 'active'
    });
  }
  // Lớp 2 có 6 học sinh
  for (let s = 16; s <= 21; s++) {
    enrollments.push({
      id: enrollId++,
      user_id: s,
      class_id: 2,
      enrolled_at: pastMonth,
      status: 'active'
    });
  }
  // Lớp 3 (upcoming) có 5 học sinh
  for (let s = 22; s <= 26; s++) {
    enrollments.push({
      id: enrollId++,
      user_id: s,
      class_id: 3,
      enrolled_at: pastWeek,
      status: 'active'
    });
  }
  // Lớp 4 (completed) có 6 học sinh
  for (let s = 27; s <= 32; s++) {
    enrollments.push({
      id: enrollId++,
      user_id: s,
      class_id: 4,
      enrolled_at: '2026-08-10',
      status: 'completed'
    });
  }
  // Lớp 5 (ongoing) có 5 học sinh
  for (let s = 33; s <= 37; s++) {
    enrollments.push({
      id: enrollId++,
      user_id: s,
      class_id: 5,
      enrolled_at: pastMonth,
      status: 'active'
    });
  }
  // Học sinh demo student@el.com (id: 8) đăng ký thêm lớp 8 (Ngữ pháp)
  enrollments.push({
    id: enrollId++,
    user_id: 8,
    class_id: 8,
    enrolled_at: pastMonth,
    status: 'active'
  });

  // 13. user_progress: tiến độ học của các học sinh
  const user_progress = [];
  let progId = 1;
  // Học sinh id 8 học xong 5 bài đầu của course 1
  for (let l = 1; l <= 5; l++) {
    user_progress.push({
      id: progId++,
      user_id: 8,
      lesson_id: l,
      score: 85 + (l % 3) * 5,
      is_completed: 1,
      completed_at: pastWeek
    });
  }

  // 14. course_reviews: đánh giá khoá học
  const course_reviews = [
    { id: 1, user_id: 8, course_id: 1, rating: 5, comment: 'Khoá học rất thực tế, thầy cô nhiệt tình chỉnh sửa phát âm từng từ.', created_at: pastWeek },
    { id: 2, user_id: 9, course_id: 1, rating: 5, comment: 'Nội dung dễ hiểu, các buổi thực hành nói theo cặp rất vui và bổ ích.', created_at: pastWeek },
    { id: 3, user_id: 10, course_id: 1, rating: 4, comment: 'Giáo trình rõ ràng, ước gì có thêm nhiều bài tập nói nâng cao hơn một chút.', created_at: pastWeek },
    { id: 4, user_id: 27, course_id: 4, rating: 5, comment: 'Nhờ khoá học này mình đã tự tin thi đạt IELTS 6.0 vượt mục tiêu ban đầu!', created_at: pastMonth },
    { id: 5, user_id: 28, course_id: 4, rating: 5, comment: 'Thầy Trí dạy kỹ năng viết Writing Task 1 cực kỳ tư duy và dễ áp dụng.', created_at: pastMonth }
  ];

  // 15. flashcards: 60 từ vựng cho học sinh demo (user_id: 8) và các bạn khác
  const flashcardWords = [
    { word: 'Accomplish', phonetic: '/əˈkʌm.plɪʃ/', meaning: 'Hoàn thành, đạt được mục tiêu', example: 'She accomplished such a lot during her visit.' },
    { word: 'Beneficial', phonetic: '/ˌben.ɪˈfɪʃ.əl/', meaning: 'Có lợi, mang lại lợi ích', example: 'A good diet is beneficial to health.' },
    { word: 'Comprehensive', phonetic: '/ˌkɒm.prɪˈhen.sɪv/', meaning: 'Toàn diện, bao quát', example: 'We offer a comprehensive training program.' },
    { word: 'Demonstrate', phonetic: '/ˈdem.ən.streɪt/', meaning: 'Chứng minh, minh họa', example: 'The numbers clearly demonstrate the success.' },
    { word: 'Efficient', phonetic: '/ɪˈfɪʃ.ənt/', meaning: 'Hiệu quả, có năng suất cao', example: 'The new system is much more efficient.' },
    { word: 'Fluctuate', phonetic: '/ˈflʌk.tʃu.eɪt/', meaning: 'Dao động, biến động liên tục', example: 'Vegetable prices fluctuate according to the season.' },
    { word: 'Generate', phonetic: '/ˈdʒen.ə.reɪt/', meaning: 'Tạo ra, phát sinh', example: 'The program will generate new jobs.' },
    { word: 'Hesitate', phonetic: '/ˈhez.ɪ.teɪt/', meaning: 'Do dự, ngập ngừng', example: 'Do not hesitate to contact us if you need help.' },
    { word: 'Implement', phonetic: '/ˈɪm.plɪ.ment/', meaning: 'Thi hành, triển khai', example: 'The changes will be implemented next month.' },
    { word: 'Justify', phonetic: '/ˈdʒʌs.tɪ.faɪ/', meaning: 'Bào chữa, biện minh hợp lý', example: 'How can they justify paying such huge salaries?' },
    { word: 'Lucrative', phonetic: '/ˈluː.krə.tɪv/', meaning: 'Sinh lợi cao, béo bở', example: 'The merger proved to be very lucrative.' },
    { word: 'Motivate', phonetic: '/ˈməʊ.tɪ.veɪt/', meaning: 'Thúc đẩy, tạo động lực', example: 'Teaching methods should motivate students to learn.' },
    { word: 'Negotiate', phonetic: '/nəˈɡəʊ.ʃi.eɪt/', meaning: 'Đàm phán, thương lượng', example: 'We need to negotiate a fair agreement.' },
    { word: 'Obstacle', phonetic: '/ˈɒb.stə.kəl/', meaning: 'Trở ngại, rào cản', example: 'Fear of failure is the biggest obstacle.' },
    { word: 'Prominent', phonetic: '/ˈprɒm.ɪ.nənt/', meaning: 'Nổi bật, xuất chúng', example: 'She is a prominent figure in education.' },
    { word: 'Qualitative', phonetic: '/ˈkwɒl.ɪ.tə.tɪv/', meaning: 'Thuộc về chất lượng, định tính', example: 'There was a qualitative change in his work.' },
    { word: 'Resilient', phonetic: '/rɪˈzɪl.jənt/', meaning: 'Kiên cường, mau hồi phục', example: 'Children are remarkably resilient.' },
    { word: 'Substantial', phonetic: '/səbˈstæn.ʃəl/', meaning: 'Đáng kể, quan trọng', example: 'He won a substantial sum of money.' },
    { word: 'Tolerant', phonetic: '/ˈtɒl.ər.ənt/', meaning: 'Khoan dung, chịu đựng tốt', example: 'We must be tolerant of different opinions.' },
    { word: 'Unprecedented', phonetic: '/ʌnˈpres.ɪ.den.tɪd/', meaning: 'Chưa từng có tiền lệ', example: 'This situation is completely unprecedented.' },
    { word: 'Versatile', phonetic: '/ˈvɜː.sə.taɪl/', meaning: 'Linh hoạt, đa năng', example: 'He is a versatile actor.' },
    { word: 'Widespread', phonetic: '/ˌwaɪdˈspred/', meaning: 'Lan rộng, phổ biến', example: 'The storm caused widespread damage.' },
    { word: 'Yield', phonetic: '/jiːld/', meaning: 'Sản lượng, mang lại', example: 'The investment yielded high returns.' },
    { word: 'Zealous', phonetic: '/ˈzel.əs/', meaning: 'Sốt sắng, nhiệt tâm', example: 'He is a zealous supporter of green energy.' },
    { word: 'Ambiguous', phonetic: '/æmˈbɪɡ.ju.əs/', meaning: 'Mơ hồ, nước đôi', example: 'The contract wording was ambiguous.' },
    { word: 'Collaborate', phonetic: '/kəˈlæb.ə.reɪt/', meaning: 'Hợp tác, cộng tác', example: 'Two teams collaborated on the research.' },
    { word: 'Deteriorate', phonetic: '/dɪˈtɪə.ri.ə.reɪt/', meaning: 'Xuống cấp, xấu đi', example: 'His health began to deteriorate rapidly.' },
    { word: 'Endeavor', phonetic: '/enˈdev.ər/', meaning: 'Nỗ lực, cố gắng', example: 'They made every endeavor to finish on time.' },
    { word: 'Feasible', phonetic: '/ˈfiː.zə.bəl/', meaning: 'Khả thi, thực hiện được', example: 'Is the new project financially feasible?' },
    { word: 'Grasp', phonetic: '/ɡrɑːsp/', meaning: 'Nắm bắt sâu sắc', example: 'He has a firm grasp of the grammar rules.' }
  ];

  const flashcards = [];
  let flashcardId = 1;
  // Tạo 60 flashcard (30 từ cho user 8, lặp lại thêm 30 từ đa dạng có next_review_at sớm)
  flashcardWords.forEach((fw, idx) => {
    // 15 từ đến hạn ôn tập (ngày hôm nay hoặc quá khứ)
    const reviewDate = idx < 15 
      ? new Date('2026-10-24T00:00:00Z').toISOString() 
      : new Date('2026-10-28T00:00:00Z').toISOString();

    flashcards.push({
      id: flashcardId++,
      user_id: 8,
      word: fw.word,
      meaning: fw.meaning,
      phonetic: fw.phonetic,
      audio_url: `https://ssl.gstatic.com/dictionary/static/sounds/oxford/${fw.word.toLowerCase()}--_gb_1.mp3`,
      example_sentence: fw.example,
      next_review_at: reviewDate,
      interval_days: idx < 15 ? 1 : 3,
      created_at: pastMonth
    });
  });

  // Thêm 30 từ nữa cho kho từ vựng đa dạng
  flashcardWords.forEach((fw, idx) => {
    flashcards.push({
      id: flashcardId++,
      user_id: 9,
      word: fw.word,
      meaning: fw.meaning,
      phonetic: fw.phonetic,
      audio_url: '',
      example_sentence: fw.example,
      next_review_at: new Date('2026-10-25T00:00:00Z').toISOString(),
      interval_days: 2,
      created_at: pastMonth
    });
  });

  // 16. questions: 40 câu hỏi đủ 4 loại
  // ['multiple_choice'|'fill_blank'|'pronunciation'|'essay']
  const questions = [
    // Multiple choice (1-15)
    { id: 1, lesson_id: 1, question_text: 'Choose the correct form: She _____ English every evening.', audio_url: '', question_type: 'multiple_choice', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 2, lesson_id: 1, question_text: 'Which sentence is grammatically correct?', audio_url: '', question_type: 'multiple_choice', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 3, lesson_id: 2, question_text: 'They haven’t seen each other _____ ten years.', audio_url: '', question_type: 'multiple_choice', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 4, lesson_id: 2, question_text: 'If it rains tomorrow, we _____ at home.', audio_url: '', question_type: 'multiple_choice', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 5, lesson_id: 3, question_text: 'Choose the word with the CLOSEST meaning to "Accomplish":', audio_url: '', question_type: 'multiple_choice', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 6, lesson_id: 3, question_text: 'Neither the teacher nor the students _____ present yesterday.', audio_url: '', question_type: 'multiple_choice', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 7, lesson_id: 4, question_text: 'Listen and choose the speaker’s main purpose in the conversation.', audio_url: '', question_type: 'multiple_choice', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 8, lesson_id: 4, question_text: 'The meeting has been called _____ due to heavy rain.', audio_url: '', question_type: 'multiple_choice', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 9, lesson_id: 5, question_text: 'By the time we arrived, the train _____ already.', audio_url: '', question_type: 'multiple_choice', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 10, lesson_id: 5, question_text: 'He suggested that she _____ a doctor immediately.', audio_url: '', question_type: 'multiple_choice', default_points: 10, created_at: pastMonth, updated_at: pastMonth },

    // Fill blank (11-20)
    { id: 11, lesson_id: 1, question_text: 'Fill in the blank: The sun _____ in the east.', audio_url: '', question_type: 'fill_blank', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 12, lesson_id: 1, question_text: 'Fill in the preposition: He is interested _____ learning foreign languages.', audio_url: '', question_type: 'fill_blank', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 13, lesson_id: 2, question_text: 'Fill in the blank: She has been living in Hanoi _____ 2018.', audio_url: '', question_type: 'fill_blank', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 14, lesson_id: 2, question_text: 'Fill in the correct comparative form: This exercise is _____ (easy) than that one.', audio_url: '', question_type: 'fill_blank', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 15, lesson_id: 3, question_text: 'Fill in the modal verb: You _____ not smoke inside the classroom.', audio_url: '', question_type: 'fill_blank', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 16, lesson_id: 3, question_text: 'Fill in the blank: Despite _____ hard, he failed the final exam. (study)', audio_url: '', question_type: 'fill_blank', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 17, lesson_id: 4, question_text: 'Fill in the blank: The book _____ I bought yesterday is interesting.', audio_url: '', question_type: 'fill_blank', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 18, lesson_id: 4, question_text: 'Fill in the blank: I look forward to _____ from you soon. (hear)', audio_url: '', question_type: 'fill_blank', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 19, lesson_id: 5, question_text: 'Fill in the blank: Hard work is the key _____ success.', audio_url: '', question_type: 'fill_blank', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 20, lesson_id: 5, question_text: 'Fill in the blank: He speaks English much more _____ than his brother. (fluent)', audio_url: '', question_type: 'fill_blank', default_points: 10, created_at: pastMonth, updated_at: pastMonth },

    // Pronunciation (21-30)
    { id: 21, lesson_id: 11, question_text: 'Find the word whose underlined part is pronounced differently: cat, fat, hat, car', audio_url: '', question_type: 'pronunciation', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 22, lesson_id: 11, question_text: 'Choose the word that has a different stress pattern from the others: "record, present, produce, import', audio_url: '', question_type: 'pronunciation', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 23, lesson_id: 11, question_text: 'Which word has the stress on the third syllable: economic, photography, comfortable, beautiful', audio_url: '', question_type: 'pronunciation', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 24, lesson_id: 11, question_text: 'Listen to the audio reference and practice reading the sentence aloud: "Thoughtful leaders communicate clearly under pressure."', audio_url: 'https://ssl.gstatic.com/dictionary/static/sounds/oxford/thoughtful--_gb_1.mp3', question_type: 'pronunciation', default_points: 15, created_at: pastMonth, updated_at: pastMonth },
    { id: 25, lesson_id: 11, question_text: 'Pronounce the "-ed" sound correctly: watched, wanted, played, washed', audio_url: '', question_type: 'pronunciation', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 26, lesson_id: 11, question_text: 'Choose the word with the silent "b": debt, doubt, climb, table', audio_url: '', question_type: 'pronunciation', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 27, lesson_id: 11, question_text: 'Listen and repeat with proper sentence intonation: "Could you please pass me the salt?"', audio_url: '', question_type: 'pronunciation', default_points: 15, created_at: pastMonth, updated_at: pastMonth },
    { id: 28, lesson_id: 11, question_text: 'Which word has the vowel sound /i:/: ship, sheep, sit, fit', audio_url: '', question_type: 'pronunciation', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 29, lesson_id: 11, question_text: 'Identify the word with the stress on the 1st syllable: arrive, decide, manage, contain', audio_url: '', question_type: 'pronunciation', default_points: 10, created_at: pastMonth, updated_at: pastMonth },
    { id: 30, lesson_id: 11, question_text: 'Pronunciation test: Practice the diphthong /aɪ/ in words: like, flight, high, time', audio_url: '', question_type: 'pronunciation', default_points: 10, created_at: pastMonth, updated_at: pastMonth },

    // Essay (31-40)
    { id: 31, lesson_id: 6, question_text: 'Write a short paragraph (80-100 words) introducing your favorite hobby and why it is important to you.', audio_url: '', question_type: 'essay', default_points: 25, created_at: pastMonth, updated_at: pastMonth },
    { id: 32, lesson_id: 6, question_text: 'IELTS Writing Task 1: Summarize the main trends shown in the sales chart of tech devices from 2020 to 2025.', audio_url: '', question_type: 'essay', default_points: 30, created_at: pastMonth, updated_at: pastMonth },
    { id: 33, lesson_id: 6, question_text: 'Essay question: Some people prefer working from home while others favor working in an office. Discuss both views and give your opinion.', audio_url: '', question_type: 'essay', default_points: 30, created_at: pastMonth, updated_at: pastMonth },
    { id: 34, lesson_id: 6, question_text: 'Write a formal email requesting a leave of absence for 3 days due to personal matters.', audio_url: '', question_type: 'essay', default_points: 20, created_at: pastMonth, updated_at: pastMonth },
    { id: 35, lesson_id: 6, question_text: 'Express your thoughts: How does artificial intelligence affect English language learning today?', audio_url: '', question_type: 'essay', default_points: 25, created_at: pastMonth, updated_at: pastMonth },
    { id: 36, lesson_id: 6, question_text: 'Write an argumentative response: Should standardized exams like IELTS be required for university graduation?', audio_url: '', question_type: 'essay', default_points: 30, created_at: pastMonth, updated_at: pastMonth },
    { id: 37, lesson_id: 6, question_text: 'Describe an unforgettable trip you took with your friends or family in about 120 words.', audio_url: '', question_type: 'essay', default_points: 20, created_at: pastMonth, updated_at: pastMonth },
    { id: 38, lesson_id: 6, question_text: 'Business writing: Draft a response to an angry customer complaining about a delayed shipment.', audio_url: '', question_type: 'essay', default_points: 25, created_at: pastMonth, updated_at: pastMonth },
    { id: 39, lesson_id: 6, question_text: 'Write an opinion paragraph about the importance of environmental conservation in urban areas.', audio_url: '', question_type: 'essay', default_points: 25, created_at: pastMonth, updated_at: pastMonth },
    { id: 40, lesson_id: 6, question_text: 'Summary exercise: Read the article on renewable energy and synthesize the 3 main takeaways in 70 words.', audio_url: '', question_type: 'essay', default_points: 20, created_at: pastMonth, updated_at: pastMonth }
  ];

  // 17. question_options: Các lựa chọn cho câu trắc nghiệm (câu 1 -> 10, 21 -> 23, 25, 26, 28, 29)
  const question_options = [
    // Q1
    { id: 1, question_id: 1, option_text: 'practices', is_correct: 1 },
    { id: 2, question_id: 1, option_text: 'practice', is_correct: 0 },
    { id: 3, question_id: 1, option_text: 'practicing', is_correct: 0 },
    { id: 4, question_id: 1, option_text: 'are practicing', is_correct: 0 },
    // Q2
    { id: 5, question_id: 2, option_text: 'He don’t like coffee.', is_correct: 0 },
    { id: 6, question_id: 2, option_text: 'He doesn’t likes coffee.', is_correct: 0 },
    { id: 7, question_id: 2, option_text: 'He doesn’t like coffee.', is_correct: 1 },
    { id: 8, question_id: 2, option_text: 'He not like coffee.', is_correct: 0 },
    // Q3
    { id: 9, question_id: 3, option_text: 'since', is_correct: 0 },
    { id: 10, question_id: 3, option_text: 'for', is_correct: 1 },
    { id: 11, question_id: 3, option_text: 'in', is_correct: 0 },
    { id: 12, question_id: 3, option_text: 'during', is_correct: 0 },
    // Q4
    { id: 13, question_id: 4, option_text: 'will stay', is_correct: 1 },
    { id: 14, question_id: 4, option_text: 'stayed', is_correct: 0 },
    { id: 15, question_id: 4, option_text: 'stay', is_correct: 0 },
    { id: 16, question_id: 4, option_text: 'would stay', is_correct: 0 },
    // Q5
    { id: 17, question_id: 5, option_text: 'Achieve', is_correct: 1 },
    { id: 18, question_id: 5, option_text: 'Delay', is_correct: 0 },
    { id: 19, question_id: 5, option_text: 'Forget', is_correct: 0 },
    { id: 20, question_id: 5, option_text: 'Destroy', is_correct: 0 }
  ];

  // 18. question_answers: Đáp án mẫu cho câu fill_blank & pronunciation
  const question_answers = [
    { id: 1, question_id: 11, answer_text: 'rises', reference_audio_url: '', is_case_sensitive: 0 },
    { id: 2, question_id: 12, answer_text: 'in', reference_audio_url: '', is_case_sensitive: 0 },
    { id: 3, question_id: 13, answer_text: 'since', reference_audio_url: '', is_case_sensitive: 0 },
    { id: 4, question_id: 14, answer_text: 'easier', reference_audio_url: '', is_case_sensitive: 0 },
    { id: 5, question_id: 15, answer_text: 'must', reference_audio_url: '', is_case_sensitive: 0 },
    { id: 6, question_id: 16, answer_text: 'studying', reference_audio_url: '', is_case_sensitive: 0 },
    { id: 7, question_id: 17, answer_text: 'which', reference_audio_url: '', is_case_sensitive: 0 },
    { id: 8, question_id: 18, answer_text: 'hearing', reference_audio_url: '', is_case_sensitive: 0 },
    { id: 9, question_id: 19, answer_text: 'to', reference_audio_url: '', is_case_sensitive: 0 },
    { id: 10, question_id: 20, answer_text: 'fluently', reference_audio_url: '', is_case_sensitive: 0 }
  ];

  // 19. exams: 5 đề thi
  const exams = [
    {
      id: 1,
      course_id: 1,
      created_by_teacher_id: 1,
      title: 'Đề Thi Giữa Kỳ Tiếng Anh Giao Tiếp Cơ Bản',
      description: 'Kiểm tra từ vựng, ngữ pháp cơ bản và phản xạ hội thoại tình huống thường nhật.',
      duration_minutes: 45,
      pass_score: 50,
      max_attempts: 3,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 2,
      course_id: 4,
      created_by_teacher_id: 3,
      title: 'IELTS Mock Test Đọc - Viết Nền Tảng Band 5.0',
      description: 'Mô phỏng kỳ thi IELTS chính thức phần Reading & Writing Task 1.',
      duration_minutes: 60,
      pass_score: 60,
      max_attempts: 2,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 3,
      course_id: 7,
      created_by_teacher_id: 4,
      title: 'TOEIC Mini Test 50 Câu Tiêu Chuẩn',
      description: 'Đánh giá tốc độ phản xạ ngữ pháp và từ vựng Part 5 & Part 6.',
      duration_minutes: 40,
      pass_score: 55,
      max_attempts: null,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 4,
      course_id: 9,
      created_by_teacher_id: 1,
      title: 'Kiểm Tra Tổng Hợp 12 Thì Tiếng Anh',
      description: 'Bài kiểm tra chuyên đề nhận biết dấu hiệu và chuyển đổi câu chủ động - bị động.',
      duration_minutes: 30,
      pass_score: 70,
      max_attempts: 5,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    },
    {
      id: 5,
      course_id: 5,
      created_by_teacher_id: 1,
      title: 'IELTS Writing Essay Assessment (Chấm Thủ Công)',
      description: 'Đề thi viết luận chuyên sâu yêu cầu giáo viên trực tiếp chấm và nhận xét chi tiết.',
      duration_minutes: 50,
      pass_score: 65,
      max_attempts: 1,
      is_published: 1,
      created_at: pastMonth,
      updated_at: pastMonth
    }
  ];

  // 20. exam_questions: gắn câu hỏi vào bài thi
  const exam_questions = [
    // Exam 1
    { id: 1, exam_id: 1, question_id: 1, points: 20, order_index: 1 },
    { id: 2, exam_id: 1, question_id: 2, points: 20, order_index: 2 },
    { id: 3, exam_id: 1, question_id: 11, points: 20, order_index: 3 },
    { id: 4, exam_id: 1, question_id: 12, points: 20, order_index: 4 },
    { id: 5, exam_id: 1, question_id: 31, points: 20, order_index: 5 },
    // Exam 5 (có bài tự luận pending review)
    { id: 6, exam_id: 5, question_id: 33, points: 100, order_index: 1 }
  ];

  // 21. user_exams: lượt làm bài thi (có bài in_progress, submitted, passed, failed)
  const user_exams = [
    {
      id: 1,
      user_id: 8,
      exam_id: 1,
      attempt_number: 1,
      started_at: '2026-10-15T09:00:00Z',
      submitted_at: '2026-10-15T09:38:00Z',
      total_score: 85,
      status: 'passed'
    },
    {
      id: 2,
      user_id: 9,
      exam_id: 1,
      attempt_number: 1,
      started_at: '2026-10-16T10:00:00Z',
      submitted_at: '2026-10-16T10:44:00Z',
      total_score: 40,
      status: 'failed'
    },
    {
      id: 3,
      user_id: 8,
      exam_id: 5,
      attempt_number: 1,
      started_at: '2026-10-23T14:00:00Z',
      submitted_at: '2026-10-23T14:45:00Z',
      total_score: null, // Chưa có điểm vì chờ giáo viên duyệt
      status: 'submitted'
    },
    {
      id: 4,
      user_id: 10,
      exam_id: 1,
      attempt_number: 1,
      started_at: '2026-10-24T07:15:00Z',
      submitted_at: '2026-10-24T07:55:00Z',
      total_score: null,
      status: 'submitted'
    }
  ];

  // 22. user_exam_answers: chi tiết câu trả lời bài thi (có bài pending_review)
  const user_exam_answers = [
    // Bài 1 của user 8 (Đã chấm tự động)
    {
      id: 1,
      user_exam_id: 1,
      question_id: 1,
      selected_option_id: 1,
      text_answer: '',
      score_earned: 20,
      grading_status: 'auto_graded',
      graded_by_teacher_id: null,
      graded_at: '2026-10-15T09:38:00Z',
      feedback: 'Chính xác!'
    },
    {
      id: 2,
      user_exam_id: 1,
      question_id: 11,
      selected_option_id: null,
      text_answer: 'rises',
      score_earned: 20,
      grading_status: 'auto_graded',
      graded_by_teacher_id: null,
      graded_at: '2026-10-15T09:38:00Z',
      feedback: 'Chính xác!'
    },
    // Bài 3 của user 8 (Pending Review chờ giáo viên chấm tự luận)
    {
      id: 3,
      user_exam_id: 3,
      question_id: 33,
      selected_option_id: null,
      text_answer: 'In recent years, the debate over working remotely versus working in a conventional office has intensified. On the one hand, remote work offers flexibility and cuts commuting time. On the other hand, offices foster real-time teamwork and camaraderie. In my view, a hybrid model that blends both is the optimal solution for sustainable productivity.',
      score_earned: null,
      grading_status: 'pending_review',
      graded_by_teacher_id: null,
      graded_at: null,
      feedback: null
    },
    // Bài 4 của user 10 (Pending Review chờ giáo viên chấm tự luận)
    {
      id: 4,
      user_exam_id: 4,
      question_id: 31,
      selected_option_id: null,
      text_answer: 'My favorite hobby is reading English literature. Not only does it enrich my vocabulary, but it also helps me explore diverse cultural perspectives and creative styles. I make it a habit to read at least 30 minutes every evening before going to bed.',
      score_earned: null,
      grading_status: 'pending_review',
      graded_by_teacher_id: null,
      graded_at: null,
      feedback: null
    }
  ];

  return {
    users,
    teachers,
    subjects,
    courses,
    lessons,
    classrooms,
    time_slots,
    course_classes,
    class_teachers,
    class_schedules,
    class_lessons,
    enrollments,
    user_progress,
    course_reviews,
    flashcards,
    questions,
    question_options,
    question_answers,
    exams,
    exam_questions,
    user_exams,
    user_exam_answers
  };
}

/**
 * Khởi tạo dữ liệu vào Storage nếu chưa có hoặc khi được yêu cầu đặt lại
 * Tự động nâng cấp khi có phiên bản dữ liệu mới (db_version)
 * @param {boolean} forceReset 
 */
export function initSeedDatabase(forceReset = false) {
  const CURRENT_DB_VERSION = 2;
  const dbVersion = storage.get('db_version', 0);
  const isInitialized = storage.get('db_initialized', false);

  if (!isInitialized || forceReset || dbVersion < CURRENT_DB_VERSION) {
    const data = getInitialSeedData();
    for (const [tableName, rows] of Object.entries(data)) {
      storage.setTable(tableName, rows);
    }
    storage.set('db_initialized', true);
    storage.set('db_version', CURRENT_DB_VERSION);
    console.log('✅ Cơ sở dữ liệu mẫu đã được đồng bộ phiên bản mới nhất v' + CURRENT_DB_VERSION);
  }
}
