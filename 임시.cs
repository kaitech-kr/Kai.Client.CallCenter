using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models
{
    /// <summary>
    /// Kai 오더 모델 (개선안)
    /// - Flags/Enum 적용
    /// - FeeCommi 중심 정산
    /// - UI 편의성 프로퍼티 포함
    /// </summary>
    public partial class TbOrder
    {
        public long KeyCode { get; set; }
        public DateTime DtRegist { get; set; }

        // [상태 관리]
        // 기존 String 대신 Enum 사용 권장 (DB 매핑은 int)
        public int OrderStateCode { get; set; } 
        // public string OrderState => ((OrderStateEnum)OrderStateCode).ToString(); // Helper

        #region 기본 운행 정보 (Basic Info)
        public string StartDongBasic { get; set; } = null!;
        public string StartDetailAddr { get; set; }
        public string DestDongBasic { get; set; } = null!;
        public string DestDetailAddr { get; set; }

        // [좌표] - 기존 int 형 유지 (호환성) 또는 double 변경 검토
        public int StartLon { get; set; }
        public int StartLat { get; set; }
        public int DestLon { get; set; }
        public int DestLat { get; set; }
        #endregion

        #region 차량 및 화물 옵션 (Flags & Enum)

        // 1. 차량 스펙 (필수, 단일선택)
        // DB 저장용
        public int CarWeightCode { get; set; } // 예: 10(1톤)
        public int CarTypeCode { get; set; }   // 예: 1(카고)
        
        // UI 표시용 Helper (읽기전용)
        public string CarSpecText => $"{((CarWts)CarWeightCode).ToDesc()} {((CarTypes)CarTypeCode).ToDesc()}";

        // 2. 차량 옵션 (Flags) - 리프트, 호로 등
        public int CarOptionFlags { get; set; }
        public string CarOptionText => ((CarOpts)CarOptionFlags).ToFlagString();

        // 3. 상/하차 작업 옵션 (Flags)
        public int StartLoadOptionFlags { get; set; }
        public int DestLoadOptionFlags { get; set; }
        
        // 4. 운행 성격 (Flags) - 긴급, 왕복, 혼적
        public int RunOptionFlags { get; set; }
        public string RunOptionText => ((RunOpts)RunOptionFlags).ToFlagString();

        #endregion

        #region 요금 및 정산 (Fee Structure)
        
        // 1. 총 운임 (화주 청구액)
        public int FeeTotal { get; set; }
        
        // 2. 중개 수수료 (Kai 수익) - 필수 저장 필드
        public int FeeCommi { get; set; }

        // 3. 기사 실지급액 (순운임) - 자동 계산
        public int FeeNet => FeeTotal - FeeCommi;

        // 4. 기타 추가/공제
        public int FeeAdd { get; set; }    // 추가금 (+)
        public int FeeDed { get; set; }    // 공제금 (-)
        public int FeeConsign { get; set; }// 탁송료/대납금
        
        #endregion

        #region 외부 앱 연동 상태 (External Apps)
        // 단순 ID + 상태 코드
        public string Insung1Id { get; set; }
        public int ExtInsung1State { get; set; } // 0:None, 1:Reg, 2:Alloc...
        
        public string Insung2Id { get; set; }
        public int ExtInsung2State { get; set; }

        public string Cargo24Id { get; set; }
        public int ExtCargo24State { get; set; }

        public string OnecallId { get; set; }
        public int ExtOnecallState { get; set; }
        
        public string ExtErrorMsg { get; set; }
        #endregion

        #region 기타 정보
        public bool Share { get; set; }
        public bool IsUrgent => (RunOptionFlags & (int)RunOpts.Urgent) != 0; // 빠른 접근용 헬퍼
        public string OrderMemo { get; set; }
        public string ImgUrl { get; set; } // 이미지 경로
        public string CancelReason { get; set; }
        #endregion
    }

    // ==========================================================
    // ENUM 정의 (별도 파일로 분리 가능)
    // ==========================================================

    public enum CarWts : int
    {
        [Description("다마스")] Damas = 1,
        [Description("라보")] Labo = 2,
        [Description("1톤")] W1_0 = 10,
        [Description("1.4톤")] W1_4 = 14,
        [Description("2.5톤")] W2_5 = 25,
        [Description("3.5톤")] W3_5 = 35,
        [Description("5톤")] W5_0 = 50,
        [Description("5톤축")] W5_0_Plus = 51,
        [Description("11톤")] W11_0 = 110,
        [Description("25톤")] W25_0 = 250
    }

    public enum CarTypes : int
    {
        [Description("카고")] Cargo = 1,
        [Description("탑")] Box = 2,
        [Description("윙바디")] Wing = 3,
        [Description("호로")] Horo = 4, // 1톤 호로는 보통 별도 취급하지만 바디로 볼 수도 있음
        [Description("냉동")] Frozen = 5,
        [Description("냉장")] Refri = 6
    }

    [Flags] // 중복 선택 가능
    public enum CarOpts : int
    {
        None = 0,
        [Description("리프트")] Lift = 1 << 0,
        [Description("무진동")] NoVib = 1 << 1,
        [Description("항온")] TempCtrl = 1 << 2,
        [Description("장축")] LongBody = 1 << 3
    }

    [Flags]
    public enum RunOpts : int
    {
        None = 0,
        [Description("긴급")] Urgent = 1 << 0,
        [Description("왕복")] Round = 1 << 1,
        [Description("예약")] Reserve = 1 << 2,
        [Description("혼적")] Mixed = 1 << 3,
        [Description("동승")] Ride = 1 << 4,
        [Description("경유")] Stopover = 1 << 5,
        [Description("익일")] Tomorrow = 1 << 6
    }
    
    [Flags]
    public enum LoadOpts : int
    {
        [Description("수작업")] Manual = 1 << 0,
        [Description("지게차")] Forklift = 1 << 1,
        [Description("호이스트")] Hoist = 1 << 2,
        [Description("사다리")] Ladder = 1 << 3,
        [Description("계단")] Stairs = 1 << 4
    }

    // ==========================================================
    // 확장 메서드 (Helper)
    // ==========================================================
    public static class EnumExtensions
    {
        // Enum의 [Description] 어트리뷰트 값을 가져옴
        public static string ToDesc(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            if (field == null) return value.ToString();
            var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return attr?.Description ?? value.ToString();
        }

        // Flags Enum을 "긴급, 왕복" 형태의 문자열로 변환
        public static string ToFlagString(this Enum value)
        {
            // 단순 ToString이면 "Urgent, Round"가 나오므로, Description 기반으로 변환 필요
            // (구현 생략 가능하지만, 실무에선 매우 유용)
            var result = new List<string>();
            foreach (Enum item in Enum.GetValues(value.GetType()))
            {
                if (Convert.ToInt32(item) == 0) continue; // None 제외
                if (value.HasFlag(item))
                {
                    result.Add(item.ToDesc());
                }
            }
            return string.Join(", ", result);
        }
    }
}
