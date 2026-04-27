import tkinter as tk
from tkinter import filedialog, messagebox
import json
import pandas as pd
import matplotlib.pyplot as plt
import os

def process_files():
    # 1. 파일 탐색기 열기 (다중 선택 허용)
    filepaths = filedialog.askopenfilenames(
        title="로그 파일 선택 (여러 개 선택 시 평균값으로 분석됩니다)",
        filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")]
    )

    if not filepaths:
        return # 취소 누름

    all_stats = []
    all_choices = []
    all_status = []

    try:
        # 2. 선택한 모든 파일 순회하며 데이터 추출
        for path in filepaths:
            with open(path, 'r', encoding='utf-8') as f:
                data = json.load(f)
            df = pd.json_normalize(data['weeks'])
            
            # (1) 스탯 누적치 계산
            plot_cols = ['stat_delta.Anxiety', 'stat_delta.Curiosity', 'stat_delta.Obedience', 'stat_delta.Trust']
            for col in plot_cols:
                if col not in df.columns:
                    df[col] = 0
                    
            cum_stats = df[['week_index'] + plot_cols].fillna(0).copy()
            for col in plot_cols:
                cum_stats[col] = cum_stats[col].cumsum()
            all_stats.append(cum_stats)
            
            # (2) 선택지 계산
            choices = df[['week_index', 'clicked_card_semantics.Blocked', 'clicked_card_semantics.Direct']].fillna(0)
            all_choices.append(choices)
            
            # (3) 이탈률 계산 (Active=1, Dropped=0)
            df['status'] = df['ending_reached'].apply(lambda x: 0 if x else 1)
            all_status.append(df[['week_index', 'status']])

        # 3. 데이터 병합 및 평균(Mean) 계산
        final_stats = pd.concat(all_stats).groupby('week_index').mean().reset_index()
        final_choices = pd.concat(all_choices).groupby('week_index').mean().reset_index()
        final_status = pd.concat(all_status).groupby('week_index').mean().reset_index()

        # 저장할 폴더 경로 (첫 번째 파일이 있는 곳 기준)
        save_dir = os.path.dirname(filepaths[0])
        file_suffix = f"_avg_{len(filepaths)}users" if len(filepaths) > 1 else "_single"

        # --- 그래프 생성 시작 ---
        
        # 1) 스탯 선 그래프
        mapping = {
            'stat_delta.Anxiety': 'stable(-) / anxious(+) 불안',
            'stat_delta.Curiosity': 'curious(-) / cautious(+) 신중',
            'stat_delta.Obedience': 'compliant(-) / defiant(+) 반항',
            'stat_delta.Trust': 'innocent(-) / clever(+) 영민 [Trust 매핑]'
        }
        
        plt.figure(figsize=(10, 6))
        colors = ['#1f77b4', '#ff7f0e', '#2ca02c', '#d62728']
        for i, col in enumerate(plot_cols):
            plt.plot(final_stats['week_index'], final_stats[col], marker='o', label=mapping.get(col, col), color=colors[i], linewidth=2)

        plt.axhline(0, color='grey', linestyle='--', linewidth=1)
        plt.xticks(final_stats['week_index'])
        plt.xlabel('Week Index')
        plt.ylabel('Cumulative Stat Value (- to +)')
        plt.title(f'Weekly Personality Stat Changes {file_suffix.replace("_", " ").strip()}')
        plt.legend()
        plt.grid(alpha=0.3)
        plt.tight_layout()
        plt.savefig(os.path.join(save_dir, f'stat_changes{file_suffix}.png'))
        plt.close()

        # 2) 생존/이탈률 바 차트 (평균 생존률 표기)
        plt.figure(figsize=(8, 5))
        plt.bar(final_status['week_index'], final_status['status'] * 100, color='skyblue')
        plt.xticks(final_status['week_index'])
        plt.xlabel('Week Index')
        plt.ylabel('Survival Rate (%)')
        plt.title(f'Session Survival Rate per Week {file_suffix.replace("_", " ").strip()}')
        plt.ylim(0, 105)
        plt.tight_layout()
        plt.savefig(os.path.join(save_dir, f'survival_rate{file_suffix}.png'))
        plt.close()

        # 3) 선택지 성향 바 차트
        plt.figure(figsize=(10, 6))
        bar_width = 0.6
        plt.bar(final_choices['week_index'], final_choices['clicked_card_semantics.Direct'], label='Direct (Clicked)', color='mediumseagreen', width=bar_width)
        plt.bar(final_choices['week_index'], final_choices['clicked_card_semantics.Blocked'], bottom=final_choices['clicked_card_semantics.Direct'], label='Blocked (Clicked)', color='tomato', width=bar_width)

        plt.xticks(final_choices['week_index'])
        plt.xlabel('Week Index')
        plt.ylabel('Average Number of Clicks')
        plt.title(f'Weekly Choice Tendencies {file_suffix.replace("_", " ").strip()}')
        plt.legend()
        plt.grid(axis='y', alpha=0.3)
        plt.tight_layout()
        plt.savefig(os.path.join(save_dir, f'choice_tendency{file_suffix}.png'))
        plt.close()

        # 4. 성공 메시지 박스 띄우기
        messagebox.showinfo("추출 완료", f"{len(filepaths)}개의 로그 파일을 분석하여\n해당 폴더에 그래프 이미지를 저장했습니다.")

    except Exception as e:
        messagebox.showerror("오류 발생", f"파일을 처리하는 중 문제가 발생했습니다.\n\n{str(e)}")


# --- UI 구성 ---
root = tk.Tk()
root.title("Balance Data Analyzer")
root.geometry("400x200")
root.eval('tk::PlaceWindow . center') # 화면 중앙 정렬

# 라벨
label = tk.Label(root, text="시뮬레이션 로그 분석기", font=("Malgun Gothic", 16, "bold"))
label.pack(pady=(30, 10))

sub_label = tk.Label(root, text="여러 개의 파일을 선택하면 평균값으로 계산됩니다.", font=("Malgun Gothic", 10))
sub_label.pack(pady=(0, 20))

# 버튼
btn = tk.Button(root, text="로그 파일 불러오기 (.json)", font=("Malgun Gothic", 12), bg="#4CAF50", fg="white", command=process_files, width=25, height=2)
btn.pack()

root.mainloop()