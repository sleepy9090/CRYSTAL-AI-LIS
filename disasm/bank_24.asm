; re-c bank 24  file 0x30010  CPU $8000-$9FFF  (tools/disasm_all.py)

snd_flags = $F0
snd_stream_ofs = $F1
snd_frame_end = $F2
snd_op = $F3
snd_ptr = $F5
snd_ptr_hi = $F6
snd_tmp = $F7
snd_chn_shadow = $FB
snd_reg_dirty = $FC
snd_op_count = $FD
snd_fade_timer = $FE
snd_dmc_drum = $FF
snd_cmd = $0110
snd_music_id = $0111
snd_sfx_priority = $0112
snd_ch_flags = $0114
snd_ch_flags_1 = $0115
snd_ch_flags_3 = $0117
snd_ch_ptr_lo = $011A
snd_ch_ptr_hi = $0120
snd_ch_loop1_cnt = $013E
snd_ch_wait = $014A
snd_ch_note_len = $0150
snd_ch_octave = $0156
snd_ch_env = $015C
snd_ch_vib = $0162
snd_ch_reg0 = $0168
snd_ch_sweep = $016E
snd_ch_period_lo = $0174
snd_ch_period_hi = $017A
SQ1_VOL = $4000
SQ1_SWEEP = $4001
SQ1_LO = $4002
SQ1_HI = $4003
DMC_FREQ = $4010
DMC_RAW = $4011
DMC_START = $4012
DMC_LEN = $4013
SND_CHN = $4015

    .org $8000
SndOpcodeTable:  ; ptr16[16] stream opcode handlers indexed by byte>>4 (see REVERSE.md Sound data format)
    .dw SndOpEnd                        ; 24:8000
    .dw SndOpEnd                        ; 24:8002
    .dw SndOpNote                       ; 24:8004
    .dw SndOpOctave                     ; 24:8006
    .dw SndOpRest                       ; 24:8008
    .dw SndOpDuration                   ; 24:800A
    .dw SndOpDuration                   ; 24:800C
    .dw SndOpLegato                     ; 24:800E
    .dw SndOpVolume                     ; 24:8010
    .dw SndOpDuty                       ; 24:8012
    .dw SndOpFlow                       ; 24:8014
    .dw SndOpNoteSweep                  ; 24:8016
    .dw SndOpSweep                      ; 24:8018
    .dw SndOpNoteDetune                 ; 24:801A
    .dw SndOpEnvelope                   ; 24:801C
    .dw SndOpVibrato                    ; 24:801E
SndInit:  ; silence all: $F0=0 $0111/$0112=$FF $0114-$0119=0 SND_CHN=0; called from Reset 31:F325
    LDA #$01                            ; 8020  a9 01
    STA snd_flags                       ; 8022  85 f0
    LDA #$FF                            ; 8024  a9 ff
    STA snd_music_id                    ; 8026  8d 11 01
    STA snd_sfx_priority                ; 8029  8d 12 01
    LDA #$00                            ; 802C  a9 00
    STA snd_ch_flags                    ; 802E  8d 14 01
    STA snd_ch_flags_1                  ; 8031  8d 15 01
    STA $0116                           ; 8034  8d 16 01
    STA snd_ch_flags_3                  ; 8037  8d 17 01
    STA $0118                           ; 803A  8d 18 01
    STA $0119                           ; 803D  8d 19 01
    STA snd_chn_shadow                  ; 8040  85 fb
    STA SND_CHN                         ; 8042  8d 15 40
    STA snd_flags                       ; 8045  85 f0
    RTS                                 ; 8047  60
SndCommand:  ; cmd in $0110: $FF=none; bit7=pause toggle; <$20 music ($80E4); >=$20 SFX ($8094); sets $F0 bit0 busy
    LDA snd_flags                       ; 8048  a5 f0
    ORA #$01                            ; 804A  09 01
    STA snd_flags                       ; 804C  85 f0
    JSR sub_24_8058                     ; 804E  20 58 80
    LDA snd_flags                       ; 8051  a5 f0
    AND #$FE                            ; 8053  29 fe
    STA snd_flags                       ; 8055  85 f0
    RTS                                 ; 8057  60
sub_24_8058:
    LDA snd_cmd                         ; 8058  ad 10 01
    CMP #$FF                            ; 805B  c9 ff
    BNE loc_24_8064                     ; 805D  d0 05
    LDA #$BC                            ; 805F  a9 bc
    STA snd_fade_timer                  ; 8061  85 fe
    RTS                                 ; 8063  60
loc_24_8064:
    LDA snd_cmd                         ; 8064  ad 10 01
    BMI loc_24_8071                     ; 8067  30 08
    CMP #$20                            ; 8069  c9 20
    BCS SndStartSfx                     ; 806B  b0 27
    JMP SndStartMusic                   ; 806D  4c e4 80
loc_24_8070:
    RTS                                 ; 8070  60
loc_24_8071:
    LDA snd_music_id                    ; 8071  ad 11 01
    BMI loc_24_8070                     ; 8074  30 fa
    LDA snd_chn_shadow                  ; 8076  a5 fb
    STA SND_CHN                         ; 8078  8d 15 40
    LDA snd_flags                       ; 807B  a5 f0
    EOR #$20                            ; 807D  49 20
    STA snd_flags                       ; 807F  85 f0
    AND #$20                            ; 8081  29 20
    BEQ loc_24_8086                     ; 8083  f0 01
    RTS                                 ; 8085  60
loc_24_8086:
    LDX #$00                            ; 8086  a2 00
loc_24_8088:
    JSR SndChannelOff                   ; 8088  20 65 84
    JSR sub_24_8388                     ; 808B  20 88 83
    INX                                 ; 808E  e8
    CPX #$04                            ; 808F  e0 04
    BNE loc_24_8088                     ; 8091  d0 f5
loc_24_8093:
    RTS                                 ; 8093  60
SndStartSfx:  ; maps R7=bank 12; ptr=$9FC0+id*2 (=12:$A000+(id-$20)*2); first byte=priority vs $0112
    LDX #$07                            ; 8094  a2 07
    STX SndOpcodeTable                  ; 8096  8e 00 80
    LDX #$0C                            ; 8099  a2 0c
    STX $8001                           ; 809B  8e 01 80
    ASL A                               ; 809E  0a
    TAX                                 ; 809F  aa
    LDA $9FC0,X                         ; 80A0  bd c0 9f
    STA snd_ptr                         ; 80A3  85 f5
    LDA $9FC1,X                         ; 80A5  bd c1 9f
    STA snd_ptr_hi                      ; 80A8  85 f6
    ORA snd_ptr                         ; 80AA  05 f5
    BEQ loc_24_8093                     ; 80AC  f0 e5
    LDA snd_flags                       ; 80AE  a5 f0
    AND #$40                            ; 80B0  29 40
    BNE loc_24_8093                     ; 80B2  d0 df
    LDY #$00                            ; 80B4  a0 00
    LDA (snd_ptr),Y                     ; 80B6  b1 f5
    CMP snd_sfx_priority                ; 80B8  cd 12 01
    BEQ loc_24_80BF                     ; 80BB  f0 02
    BCS loc_24_8093                     ; 80BD  b0 d4
loc_24_80BF:
    STA snd_sfx_priority                ; 80BF  8d 12 01
    INY                                 ; 80C2  c8
    LDA #$0A                            ; 80C3  a9 0a
    STA snd_chn_shadow                  ; 80C5  85 fb
    LDX #$04                            ; 80C7  a2 04
    JSR SndInitSfxChannel               ; 80C9  20 69 81
    LDY #$03                            ; 80CC  a0 03
    JSR SndInitSfxChannel               ; 80CE  20 69 81
    LDA snd_ch_flags_1                  ; 80D1  ad 15 01
    ORA #$80                            ; 80D4  09 80
    STA snd_ch_flags_1                  ; 80D6  8d 15 01
    LDA snd_ch_flags_3                  ; 80D9  ad 17 01
    ORA #$80                            ; 80DC  09 80
    STA snd_ch_flags_3                  ; 80DE  8d 17 01
    RTS                                 ; 80E1  60
    .db $EA,$EA                         ; 24:80E2
SndStartMusic:  ; $0111=id&$7F; inits 4 channel streams via $812A
    JSR SndGetMusicHeader               ; 80E4  20 77 81
    BEQ loc_24_8102                     ; 80E7  f0 19
    LDA snd_flags                       ; 80E9  a5 f0
    BMI loc_24_8102                     ; 80EB  30 15
    JSR sub_24_8103                     ; 80ED  20 03 81
    LDX #$81                            ; 80F0  a2 81
    LDA snd_chn_shadow                  ; 80F2  a5 fb
    LSR A                               ; 80F4  4a
    LSR A                               ; 80F5  4a
    BCC loc_24_80FB                     ; 80F6  90 03
    STX snd_ch_flags_1                  ; 80F8  8e 15 01
loc_24_80FB:
    LSR A                               ; 80FB  4a
    LSR A                               ; 80FC  4a
    BCC loc_24_8102                     ; 80FD  90 03
    STX snd_ch_flags_3                  ; 80FF  8e 17 01
loc_24_8102:
    RTS                                 ; 8102  60
sub_24_8103:
    LDA #$00                            ; 8103  a9 00
    STA snd_fade_timer                  ; 8105  85 fe
    STA snd_dmc_drum                    ; 8107  85 ff
    LDA snd_cmd                         ; 8109  ad 10 01
    AND #$7F                            ; 810C  29 7f
    STA snd_music_id                    ; 810E  8d 11 01
    LDA snd_flags                       ; 8111  a5 f0
    AND #$DF                            ; 8113  29 df
    STA snd_flags                       ; 8115  85 f0
    LDY #$01                            ; 8117  a0 01
    LDX #$00                            ; 8119  a2 00
    JSR sub_24_812A                     ; 811B  20 2a 81
    LDY #$03                            ; 811E  a0 03
    JSR sub_24_812A                     ; 8120  20 2a 81
    LDY #$05                            ; 8123  a0 05
    JSR sub_24_812A                     ; 8125  20 2a 81
    LDY #$07                            ; 8128  a0 07
sub_24_812A:
    LDA (snd_ptr),Y                     ; 812A  b1 f5
    STA snd_ch_ptr_lo,X                 ; 812C  9d 1a 01
    INY                                 ; 812F  c8
    LDA (snd_ptr),Y                     ; 8130  b1 f5
SndInitChannel:  ; X=chn: $011A/$0120=stream ptr; dur=1; octave=0; regs cleared; $0114=1 (active)
    STA snd_ch_ptr_hi,X                 ; 8132  9d 20 01
    LDA #$01                            ; 8135  a9 01
    STA snd_ch_wait,X                   ; 8137  9d 4a 01
    STA snd_ch_note_len,X               ; 813A  9d 50 01
    LDA #$00                            ; 813D  a9 00
    STA snd_ch_octave,X                 ; 813F  9d 56 01
    LDY SndChnRegTable,X                ; 8142  bc ac 87
    STA SQ1_VOL,Y                       ; 8145  99 00 40
    STA SQ1_LO,Y                        ; 8148  99 02 40
    STA SQ1_HI,Y                        ; 814B  99 03 40
    STA snd_ch_reg0,X                   ; 814E  9d 68 01
    STA snd_ch_period_lo,X              ; 8151  9d 74 01
    STA snd_ch_period_hi,X              ; 8154  9d 7a 01
    LDA #$08                            ; 8157  a9 08
    STA SQ1_SWEEP,Y                     ; 8159  99 01 40
    STA snd_ch_sweep,X                  ; 815C  9d 6e 01
    LDA #$01                            ; 815F  a9 01
    STA snd_ch_flags,X                  ; 8161  9d 14 01
    JSR SndChannelOff                   ; 8164  20 65 84
    INX                                 ; 8167  e8
    RTS                                 ; 8168  60
SndInitSfxChannel:  ; like SndInitChannel but stream hi += $20 (bank 12 data assembled at $8xxx; played at $Axxx)
    LDA (snd_ptr),Y                     ; 8169  b1 f5
    STA snd_ch_ptr_lo,X                 ; 816B  9d 1a 01
    INY                                 ; 816E  c8
    CLC                                 ; 816F  18
    LDA (snd_ptr),Y                     ; 8170  b1 f5
    ADC #$20                            ; 8172  69 20
    JMP SndInitChannel                  ; 8174  4c 32 81
SndGetMusicHeader:  ; A=music id -> $F5/$F6 = SndMusicTable[id]; Z=1 if null
    ASL A                               ; 8177  0a
    TAX                                 ; 8178  aa
    LDA SndMusicTable,X                 ; 8179  bd 0c 8c
    STA snd_ptr                         ; 817C  85 f5
    LDA $8C0D,X                         ; 817E  bd 0d 8c
    STA snd_ptr_hi                      ; 8181  85 f6
    ORA snd_ptr                         ; 8183  05 f5
    RTS                                 ; 8185  60
SndTick:  ; per-frame playback; returns early if $F0 bit0 (command in progress)
    LDA snd_flags                       ; 8186  a5 f0
    LSR A                               ; 8188  4a
    BCC loc_24_818C                     ; 8189  90 01
    RTS                                 ; 818B  60
loc_24_818C:
    LDX #$00                            ; 818C  a2 00
    STX snd_op_count                    ; 818E  86 fd
    ASL A                               ; 8190  0a
    ORA #$01                            ; 8191  09 01
    STA snd_flags                       ; 8193  85 f0
    STA snd_op                          ; 8195  85 f3
    BPL loc_24_81AE                     ; 8197  10 15
    LDA snd_flags                       ; 8199  a5 f0
    AND #$DF                            ; 819B  29 df
    STA snd_flags                       ; 819D  85 f0
    LDA snd_music_id                    ; 819F  ad 11 01
    BMI loc_24_81AE                     ; 81A2  30 0a
    LDA #$FF                            ; 81A4  a9 ff
    STA snd_music_id                    ; 81A6  8d 11 01
    LDA snd_chn_shadow                  ; 81A9  a5 fb
    STA SND_CHN                         ; 81AB  8d 15 40
loc_24_81AE:
    ASL snd_op                          ; 81AE  06 f3
    BPL loc_24_81D0                     ; 81B0  10 1e
    LDA snd_sfx_priority                ; 81B2  ad 12 01
    BMI loc_24_81D0                     ; 81B5  30 19
    LDX #$FF                            ; 81B7  a2 ff
    STX snd_sfx_priority                ; 81B9  8e 12 01
    INX                                 ; 81BC  e8
    LDA snd_chn_shadow                  ; 81BD  a5 fb
    EOR #$0F                            ; 81BF  49 0f
    STA SND_CHN                         ; 81C1  8d 15 40
    STX snd_chn_shadow                  ; 81C4  86 fb
    LDX #$01                            ; 81C6  a2 01
    JSR sub_24_8388                     ; 81C8  20 88 83
    LDX #$03                            ; 81CB  a2 03
    JSR sub_24_8388                     ; 81CD  20 88 83
loc_24_81D0:
    LDA snd_music_id                    ; 81D0  ad 11 01
    BMI loc_24_81DE                     ; 81D3  30 09
    LDA snd_flags                       ; 81D5  a5 f0
    AND #$20                            ; 81D7  29 20
    BNE loc_24_81DE                     ; 81D9  d0 03
    JSR SndMusicTick                    ; 81DB  20 f3 81
loc_24_81DE:
    LDA snd_sfx_priority                ; 81DE  ad 12 01
    BMI loc_24_81EC                     ; 81E1  30 09
    LDA snd_op_count                    ; 81E3  a5 fd
    CMP #$14                            ; 81E5  c9 14
    BCS loc_24_81EC                     ; 81E7  b0 03
    JSR sub_24_8258                     ; 81E9  20 58 82
loc_24_81EC:
    LDA snd_flags                       ; 81EC  a5 f0
    AND #$FE                            ; 81EE  29 fe
    STA snd_flags                       ; 81F0  85 f0
    RTS                                 ; 81F2  60
SndMusicTick:  ; s57: music channels 0-3 tick; first reads $4015 and writes $4013=0 if the DMC is busy; then the $0113 next-music chain (web: driver.js musicTick)
    LDA SND_CHN                         ; 81F3  ad 15 40
    AND #$10                            ; 81F6  29 10
    BEQ loc_24_81FF                     ; 81F8  f0 05
    LDA #$00                            ; 81FA  a9 00
    STA DMC_LEN                         ; 81FC  8d 13 40
loc_24_81FF:
    LDX #$00                            ; 81FF  a2 00
    JSR SndChannelTick                  ; 8201  20 76 82
    LDX #$01                            ; 8204  a2 01
    JSR SndChannelTick                  ; 8206  20 76 82
    LDX #$02                            ; 8209  a2 02
    JSR SndChannelTick                  ; 820B  20 76 82
    LDX #$03                            ; 820E  a2 03
    JSR SndChannelTick                  ; 8210  20 76 82
    LDA snd_ch_flags                    ; 8213  ad 14 01
    ORA snd_ch_flags_1                  ; 8216  0d 15 01
    ORA $0116                           ; 8219  0d 16 01
    ORA snd_ch_flags_3                  ; 821C  0d 17 01
    LSR A                               ; 821F  4a
    BCS loc_24_822F                     ; 8220  b0 0d
    LDA $0113                           ; 8222  ad 13 01
    STA snd_cmd                         ; 8225  8d 10 01
    BNE loc_24_8230                     ; 8228  d0 06
loc_24_822A:
    LDA #$FF                            ; 822A  a9 ff
    STA snd_music_id                    ; 822C  8d 11 01
loc_24_822F:
    RTS                                 ; 822F  60
loc_24_8230:
    JSR SndGetMusicHeader               ; 8230  20 77 81
    BEQ loc_24_822A                     ; 8233  f0 f5
    LDY #$01                            ; 8235  a0 01
    LDX #$00                            ; 8237  a2 00
loc_24_8239:
    LDA (snd_ptr),Y                     ; 8239  b1 f5
    STA snd_ch_ptr_lo,X                 ; 823B  9d 1a 01
    INY                                 ; 823E  c8
    LDA (snd_ptr),Y                     ; 823F  b1 f5
    STA snd_ch_ptr_hi,X                 ; 8241  9d 20 01
    INY                                 ; 8244  c8
    LDA snd_ch_flags,X                  ; 8245  bd 14 01
    ORA #$01                            ; 8248  09 01
    STA snd_ch_flags,X                  ; 824A  9d 14 01
    LDA #$01                            ; 824D  a9 01
    STA snd_ch_wait,X                   ; 824F  9d 4a 01
    INX                                 ; 8252  e8
    CPX #$04                            ; 8253  e0 04
    BNE loc_24_8239                     ; 8255  d0 e2
    RTS                                 ; 8257  60
sub_24_8258:
    LDX #$07                            ; 8258  a2 07
    STX SndOpcodeTable                  ; 825A  8e 00 80
    LDX #$0C                            ; 825D  a2 0c
    STX $8001                           ; 825F  8e 01 80
    LDX #$04                            ; 8262  a2 04
    JSR SndChannelTick                  ; 8264  20 76 82
    LDX #$05                            ; 8267  a2 05
    JSR SndChannelTick                  ; 8269  20 76 82
    LDA snd_chn_shadow                  ; 826C  a5 fb
    BNE loc_24_8275                     ; 826E  d0 05
    LDA #$FF                            ; 8270  a9 ff
    STA snd_sfx_priority                ; 8272  8d 12 01
loc_24_8275:
    RTS                                 ; 8275  60
SndChannelTick:  ; X=chn 0-5: dec duration; on 0 read ops until a frame-ending op; envelope/vibrato; write regs per $FC dirty bits
    LDA snd_ch_flags,X                  ; 8276  bd 14 01
    LSR A                               ; 8279  4a
    BCC loc_24_8275                     ; 827A  90 f9
    LDY #$00                            ; 827C  a0 00
    STY snd_reg_dirty                   ; 827E  84 fc
    STY snd_frame_end                   ; 8280  84 f2
    STY snd_stream_ofs                  ; 8282  84 f1
    DEC snd_ch_wait,X                   ; 8284  de 4a 01
    BNE loc_24_82B3                     ; 8287  d0 2a
    STX $FA                             ; 8289  86 fa
    LDA snd_ch_ptr_lo,X                 ; 828B  bd 1a 01
    STA snd_ptr                         ; 828E  85 f5
    LDA snd_ch_ptr_hi,X                 ; 8290  bd 20 01
    STA snd_ptr_hi                      ; 8293  85 f6
loc_24_8295:
    JSR SndReadOp                       ; 8295  20 47 83
    LDX $FA                             ; 8298  a6 fa
    LDA snd_frame_end                   ; 829A  a5 f2
    BEQ loc_24_8295                     ; 829C  f0 f7
    LDA snd_ch_note_len,X               ; 829E  bd 50 01
    STA snd_ch_wait,X                   ; 82A1  9d 4a 01
    CLC                                 ; 82A4  18
    LDA snd_ptr                         ; 82A5  a5 f5
    ADC snd_stream_ofs                  ; 82A7  65 f1
    STA snd_ch_ptr_lo,X                 ; 82A9  9d 1a 01
    LDA #$00                            ; 82AC  a9 00
    ADC snd_ptr_hi                      ; 82AE  65 f6
    STA snd_ch_ptr_hi,X                 ; 82B0  9d 20 01
loc_24_82B3:
    LDA snd_dmc_drum                    ; 82B3  a5 ff
    ORA #$0F                            ; 82B5  09 0f
    STA SND_CHN                         ; 82B7  8d 15 40
    LDA snd_reg_dirty                   ; 82BA  a5 fc
    AND #$08                            ; 82BC  29 08
    BEQ loc_24_82D8                     ; 82BE  f0 18
    LDA snd_ch_env,X                    ; 82C0  bd 5c 01
    AND #$F0                            ; 82C3  29 f0
    STA snd_ch_env,X                    ; 82C5  9d 5c 01
    LDA snd_ch_vib,X                    ; 82C8  bd 62 01
    AND #$F8                            ; 82CB  29 f8
    STA snd_ch_vib,X                    ; 82CD  9d 62 01
    LDA snd_ch_flags,X                  ; 82D0  bd 14 01
    AND #$EF                            ; 82D3  29 ef
    STA snd_ch_flags,X                  ; 82D5  9d 14 01
loc_24_82D8:
    LDA snd_ch_flags,X                  ; 82D8  bd 14 01
    AND #$10                            ; 82DB  29 10
    BNE loc_24_82F3                     ; 82DD  d0 14
    LDA snd_ch_flags,X                  ; 82DF  bd 14 01
    AND #$40                            ; 82E2  29 40
    BEQ loc_24_82E9                     ; 82E4  f0 03
    JSR SndEnvelopeTick                 ; 82E6  20 af 84
loc_24_82E9:
    LDA snd_ch_flags,X                  ; 82E9  bd 14 01
    AND #$20                            ; 82EC  29 20
    BEQ loc_24_82F3                     ; 82EE  f0 03
    JSR SndVibratoTick                  ; 82F0  20 aa 86
loc_24_82F3:
    LDA snd_fade_timer                  ; 82F3  a5 fe
    BEQ loc_24_8307                     ; 82F5  f0 10
    AND #$3F                            ; 82F7  29 3f
    BEQ loc_24_8300                     ; 82F9  f0 05
    DEC snd_fade_timer                  ; 82FB  c6 fe
    JMP loc_24_8307                     ; 82FD  4c 07 83
loc_24_8300:
    JSR SndFadeStep                     ; 8300  20 09 86
    LDA #$BC                            ; 8303  a9 bc
    STA snd_fade_timer                  ; 8305  85 fe
loc_24_8307:
    LDA snd_ch_flags,X                  ; 8307  bd 14 01
    BMI loc_24_8346                     ; 830A  30 3a
    LDY SndChnRegTable,X                ; 830C  bc ac 87
    LSR snd_reg_dirty                   ; 830F  46 fc
    BCC loc_24_8319                     ; 8311  90 06
    LDA snd_ch_reg0,X                   ; 8313  bd 68 01
    STA SQ1_VOL,Y                       ; 8316  99 00 40
loc_24_8319:
    LSR snd_reg_dirty                   ; 8319  46 fc
    BCC loc_24_8323                     ; 831B  90 06
    LDA snd_ch_sweep,X                  ; 831D  bd 6e 01
    STA SQ1_SWEEP,Y                     ; 8320  99 01 40
loc_24_8323:
    LSR snd_reg_dirty                   ; 8323  46 fc
    BCC loc_24_832D                     ; 8325  90 06
    LDA snd_ch_period_lo,X              ; 8327  bd 74 01
    STA SQ1_LO,Y                        ; 832A  99 02 40
loc_24_832D:
    LSR snd_reg_dirty                   ; 832D  46 fc
    BCC loc_24_8346                     ; 832F  90 15
    LDA snd_ch_sweep,X                  ; 8331  bd 6e 01
    STA SQ1_SWEEP,Y                     ; 8334  99 01 40
    AND #$7F                            ; 8337  29 7f
    ORA #$08                            ; 8339  09 08
    STA snd_ch_sweep,X                  ; 833B  9d 6e 01
    LDA snd_ch_period_hi,X              ; 833E  bd 7a 01
    ORA #$08                            ; 8341  09 08
    STA SQ1_HI,Y                        ; 8343  99 03 40
loc_24_8346:
    RTS                                 ; 8346  60
SndReadOp:  ; read stream byte -> $F3; JMP (SndOpcodeTable + (byte>>4)*2)
    INC snd_op_count                    ; 8347  e6 fd
    LDY snd_stream_ofs                  ; 8349  a4 f1
    LDA (snd_ptr),Y                     ; 834B  b1 f5
    INC snd_stream_ofs                  ; 834D  e6 f1
    STA snd_op                          ; 834F  85 f3
    LSR A                               ; 8351  4a
    LSR A                               ; 8352  4a
    LSR A                               ; 8353  4a
    AND #$1E                            ; 8354  29 1e
    TAY                                 ; 8356  a8
    LDA SndOpcodeTable,Y                ; 8357  b9 00 80
    STA $F8                             ; 835A  85 f8
    LDA $8001,Y                         ; 835C  b9 01 80
    STA $F9                             ; 835F  85 f9
    JMP ($00F8)                         ; 8361  6c f8 00
SndOpEnd:  ; op $0x/$1x: channel stops; music: $0113=byte&$1F = next music id (chain/loop); sfx: release channel
    INC snd_frame_end                   ; 8364  e6 f2
    JSR SndChannelOff                   ; 8366  20 65 84
    LDA snd_ch_flags,X                  ; 8369  bd 14 01
    AND #$FE                            ; 836C  29 fe
    STA snd_ch_flags,X                  ; 836E  9d 14 01
    CPX #$04                            ; 8371  e0 04
    BCS loc_24_837D                     ; 8373  b0 08
    LDA snd_op                          ; 8375  a5 f3
    AND #$1F                            ; 8377  29 1f
    STA $0113                           ; 8379  8d 13 01
    RTS                                 ; 837C  60
loc_24_837D:
    LDA SndChnMaskTable,X               ; 837D  bd b2 87
    AND snd_chn_shadow                  ; 8380  25 fb
    STA snd_chn_shadow                  ; 8382  85 fb
    LDA SndChnTypeTable,X               ; 8384  bd a6 87
    TAX                                 ; 8387  aa
sub_24_8388:
    LDA snd_ch_flags,X                  ; 8388  bd 14 01
    BPL loc_24_8394                     ; 838B  10 07
    AND #$7F                            ; 838D  29 7f
    ORA #$10                            ; 838F  09 10
    STA snd_ch_flags,X                  ; 8391  9d 14 01
loc_24_8394:
    RTS                                 ; 8394  60
SndOpSweep:  ; op $Cx: next byte = sweep reg; x bit0=1 -> sweep off else on; ends frame
    LDA snd_op                          ; 8395  a5 f3
    LSR A                               ; 8397  4a
    BCC loc_24_83AE                     ; 8398  90 14
    INC snd_frame_end                   ; 839A  e6 f2
    LDA snd_reg_dirty                   ; 839C  a5 fc
    ORA #$02                            ; 839E  09 02
    STA snd_reg_dirty                   ; 83A0  85 fc
    LDY snd_stream_ofs                  ; 83A2  a4 f1
    INC snd_stream_ofs                  ; 83A4  e6 f1
    LDA (snd_ptr),Y                     ; 83A6  b1 f5
    AND #$00                            ; 83A8  29 00
    STA snd_ch_sweep,X                  ; 83AA  9d 6e 01
    RTS                                 ; 83AD  60
loc_24_83AE:
    INC snd_frame_end                   ; 83AE  e6 f2
    LDA snd_reg_dirty                   ; 83B0  a5 fc
    ORA #$02                            ; 83B2  09 02
    STA snd_reg_dirty                   ; 83B4  85 fc
sub_24_83B6:
    LDY snd_stream_ofs                  ; 83B6  a4 f1
    INC snd_stream_ofs                  ; 83B8  e6 f1
    LDA (snd_ptr),Y                     ; 83BA  b1 f5
    ORA #$80                            ; 83BC  09 80
    STA snd_ch_sweep,X                  ; 83BE  9d 6e 01
    RTS                                 ; 83C1  60
SndOpNoteDetune:  ; op $Dx: note + octave-dependent detune ($83DA); noise chn -> DMC drum trigger ($863B)
    LDA SndChnTypeTable,X               ; 83C2  bd a6 87
    CMP #$03                            ; 83C5  c9 03
    BNE loc_24_83CC                     ; 83C7  d0 03
    JMP SndDrumSelect                   ; 83C9  4c 3b 86
loc_24_83CC:
    LDA snd_ch_octave,X                 ; 83CC  bd 56 01
    LSR A                               ; 83CF  4a
    LSR A                               ; 83D0  4a
    LSR A                               ; 83D1  4a
    LSR A                               ; 83D2  4a
    TAY                                 ; 83D3  a8
    LDA SndDetuneTable,Y                ; 83D4  b9 da 83
    JMP loc_24_83E7                     ; 83D7  4c e7 83
SndDetuneTable:  ; u8[8] per-octave period lo add for op $Dx notes
    .db $0C,$06,$04,$03,$02,$02,$01,$01 ; 24:83DA
SndOpNoteSweep:  ; op $Bx: sweep(next byte) + note
    JSR sub_24_83B6                     ; 83E2  20 b6 83
SndOpNote:  ; op $2x: note x (hex-letter pitch) in current octave; ends frame
    LDA #$00                            ; 83E5  a9 00
loc_24_83E7:
    STA snd_tmp                         ; 83E7  85 f7
    INC snd_frame_end                   ; 83E9  e6 f2
    JSR sub_24_83F8                     ; 83EB  20 f8 83
    STA snd_ch_period_hi,X              ; 83EE  9d 7a 01
    LDA snd_reg_dirty                   ; 83F1  a5 fc
    ORA #$0C                            ; 83F3  09 0c
    STA snd_reg_dirty                   ; 83F5  85 fc
    RTS                                 ; 83F7  60
sub_24_83F8:
    LDA SndChnTypeTable,X               ; 83F8  bd a6 87
    CMP #$03                            ; 83FB  c9 03
    LDA snd_op                          ; 83FD  a5 f3
    BCC loc_24_840D                     ; 83FF  90 0c
    AND #$0F                            ; 8401  29 0f
    STA snd_ch_period_lo,X              ; 8403  9d 74 01
    JSR SndDrumTrigger                  ; 8406  20 51 86
    LDA #$00                            ; 8409  a9 00
    BEQ loc_24_8421                     ; 840B  f0 14
loc_24_840D:
    AND #$0F                            ; 840D  29 0f
    ORA snd_ch_octave,X                 ; 840F  1d 56 01
    TAY                                 ; 8412  a8
    CLC                                 ; 8413  18
    LDA SndPeriodLo,Y                   ; 8414  b9 b8 87
    ADC snd_tmp                         ; 8417  65 f7
    STA snd_ch_period_lo,X              ; 8419  9d 74 01
    LDA $8838,Y                         ; 841C  b9 38 88
    ADC #$00                            ; 841F  69 00
loc_24_8421:
    RTS                                 ; 8421  60
SndOpOctave:  ; op $3x: octave = (x-1)*$10 into $0156
    LDA snd_op                          ; 8422  a5 f3
    AND #$0F                            ; 8424  29 0f
    TAY                                 ; 8426  a8
    LDA SndOctaveTable,Y                ; 8427  b9 2d 84
    STA snd_ch_octave,X                 ; 842A  9d 56 01
SndOctaveTable:  ; octave op $3x reads $842D+x: x=1..8 -> $00-$70; x=0 reads RTS byte $60 (quirk)
    RTS                                 ; 842D  60
    .db $00,$10,$20,$30,$40,$50,$60,$70 ; 24:842E
SndOpRest:  ; op $4x: hold/rest; x=0 silences channel; else volume=x (no envelope)
    INC snd_frame_end                   ; 8436  e6 f2
    LDA snd_ch_flags,X                  ; 8438  bd 14 01
    ORA #$10                            ; 843B  09 10
    STA snd_ch_flags,X                  ; 843D  9d 14 01
    LDA snd_op                          ; 8440  a5 f3
    AND #$0F                            ; 8442  29 0f
    BEQ SndChannelOff                   ; 8444  f0 1f
    STA snd_op                          ; 8446  85 f3
    TXA                                 ; 8448  8a
    AND #$04                            ; 8449  29 04
    BNE loc_24_8457                     ; 844B  d0 0a
    TXA                                 ; 844D  8a
    AND #$01                            ; 844E  29 01
    BEQ loc_24_8457                     ; 8450  f0 05
    LDA snd_chn_shadow                  ; 8452  a5 fb
    BEQ loc_24_8457                     ; 8454  f0 01
    RTS                                 ; 8456  60
loc_24_8457:
    LDA snd_ch_reg0,X                   ; 8457  bd 68 01
    AND #$F0                            ; 845A  29 f0
    ORA snd_op                          ; 845C  05 f3
    LDY SndChnRegTable,X                ; 845E  bc ac 87
    STA SQ1_VOL,Y                       ; 8461  99 00 40
    RTS                                 ; 8464  60
SndChannelOff:  ; clear channel bit in $4015 (keeps DMC bit from $FF); skipped if chn stolen by SFX
    LDA snd_ch_flags,X                  ; 8465  bd 14 01
    BMI loc_24_847C                     ; 8468  30 12
    LDA snd_dmc_drum                    ; 846A  a5 ff
    PHA                                 ; 846C  48
    AND #$10                            ; 846D  29 10
    STA snd_dmc_drum                    ; 846F  85 ff
    LDA SndChnMaskTable,X               ; 8471  bd b2 87
    ORA snd_dmc_drum                    ; 8474  05 ff
    STA SND_CHN                         ; 8476  8d 15 40
    PLA                                 ; 8479  68
    STA snd_dmc_drum                    ; 847A  85 ff
loc_24_847C:
    RTS                                 ; 847C  60
SndOpDuration:  ; op $50-$6F: note length = SndDurationTable[byte-$50]
    SEC                                 ; 847D  38
    LDA snd_op                          ; 847E  a5 f3
    SBC #$50                            ; 8480  e9 50
    TAY                                 ; 8482  a8
    LDA SndDurationTable,Y              ; 8483  b9 b8 8a
    STA snd_ch_note_len,X               ; 8486  9d 50 01
    RTS                                 ; 8489  60
SndOpLegato:  ; op $7x: change pitch without retrigger (x=7 = tie/no-op); ends frame
    LDA #$00                            ; 848A  a9 00
    STA snd_tmp                         ; 848C  85 f7
    INC snd_frame_end                   ; 848E  e6 f2
    LDA snd_op                          ; 8490  a5 f3
    AND #$0F                            ; 8492  29 0f
    STA snd_op                          ; 8494  85 f3
    CMP #$07                            ; 8496  c9 07
    BEQ loc_24_84AE                     ; 8498  f0 14
    JSR sub_24_83F8                     ; 849A  20 f8 83
    LDY #$04                            ; 849D  a0 04
    CMP snd_ch_period_hi,X              ; 849F  dd 7a 01
    STA snd_ch_period_hi,X              ; 84A2  9d 7a 01
    BEQ loc_24_84A9                     ; 84A5  f0 02
    LDY #$0C                            ; 84A7  a0 0c
loc_24_84A9:
    TYA                                 ; 84A9  98
    ORA snd_reg_dirty                   ; 84AA  05 fc
    STA snd_reg_dirty                   ; 84AC  85 fc
loc_24_84AE:
    RTS                                 ; 84AE  60
SndEnvelopeTick:  ; per-frame envelope step: vol = SndVolumeTable[base*16+env]; entry bit4 = end/hold (fade: back up 2 steps)
    LDA snd_ch_flags,X                  ; 84AF  bd 14 01
    BMI loc_24_84F0                     ; 84B2  30 3c
loc_24_84B4:
    LDA snd_ch_env,X                    ; 84B4  bd 5c 01
    TAY                                 ; 84B7  a8
    AND #$0F                            ; 84B8  29 0f
    CMP #$0F                            ; 84BA  c9 0f
    BEQ loc_24_84DB                     ; 84BC  f0 1d
    INC snd_ch_env,X                    ; 84BE  fe 5c 01
    LDA SndChnTypeTable,X               ; 84C1  bd a6 87
    CMP #$03                            ; 84C4  c9 03
    BEQ loc_24_84CE                     ; 84C6  f0 06
    LDA SndEnvTableTonal,Y              ; 84C8  b9 b8 88
    JMP loc_24_84D1                     ; 84CB  4c d1 84
loc_24_84CE:
    LDA SndEnvTableNoise,Y              ; 84CE  b9 b8 89
loc_24_84D1:
    PHA                                 ; 84D1  48
    AND #$CF                            ; 84D2  29 cf
    STA snd_op                          ; 84D4  85 f3
    PLA                                 ; 84D6  68
    AND #$10                            ; 84D7  29 10
    BEQ loc_24_84F1                     ; 84D9  f0 16
loc_24_84DB:
    LDA snd_fade_timer                  ; 84DB  a5 fe
    BEQ loc_24_84E8                     ; 84DD  f0 09
    DEC snd_ch_env,X                    ; 84DF  de 5c 01
    DEC snd_ch_env,X                    ; 84E2  de 5c 01
    JMP loc_24_84B4                     ; 84E5  4c b4 84
loc_24_84E8:
    LDA snd_ch_env,X                    ; 84E8  bd 5c 01
    ORA #$0F                            ; 84EB  09 0f
    STA snd_ch_env,X                    ; 84ED  9d 5c 01
loc_24_84F0:
    RTS                                 ; 84F0  60
loc_24_84F1:
    LDA snd_op                          ; 84F1  a5 f3
    PHA                                 ; 84F3  48
    AND #$0F                            ; 84F4  29 0f
    STA snd_op                          ; 84F6  85 f3
    LSR snd_reg_dirty                   ; 84F8  46 fc
    ASL snd_reg_dirty                   ; 84FA  06 fc
    LDA snd_ch_reg0,X                   ; 84FC  bd 68 01
    ASL A                               ; 84FF  0a
    ASL A                               ; 8500  0a
    ASL A                               ; 8501  0a
    ASL A                               ; 8502  0a
    ORA snd_op                          ; 8503  05 f3
    TAY                                 ; 8505  a8
    PLA                                 ; 8506  68
    PHA                                 ; 8507  48
    AND #$C0                            ; 8508  29 c0
    BEQ loc_24_8522                     ; 850A  f0 16
    PLA                                 ; 850C  68
    AND #$C0                            ; 850D  29 c0
    STA snd_op                          ; 850F  85 f3
    LDA snd_ch_reg0,X                   ; 8511  bd 68 01
    AND #$30                            ; 8514  29 30
    ORA snd_op                          ; 8516  05 f3
    ORA SndVolumeTable,Y                ; 8518  19 d8 8a
    LDY SndChnRegTable,X                ; 851B  bc ac 87
    STA SQ1_VOL,Y                       ; 851E  99 00 40
    RTS                                 ; 8521  60
loc_24_8522:
    PLA                                 ; 8522  68
    LDA snd_ch_reg0,X                   ; 8523  bd 68 01
    AND #$F0                            ; 8526  29 f0
    ORA SndVolumeTable,Y                ; 8528  19 d8 8a
    LDY SndChnRegTable,X                ; 852B  bc ac 87
    STA SQ1_VOL,Y                       ; 852E  99 00 40
    RTS                                 ; 8531  60
SndOpVolume:  ; op $8x: base volume x into $0168 low nibble
    LDA snd_fade_timer                  ; 8532  a5 fe
    BNE loc_24_854C                     ; 8534  d0 16
    LDA snd_op                          ; 8536  a5 f3
    AND #$0F                            ; 8538  29 0f
    STA snd_op                          ; 853A  85 f3
    LDA snd_ch_reg0,X                   ; 853C  bd 68 01
    AND #$F0                            ; 853F  29 f0
    ORA snd_op                          ; 8541  05 f3
    STA snd_ch_reg0,X                   ; 8543  9d 68 01
    LDA #$01                            ; 8546  a9 01
    ORA snd_reg_dirty                   ; 8548  05 fc
    STA snd_reg_dirty                   ; 854A  85 fc
loc_24_854C:
    RTS                                 ; 854C  60
SndOpDuty:  ; op $9x: TRI $0168 hi nibble=x; SQ/NOISE duty=(x&6)<<5 constvol=x bit0 +$20 halt; x bit0=0 clears env flag
    LDY SndChnTypeTable,X               ; 854D  bc a6 87
    LDA snd_op                          ; 8550  a5 f3
    ASL A                               ; 8552  0a
    ASL A                               ; 8553  0a
    ASL A                               ; 8554  0a
    ASL A                               ; 8555  0a
    CPY #$02                            ; 8556  c0 02
    BEQ loc_24_8571                     ; 8558  f0 17
    PHA                                 ; 855A  48
    AND #$10                            ; 855B  29 10
    STA snd_op                          ; 855D  85 f3
    BNE loc_24_8569                     ; 855F  d0 08
    LDA snd_ch_flags,X                  ; 8561  bd 14 01
    AND #$BF                            ; 8564  29 bf
    STA snd_ch_flags,X                  ; 8566  9d 14 01
loc_24_8569:
    PLA                                 ; 8569  68
    ASL A                               ; 856A  0a
    AND #$C0                            ; 856B  29 c0
    ORA snd_op                          ; 856D  05 f3
    ORA #$20                            ; 856F  09 20
loc_24_8571:
    STA snd_op                          ; 8571  85 f3
    LDA snd_ch_reg0,X                   ; 8573  bd 68 01
    AND #$0F                            ; 8576  29 0f
    ORA snd_op                          ; 8578  05 f3
    STA snd_ch_reg0,X                   ; 857A  9d 68 01
    LDA #$01                            ; 857D  a9 01
    ORA snd_reg_dirty                   ; 857F  05 fc
    STA snd_reg_dirty                   ; 8581  85 fc
    RTS                                 ; 8583  60
SndOpFlow:  ; op $Ax: A0 lo hi=jump; A1 n=loop1 start; A2 n=loop2 start; A4=loop1 end; A3/A5+=loop2 end
    LDY snd_stream_ofs                  ; 8584  a4 f1
    LDA snd_op                          ; 8586  a5 f3
    AND #$0F                            ; 8588  29 0f
    BEQ loc_24_85C9                     ; 858A  f0 3d
    CMP #$04                            ; 858C  c9 04
    BEQ loc_24_859B                     ; 858E  f0 0b
    CMP #$01                            ; 8590  c9 01
    BEQ loc_24_85B2                     ; 8592  f0 1e
    CMP #$02                            ; 8594  c9 02
    BEQ loc_24_85AF                     ; 8596  f0 17
    JSR sub_24_85F1                     ; 8598  20 f1 85
loc_24_859B:
    DEC snd_ch_loop1_cnt,X              ; 859B  de 3e 01
    BEQ loc_24_85AE                     ; 859E  f0 0e
    LDA $0126,X                         ; 85A0  bd 26 01
    STA snd_ptr                         ; 85A3  85 f5
    LDA $0132,X                         ; 85A5  bd 32 01
    STA snd_ptr_hi                      ; 85A8  85 f6
    LDA #$00                            ; 85AA  a9 00
    STA snd_stream_ofs                  ; 85AC  85 f1
loc_24_85AE:
    RTS                                 ; 85AE  60
loc_24_85AF:
    JSR sub_24_85F1                     ; 85AF  20 f1 85
loc_24_85B2:
    LDA (snd_ptr),Y                     ; 85B2  b1 f5
    STA snd_ch_loop1_cnt,X              ; 85B4  9d 3e 01
    INC snd_stream_ofs                  ; 85B7  e6 f1
    CLC                                 ; 85B9  18
    LDA snd_stream_ofs                  ; 85BA  a5 f1
    ADC snd_ptr                         ; 85BC  65 f5
    STA $0126,X                         ; 85BE  9d 26 01
    LDA #$00                            ; 85C1  a9 00
    ADC snd_ptr_hi                      ; 85C3  65 f6
    STA $0132,X                         ; 85C5  9d 32 01
    RTS                                 ; 85C8  60
loc_24_85C9:
    LDA (snd_ptr),Y                     ; 85C9  b1 f5
    PHA                                 ; 85CB  48
    INY                                 ; 85CC  c8
    LDA (snd_ptr),Y                     ; 85CD  b1 f5
    PHA                                 ; 85CF  48
    INY                                 ; 85D0  c8
    TYA                                 ; 85D1  98
    STA $0144,X                         ; 85D2  9d 44 01
    CLC                                 ; 85D5  18
    ADC snd_ptr                         ; 85D6  65 f5
    STA $012C,X                         ; 85D8  9d 2c 01
    LDA #$00                            ; 85DB  a9 00
    STA snd_stream_ofs                  ; 85DD  85 f1
    ADC snd_ptr_hi                      ; 85DF  65 f6
    STA $0138,X                         ; 85E1  9d 38 01
    PLA                                 ; 85E4  68
    CPX #$04                            ; 85E5  e0 04
    BCC loc_24_85EB                     ; 85E7  90 02
    ADC #$1F                            ; 85E9  69 1f
loc_24_85EB:
    STA snd_ptr_hi                      ; 85EB  85 f6
    PLA                                 ; 85ED  68
    STA snd_ptr                         ; 85EE  85 f5
    RTS                                 ; 85F0  60
sub_24_85F1:
    CLC                                 ; 85F1  18
    TXA                                 ; 85F2  8a
    ADC #$06                            ; 85F3  69 06
    TAX                                 ; 85F5  aa
    RTS                                 ; 85F6  60
SndOpEnvelope:  ; op $Ex: envelope #x ($015C=x<<4) enable ($0114 bit6)
    LDA snd_op                          ; 85F7  a5 f3
    ASL A                               ; 85F9  0a
    ASL A                               ; 85FA  0a
    ASL A                               ; 85FB  0a
    ASL A                               ; 85FC  0a
    STA snd_ch_env,X                    ; 85FD  9d 5c 01
    LDA snd_ch_flags,X                  ; 8600  bd 14 01
    ORA #$40                            ; 8603  09 40
    STA snd_ch_flags,X                  ; 8605  9d 14 01
    RTS                                 ; 8608  60
SndFadeStep:  ; fade step: TRI cut; other active music chn base vol -1
    LDY #$00                            ; 8609  a0 00
loc_24_860B:
    LDA snd_ch_flags,Y                  ; 860B  b9 14 01
    AND #$01                            ; 860E  29 01
    BEQ loc_24_8634                     ; 8610  f0 22
    LDA snd_ch_reg0,Y                   ; 8612  b9 68 01
    PHA                                 ; 8615  48
    TYA                                 ; 8616  98
    SEC                                 ; 8617  38
    SBC #$02                            ; 8618  e9 02
    BNE loc_24_8626                     ; 861A  d0 0a
    PLA                                 ; 861C  68
    LDA #$00                            ; 861D  a9 00
    STA snd_ch_flags,Y                  ; 861F  99 14 01
    STA snd_ch_reg0,Y                   ; 8622  99 68 01
    RTS                                 ; 8625  60
loc_24_8626:
    PLA                                 ; 8626  68
    AND #$0F                            ; 8627  29 0f
    BEQ loc_24_8634                     ; 8629  f0 09
    LDA snd_ch_reg0,Y                   ; 862B  b9 68 01
    SEC                                 ; 862E  38
    SBC #$01                            ; 862F  e9 01
    STA snd_ch_reg0,Y                   ; 8631  99 68 01
loc_24_8634:
    INY                                 ; 8634  c8
    TYA                                 ; 8635  98
    AND #$04                            ; 8636  29 04
    BEQ loc_24_860B                     ; 8638  f0 d1
    RTS                                 ; 863A  60
SndDrumSelect:  ; op $Dx on music NOISE: x=0 drums off ($FF=0 $4013=0); else $FF=$10|x (arm DMC record x)
    TXA                                 ; 863B  8a
    AND #$04                            ; 863C  29 04
    BNE loc_24_865E                     ; 863E  d0 1e
    LDA snd_op                          ; 8640  a5 f3
    AND #$0F                            ; 8642  29 0f
    BNE loc_24_864C                     ; 8644  d0 06
    STA snd_dmc_drum                    ; 8646  85 ff
    STA DMC_LEN                         ; 8648  8d 13 40
    RTS                                 ; 864B  60
loc_24_864C:
    ORA #$10                            ; 864C  09 10
    STA snd_dmc_drum                    ; 864E  85 ff
    RTS                                 ; 8650  60
SndDrumTrigger:  ; noise note: if drum armed ($FF&$0F) restart DMC with SndDmcTable[x]
    TXA                                 ; 8651  8a
    AND #$04                            ; 8652  29 04
    BNE loc_24_865E                     ; 8654  d0 08
    LDA snd_dmc_drum                    ; 8656  a5 ff
    AND #$0F                            ; 8658  29 0f
    BNE loc_24_865F                     ; 865A  d0 03
    STA snd_dmc_drum                    ; 865C  85 ff
loc_24_865E:
    RTS                                 ; 865E  60
loc_24_865F:
    ASL A                               ; 865F  0a
    ASL A                               ; 8660  0a
    TAY                                 ; 8661  a8
    LDA #$00                            ; 8662  a9 00
    STA DMC_LEN                         ; 8664  8d 13 40
    LDA #$0F                            ; 8667  a9 0f
    STA SND_CHN                         ; 8669  8d 15 40
    LDA SndDmcTable,Y                   ; 866C  b9 d8 8b
    STA DMC_FREQ                        ; 866F  8d 10 40
    LDA $8BD9,Y                         ; 8672  b9 d9 8b
    STA DMC_RAW                         ; 8675  8d 11 40
    LDA $8BDA,Y                         ; 8678  b9 da 8b
    STA DMC_START                       ; 867B  8d 12 40
    LDA $8BDB,Y                         ; 867E  b9 db 8b
    STA DMC_LEN                         ; 8681  8d 13 40
    RTS                                 ; 8684  60
SndOpVibrato:  ; op $Fx: vibrato depth x*8 ($0162); x<8 on; x=8 off; x>8 = wait one frame
    LDA snd_op                          ; 8685  a5 f3
    AND #$0F                            ; 8687  29 0f
    ASL A                               ; 8689  0a
    ASL A                               ; 868A  0a
    ASL A                               ; 868B  0a
    STA snd_ch_vib,X                    ; 868C  9d 62 01
    CMP #$40                            ; 868F  c9 40
    BCC loc_24_86A1                     ; 8691  90 0e
    BEQ loc_24_8698                     ; 8693  f0 03
    INC snd_frame_end                   ; 8695  e6 f2
    RTS                                 ; 8697  60
loc_24_8698:
    LDA snd_ch_flags,X                  ; 8698  bd 14 01
    AND #$DF                            ; 869B  29 df
    STA snd_ch_flags,X                  ; 869D  9d 14 01
    RTS                                 ; 86A0  60
loc_24_86A1:
    LDA snd_ch_flags,X                  ; 86A1  bd 14 01
    ORA #$20                            ; 86A4  09 20
    STA snd_ch_flags,X                  ; 86A6  9d 14 01
    RTS                                 ; 86A9  60
SndVibratoTick:  ; per-frame vibrato; phase in $0162 low 3 bits; lo-period only; off-by-one index Y=oct*16+e+2
    LDA snd_ch_flags,X                  ; 86AA  bd 14 01
    BMI loc_24_8713                     ; 86AD  30 64
    AND #$40                            ; 86AF  29 40
    BEQ loc_24_86BC                     ; 86B1  f0 09
    LDA snd_ch_env,X                    ; 86B3  bd 5c 01
    AND #$0F                            ; 86B6  29 0f
    CMP #$0F                            ; 86B8  c9 0f
    BNE loc_24_8713                     ; 86BA  d0 57
loc_24_86BC:
    INC snd_op_count                    ; 86BC  e6 fd
    LDA snd_ch_vib,X                    ; 86BE  bd 62 01
    CLC                                 ; 86C1  18
    ADC #$01                            ; 86C2  69 01
    AND #$07                            ; 86C4  29 07
    STA snd_op                          ; 86C6  85 f3
    LDA snd_ch_vib,X                    ; 86C8  bd 62 01
    AND #$F8                            ; 86CB  29 f8
    ORA snd_op                          ; 86CD  05 f3
    STA snd_ch_vib,X                    ; 86CF  9d 62 01
    AND #$08                            ; 86D2  29 08
    BEQ loc_24_86D8                     ; 86D4  f0 02
    LSR snd_op                          ; 86D6  46 f3
loc_24_86D8:
    LDA snd_op                          ; 86D8  a5 f3
    AND #$03                            ; 86DA  29 03
    STA snd_op                          ; 86DC  85 f3
    LDA snd_ch_vib,X                    ; 86DE  bd 62 01
    LSR A                               ; 86E1  4a
    LSR A                               ; 86E2  4a
    AND #$FC                            ; 86E3  29 fc
    ORA snd_op                          ; 86E5  05 f3
    TAY                                 ; 86E7  a8
    LDA SndVibStepTable,Y               ; 86E8  b9 14 87
    BEQ loc_24_86F6                     ; 86EB  f0 09
    CLC                                 ; 86ED  18
    ADC #$FE                            ; 86EE  69 fe
    ORA snd_ch_octave,X                 ; 86F0  1d 56 01
    CLC                                 ; 86F3  18
    ADC #$04                            ; 86F4  69 04
loc_24_86F6:
    TAY                                 ; 86F6  a8
    CLC                                 ; 86F7  18
    LDA SndVibOffsetTable,Y             ; 86F8  b9 24 87
    ADC snd_ch_period_lo,X              ; 86FB  7d 74 01
    STA snd_op                          ; 86FE  85 f3
    LDA $8725,Y                         ; 8700  b9 25 87
    ADC snd_ch_period_hi,X              ; 8703  7d 7a 01
    CMP snd_ch_period_hi,X              ; 8706  dd 7a 01
    BNE loc_24_8713                     ; 8709  d0 08
    LDY SndChnRegTable,X                ; 870B  bc ac 87
    LDA snd_op                          ; 870E  a5 f3
    STA SQ1_LO,Y                        ; 8710  99 02 40
loc_24_8713:
    RTS                                 ; 8713  60
SndVibStepTable:  ; 16 bytes: (depth>>1)*4+phase -> offset-table byte index e (0=no offset)
    .db $00,$02,$00,$04,$00,$06,$00,$08,$00,$0A,$00,$0C,$00,$0E,$00,$10; 24:8714
SndVibOffsetTable:  ; s16 period offsets; row per octave at $8726+oct*16
    .db $00,$00,$04,$00,$FC,$FF,$08,$00,$F8,$FF,$0C,$00,$F4,$FF,$10,$00; 24:8724
    .db $F0,$FF,$02,$00,$FE,$FF,$04,$00,$FC,$FF,$06,$00,$FA,$FF,$08,$00; 24:8734
    .db $F8,$FF,$01,$00,$FF,$FF,$02,$00,$FE,$FF,$03,$00,$FD,$FF,$04,$00; 24:8744
    .db $FC,$FF,$01,$00,$FF,$FF,$01,$00,$FF,$FF,$02,$00,$FE,$FF,$02,$00; 24:8754
    .db $FE,$FF,$01,$00,$FF,$FF,$01,$00,$FF,$FF,$01,$00,$FF,$FF,$01,$00; 24:8764
    .db $FF,$FF,$01,$00,$FF,$FF,$01,$00,$FF,$FF,$01,$00,$FF,$FF,$01,$00; 24:8774
    .db $FF,$FF,$01,$00,$FF,$FF,$01,$00,$FF,$FF,$01,$00,$FF,$FF,$01,$00; 24:8784
    .db $FF,$FF,$01,$00,$FF,$FF,$01,$00,$FF,$FF,$01,$00,$FF,$FF,$01,$00; 24:8794
    .db $FF,$FF                         ; 24:87A4
SndChnTypeTable:  ; u8[6] channel -> APU type 0 1 2 3 1 3 (chn 4=sfx on SQ2 / 5=sfx on noise)
    .db $00,$01,$02,$03,$01,$03         ; 24:87A6
SndChnRegTable:  ; u8[6] channel -> $4000 reg offset 0 4 8 C 4 C
    .db $00,$04,$08,$0C,$04,$0C         ; 24:87AC
SndChnMaskTable:  ; u8[6] $4015 mask with channel bit cleared
    .db $0E,$0D,$0B,$07,$0D,$07         ; 24:87B2
SndPeriodLo:  ; u8[128] period lo indexed octave*16+note; hi at $8838
    .db $74,$4C,$9C,$00,$B8,$34,$BE,$00,$00,$00,$F8,$88,$AC,$F0,$4C,$00; 24:87B8
    .db $3A,$26,$CE,$00,$5C,$1A,$DF,$00,$00,$00,$FC,$C4,$56,$F8,$A6,$80; 24:87C8
    .db $1D,$93,$67,$00,$2E,$0D,$EF,$00,$00,$00,$FE,$E2,$AB,$7C,$53,$40; 24:87D8
    .db $8E,$C9,$B3,$00,$97,$86,$77,$00,$00,$00,$7F,$71,$D5,$BE,$A9,$A0; 24:87E8
    .db $47,$64,$59,$00,$4B,$43,$3B,$00,$00,$00,$3F,$38,$6A,$5F,$54,$50; 24:87F8
    .db $23,$32,$2C,$00,$25,$21,$1D,$00,$00,$00,$1F,$1C,$35,$2F,$2A,$28; 24:8808
    .db $11,$19,$16,$00,$12,$10,$0E,$00,$00,$00,$0F,$0E,$1A,$17,$15,$14; 24:8818
    .db $08,$0C,$0B,$00,$09,$08,$07,$00,$00,$00,$07,$07,$0D,$0B,$0A,$0A; 24:8828
    .db $04,$06,$05,$00,$04,$04,$03,$00,$00,$00,$03,$03,$06,$05,$05,$05; 24:8838
    .db $02,$03,$02,$00,$02,$02,$01,$00,$00,$00,$01,$01,$03,$02,$02,$02; 24:8848
    .db $01,$01,$01,$00,$01,$01,$00,$00,$00,$00,$00,$00,$01,$01,$01,$01; 24:8858
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 24:8868
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 24:8878
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 24:8888
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 24:8898
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 24:88A8
SndEnvTableTonal:  ; u8[16x16] volume envelopes for pulse/tri (bit4=loop/hold)
    .db $0D,$0B,$09,$07,$06,$05,$04,$03,$03,$02,$02,$01,$01,$00,$00,$FF; 24:88B8
    .db $0D,$0D,$0D,$0D,$00,$00,$08,$08,$08,$08,$FF,$00,$00,$00,$00,$FF; 24:88C8
    .db $0F,$0F,$0E,$0E,$0D,$0D,$0C,$0C,$0C,$0C,$0C,$00,$01,$02,$04,$FF; 24:88D8
    .db $01,$08,$0A,$0C,$0D,$0D,$0D,$0E,$FF,$00,$00,$00,$00,$00,$00,$FF; 24:88E8
    .db $0F,$0F,$0F,$0F,$0F,$00,$00,$00,$0A,$0A,$09,$09,$09,$09,$04,$FF; 24:88F8
    .db $A8,$0A,$0A,$0E,$0E,$0D,$0B,$0E,$0F,$0F,$0F,$FF,$00,$00,$00,$FF; 24:8908
    .db $05,$0A,$0D,$0E,$0F,$0F,$0E,$0D,$0E,$0E,$0E,$0D,$0C,$0A,$08,$FF; 24:8918
    .db $6F,$6E,$0D,$0E,$0F,$0F,$0E,$0D,$06,$06,$06,$06,$06,$06,$06,$FF; 24:8928
    .db $0E,$0F,$0A,$04,$01,$00,$FF,$00,$00,$00,$00,$00,$00,$00,$00,$FF; 24:8938
    .db $0F,$0F,$0F,$0F,$0F,$00,$00,$00,$08,$08,$08,$08,$08,$08,$04,$FF; 24:8948
    .db $0D,$0D,$0D,$00,$00,$06,$05,$04,$03,$02,$01,$00,$01,$02,$04,$FF; 24:8958
    .db $0F,$0E,$0D,$0C,$0A,$08,$06,$06,$06,$06,$06,$06,$04,$03,$00,$FF; 24:8968
    .db $0F,$0F,$0F,$02,$02,$08,$07,$06,$05,$04,$03,$02,$03,$04,$06,$FF; 24:8978
    .db $0F,$08,$04,$00,$00,$00,$FF,$00,$00,$00,$00,$00,$00,$00,$00,$FF; 24:8988
    .db $01,$04,$06,$08,$0A,$0A,$0C,$0C,$0E,$0C,$0A,$0A,$0A,$0A,$08,$FF; 24:8998
    .db $0F,$0E,$0D,$0C,$0A,$08,$06,$06,$06,$06,$06,$06,$06,$06,$06,$FF; 24:89A8
SndEnvTableNoise:  ; u8[16x16] volume envelopes for noise
    .db $0D,$0B,$09,$07,$06,$05,$04,$03,$03,$02,$02,$01,$01,$00,$00,$FF; 24:89B8
    .db $0F,$0E,$0D,$0C,$0B,$0A,$09,$08,$07,$06,$05,$04,$03,$02,$01,$FF; 24:89C8
    .db $0A,$09,$08,$07,$06,$05,$04,$03,$02,$01,$00,$00,$00,$00,$00,$FF; 24:89D8
    .db $0F,$09,$0A,$0C,$0D,$0D,$0D,$0E,$FF,$00,$00,$00,$00,$00,$00,$FF; 24:89E8
    .db $01,$0F,$0B,$0A,$09,$09,$08,$08,$08,$08,$07,$07,$07,$07,$07,$FF; 24:89F8
    .db $08,$0A,$0A,$0E,$0E,$0D,$0B,$0E,$0F,$0F,$0F,$FF,$00,$00,$00,$FF; 24:8A08
    .db $05,$0A,$0D,$0E,$0F,$0F,$0E,$0D,$0E,$0E,$0E,$0D,$0C,$0A,$08,$FF; 24:8A18
    .db $00,$0F,$07,$06,$05,$06,$06,$07,$08,$07,$09,$06,$04,$07,$0F,$FF; 24:8A28
    .db $0E,$0F,$0A,$04,$01,$00,$FF,$00,$00,$00,$00,$00,$00,$00,$00,$FF; 24:8A38
    .db $0F,$0F,$0F,$0F,$0F,$00,$00,$00,$08,$08,$08,$08,$08,$08,$04,$FF; 24:8A48
    .db $0D,$0D,$0D,$00,$00,$06,$05,$04,$03,$02,$01,$00,$01,$02,$04,$FF; 24:8A58
    .db $00,$02,$04,$06,$08,$0A,$0C,$0E,$0F,$0C,$0A,$08,$08,$06,$06,$FF; 24:8A68
    .db $0F,$0F,$0F,$02,$02,$08,$07,$06,$05,$04,$03,$02,$03,$04,$06,$FF; 24:8A78
    .db $0F,$08,$04,$00,$00,$00,$FF,$00,$00,$00,$00,$00,$00,$00,$00,$FF; 24:8A88
    .db $00,$04,$06,$08,$0A,$0C,$0E,$0C,$09,$05,$03,$01,$00,$00,$00,$FF; 24:8A98
    .db $00,$04,$06,$08,$0A,$0C,$0E,$0C,$0C,$09,$05,$03,$02,$01,$01,$FF; 24:8AA8
SndDurationTable:  ; u8[32] frames per note for ops $50-$6F
    .db $05,$40,$38,$01,$14,$02,$28,$03,$50,$04,$1E,$1C,$07,$2A,$13,$15; 24:8AB8
    .db $06,$08,$0C,$09,$18,$0A,$30,$0B,$60,$12,$24,$0D,$20,$0E,$10,$0F; 24:8AC8
SndVolumeTable:  ; u8[16x16] output vol = max(env+base-15;1) if both >0 else 0 (row base vol; col env vol)
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 24:8AD8
    .db $00,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01; 24:8AE8
    .db $00,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02; 24:8AF8
    .db $00,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$03; 24:8B08
    .db $00,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$03,$04; 24:8B18
    .db $00,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$03,$04,$05; 24:8B28
    .db $00,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$03,$04,$05,$06; 24:8B38
    .db $00,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$03,$04,$05,$06,$07; 24:8B48
    .db $00,$01,$01,$01,$01,$01,$01,$01,$01,$02,$03,$04,$05,$06,$07,$08; 24:8B58
    .db $00,$01,$01,$01,$01,$01,$01,$01,$02,$03,$04,$05,$06,$07,$08,$09; 24:8B68
    .db $00,$01,$01,$01,$01,$01,$01,$02,$03,$04,$05,$06,$07,$08,$09,$0A; 24:8B78
    .db $00,$01,$01,$01,$01,$01,$02,$03,$04,$05,$06,$07,$08,$09,$0A,$0B; 24:8B88
    .db $00,$01,$01,$01,$01,$02,$03,$04,$05,$06,$07,$08,$09,$0A,$0B,$0C; 24:8B98
    .db $00,$01,$01,$01,$02,$03,$04,$05,$06,$07,$08,$09,$0A,$0B,$0C,$0D; 24:8BA8
    .db $00,$01,$01,$02,$03,$04,$05,$06,$07,$08,$09,$0A,$0B,$0C,$0D,$0E; 24:8BB8
    .db $00,$01,$02,$03,$04,$05,$06,$07,$08,$09,$0A,$0B,$0C,$0D,$0E,$0F; 24:8BC8
SndDmcTable:  ; 4-byte DMC records (freq raw start len) for noise-channel drum triggers; 13 records all point at 31:FA00 len $3E; record 0 unreachable
    .db $0F,$00,$E8,$3E,$0F,$00,$E8,$3E,$0E,$00,$E8,$3E,$0D,$00,$E8,$3E; 24:8BD8
    .db $0C,$00,$E8,$3E,$0B,$00,$E8,$3E,$0E,$00,$E8,$3E,$06,$00,$E8,$3E; 24:8BE8
    .db $08,$2A,$E8,$3E,$09,$2A,$E8,$3E,$0A,$2A,$E8,$3E,$0B,$2A,$E8,$3E; 24:8BF8
    .db $0C,$2A,$E8,$3E                 ; 24:8C08
SndMusicTable:  ; ptr16[32] music headers for ids $00-$1F (0 = null); header = 1 byte + 4 stream ptrs (SQ1 SQ2 TRI NOISE)
    .db $4C,$8C,$61,$8C,$00,$00,$BC,$90,$B6,$91,$00,$00,$14,$93,$95,$96; 24:8C0C
    .db $8D,$98,$8B,$9A,$6D,$9C,$8E,$9E,$4B,$9F,$E5,$A0,$47,$A2,$CE,$A3; 24:8C1C
    .db $00,$00,$70,$A4,$DB,$A5,$96,$A7,$E7,$A7,$AF,$A9,$3D,$AD,$2A,$B0; 24:8C2C
    .db $55,$B2,$58,$B6,$14,$B8,$F2,$B9,$8E,$BA,$DE,$BD,$D1,$BE,$3A,$BF; 24:8C3C
    .db $01,$55,$8C,$58,$8C,$5B,$8C,$5E,$8C,$53,$40,$00,$53,$40,$00,$53; 24:8C4C
    .db $40,$00,$53,$40,$00,$01,$6A,$8C,$DC,$8D,$37,$8F,$6D,$90,$93,$89; 24:8C5C
    .db $EF,$F3,$A0,$77,$8D,$64,$34,$2C,$33,$20,$62,$26,$60,$2A,$20,$62; 24:8C6C
    .db $77,$2F,$A0,$77,$8D,$66,$2E,$64,$77,$22,$A0,$AD,$8D,$62,$40,$64; 24:8C7C
    .db $2F,$62,$2E,$A0,$AD,$8D,$62,$2F,$2E,$20,$2F,$60,$2E,$2F,$2E,$22; 24:8C8C
    .db $2E,$2F,$62,$42,$40,$64,$2F,$62,$2E,$2F,$2E,$22,$2E,$2F,$2E,$2A; 24:8C9C
    .db $20,$93,$89,$EC,$F3,$64,$33,$2A,$60,$42,$32,$26,$33,$2D,$2F,$64; 24:8CAC
    .db $2A,$20,$64,$20,$60,$32,$42,$2A,$33,$2C,$2E,$64,$20,$2F,$62,$40; 24:8CBC
    .db $2D,$60,$2E,$62,$2F,$60,$2D,$62,$42,$2D,$60,$2E,$69,$2F,$62,$40; 24:8CCC
    .db $2E,$60,$2F,$62,$20,$60,$2A,$62,$42,$20,$60,$2F,$69,$20,$93,$89; 24:8CDC
    .db $EF,$F3,$A0,$B7,$8D,$62,$40,$26,$60,$34,$2C,$62,$33,$26,$60,$2A; 24:8CEC
    .db $66,$77,$A0,$B7,$8D,$6A,$34,$2D,$60,$2C,$33,$2B,$64,$34,$2F,$2E; 24:8CFC
    .db $A0,$B7,$8D,$62,$40,$26,$60,$34,$2C,$62,$33,$26,$60,$2A,$62,$77; 24:8D0C
    .db $64,$34,$2D,$62,$2E,$A0,$C6,$8D,$62,$40,$60,$2F,$2F,$2E,$62,$2F; 24:8D1C
    .db $60,$20,$62,$77,$2F,$60,$2E,$69,$2F,$62,$40,$60,$2A,$2A,$60,$20; 24:8D2C
    .db $62,$2F,$60,$77,$2A,$62,$2A,$60,$2A,$62,$77,$60,$20,$2F,$62,$40; 24:8D3C
    .db $60,$2F,$2F,$2E,$62,$2F,$60,$20,$62,$77,$2F,$60,$2E,$69,$22,$69; 24:8D4C
    .db $2D,$60,$21,$62,$77,$2C,$77,$33,$2B,$77,$26,$62,$40,$34,$60,$2F; 24:8D5C
    .db $2F,$60,$40,$62,$2F,$60,$2F,$64,$2F,$77,$01,$62,$32,$26,$60,$33; 24:8D6C
    .db $2D,$2F,$40,$69,$2A,$62,$20,$60,$2F,$2E,$40,$69,$2D,$62,$2C,$60; 24:8D7C
    .db $2E,$2C,$64,$77,$60,$32,$26,$62,$33,$2C,$60,$32,$2A,$64,$77,$62; 24:8D8C
    .db $26,$60,$33,$2D,$2F,$40,$69,$2A,$62,$20,$60,$2F,$2E,$40,$69,$2D; 24:8D9C
    .db $A8,$60,$2D,$2E,$2D,$21,$2D,$2E,$62,$42,$A8,$62,$33,$2D,$60,$2F; 24:8DAC
    .db $2A,$40,$69,$34,$2C,$64,$33,$2B,$20,$A8,$62,$40,$60,$2F,$2F,$60; 24:8DBC
    .db $2E,$62,$2D,$60,$42,$2F,$62,$2F,$60,$2F,$62,$77,$60,$2E,$2D,$A8; 24:8DCC
    .db $93,$89,$EF,$F3,$A0,$E8,$8E,$64,$34,$DC,$33,$D0,$62,$D6,$60,$DA; 24:8DDC
    .db $D0,$62,$77,$DF,$A0,$E8,$8E,$66,$DE,$64,$77,$D2,$A0,$1E,$8F,$62; 24:8DEC
    .db $40,$64,$DF,$62,$DE,$A0,$1E,$8F,$62,$DF,$DE,$D0,$DF,$60,$DE,$DF; 24:8DFC
    .db $DE,$D2,$DE,$DF,$62,$77,$40,$64,$DF,$62,$DE,$DF,$DE,$D2,$DE,$DF; 24:8E0C
    .db $DE,$DA,$D0,$93,$89,$EC,$F3,$64,$33,$DA,$60,$40,$32,$D6,$33,$DD; 24:8E1C
    .db $DF,$64,$DA,$D0,$64,$D0,$60,$32,$40,$DA,$33,$DC,$DE,$64,$D0,$DF; 24:8E2C
    .db $62,$40,$DD,$60,$DE,$62,$DF,$60,$DD,$62,$77,$DD,$60,$DE,$69,$DF; 24:8E3C
    .db $62,$40,$DE,$60,$DF,$62,$D0,$60,$DA,$62,$77,$D0,$60,$DF,$69,$D0; 24:8E4C
    .db $93,$89,$EF,$F3,$A0,$28,$8F,$62,$40,$D6,$60,$34,$DC,$62,$33,$D6; 24:8E5C
    .db $60,$DA,$66,$77,$A0,$28,$8F,$6A,$34,$DD,$60,$DC,$33,$DB,$64,$34; 24:8E6C
    .db $DF,$DE,$A0,$28,$8F,$62,$40,$D6,$60,$34,$DC,$62,$33,$D6,$60,$DA; 24:8E7C
    .db $62,$77,$64,$34,$DD,$62,$DE,$62,$40,$60,$DF,$DF,$60,$DE,$62,$DD; 24:8E8C
    .db $60,$40,$DF,$62,$DF,$60,$DF,$62,$40,$60,$DE,$DD,$62,$40,$60,$DF; 24:8E9C
    .db $DF,$DE,$62,$DF,$60,$D0,$62,$77,$DF,$60,$DE,$69,$DF,$A0,$C6,$8D; 24:8EAC
    .db $62,$40,$60,$2D,$2D,$2C,$62,$2D,$60,$2E,$62,$77,$2D,$60,$2C,$69; 24:8EBC
    .db $33,$2B,$69,$26,$60,$2A,$62,$77,$25,$77,$20,$77,$24,$62,$40,$33; 24:8ECC
    .db $60,$2D,$2D,$60,$40,$62,$2D,$60,$2D,$66,$2D,$01,$62,$32,$D6,$60; 24:8EDC
    .db $33,$DD,$DF,$40,$69,$DA,$62,$D0,$60,$DF,$DE,$40,$69,$DD,$62,$DC; 24:8EEC
    .db $60,$DE,$DC,$64,$77,$60,$32,$D6,$62,$33,$DC,$60,$32,$DA,$64,$77; 24:8EFC
    .db $62,$D6,$60,$33,$DD,$DF,$40,$69,$DA,$62,$D0,$60,$DF,$DE,$40,$69; 24:8F0C
    .db $DD,$A8,$60,$DD,$DE,$DD,$D1,$DD,$DE,$62,$77,$A8,$62,$33,$DD,$60; 24:8F1C
    .db $DF,$DA,$40,$69,$34,$DC,$64,$33,$DB,$D0,$A8,$91,$88,$A0,$30,$90; 24:8F2C
    .db $A1,$02,$33,$2E,$34,$2E,$A4,$A1,$02,$33,$2F,$34,$2F,$A4,$A0,$30; 24:8F3C
    .db $90,$A1,$02,$32,$2E,$33,$2E,$A4,$33,$2C,$34,$2C,$32,$2B,$33,$2B; 24:8F4C
    .db $90,$8F,$60,$26,$62,$26,$60,$26,$64,$40,$A1,$02,$32,$62,$26,$33; 24:8F5C
    .db $60,$26,$26,$A4,$26,$62,$26,$60,$26,$64,$40,$32,$62,$26,$33,$60; 24:8F6C
    .db $26,$26,$32,$62,$2B,$33,$60,$2B,$2B,$34,$2C,$62,$2C,$60,$2C,$64; 24:8F7C
    .db $40,$A1,$02,$33,$62,$2C,$34,$60,$2C,$2C,$A4,$A1,$04,$33,$62,$2C; 24:8F8C
    .db $60,$2C,$2C,$A4,$60,$33,$26,$62,$26,$60,$26,$34,$2D,$62,$2D,$60; 24:8F9C
    .db $2D,$62,$33,$2D,$2D,$2C,$2C,$60,$2A,$62,$2A,$60,$2A,$34,$2C,$62; 24:8FAC
    .db $2C,$60,$2C,$62,$33,$2C,$2C,$2D,$2D,$A1,$02,$32,$26,$33,$26,$A4; 24:8FBC
    .db $32,$26,$33,$60,$26,$26,$62,$32,$26,$33,$26,$A1,$02,$33,$2C,$34; 24:8FCC
    .db $2C,$A4,$33,$2C,$34,$60,$2C,$2C,$62,$33,$2C,$2C,$91,$8F,$A1,$03; 24:8FDC
    .db $62,$A0,$5C,$90,$A4,$A1,$02,$33,$2D,$34,$2D,$A4,$33,$2C,$34,$2C; 24:8FEC
    .db $33,$21,$34,$21,$A0,$5C,$90,$33,$2D,$34,$2D,$33,$2F,$34,$2F,$A1; 24:8FFC
    .db $02,$33,$20,$34,$20,$A4,$A1,$07,$33,$26,$34,$26,$A4,$33,$2C,$34; 24:900C
    .db $2C,$A2,$03,$A1,$04,$33,$2D,$34,$2D,$A4,$A8,$64,$33,$2D,$2D,$2D; 24:901C
    .db $62,$2D,$2C,$01,$62,$A1,$02,$32,$26,$33,$26,$A4,$A1,$02,$33,$2C; 24:902C
    .db $34,$2C,$A4,$A1,$02,$32,$2A,$33,$2A,$A4,$32,$20,$33,$20,$33,$2D; 24:903C
    .db $2C,$A1,$02,$32,$26,$33,$26,$A4,$A1,$02,$33,$2C,$34,$2C,$A4,$A8; 24:904C
    .db $33,$2D,$34,$2D,$33,$2F,$34,$2F,$33,$2E,$34,$2E,$33,$22,$34,$22; 24:905C
    .db $A8,$91,$8F,$ED,$A1,$08,$D1,$80,$64,$40,$2A,$40,$2A,$A4,$A1,$03; 24:906C
    .db $D1,$8F,$60,$25,$25,$6A,$40,$D0,$62,$40,$64,$2A,$62,$2A,$A4,$A1; 24:907C
    .db $02,$D1,$60,$22,$22,$40,$22,$62,$40,$2A,$A4,$A1,$03,$D1,$82,$64; 24:908C
    .db $40,$2A,$40,$2A,$A4,$40,$2A,$40,$D0,$8F,$60,$25,$25,$25,$25,$D1; 24:909C
    .db $82,$A1,$0B,$64,$40,$2A,$40,$2A,$A4,$40,$2A,$40,$62,$2A,$2A,$01; 24:90AC
    .db $01,$C5,$90,$14,$91,$65,$91,$B3,$91,$93,$89,$E9,$F3,$A1,$02,$52; 24:90BC
    .db $33,$2A,$5B,$77,$25,$52,$2F,$5B,$77,$25,$A4,$52,$26,$5B,$77,$2A; 24:90CC
    .db $52,$24,$5B,$77,$2A,$52,$26,$5B,$77,$2A,$52,$24,$5B,$77,$2F,$6D; 24:90DC
    .db $2E,$2F,$2E,$22,$2E,$24,$25,$2E,$5D,$2A,$34,$6D,$2D,$33,$52,$2B; 24:90EC
    .db $6D,$2F,$2E,$22,$2E,$2F,$24,$25,$2E,$EA,$34,$55,$25,$65,$2A,$6D; 24:90FC
    .db $25,$20,$24,$2F,$2E,$22,$2E,$03,$93,$89,$E9,$57,$40,$A1,$02,$52; 24:910C
    .db $32,$DA,$5B,$77,$D5,$52,$DF,$5B,$77,$D5,$A4,$52,$D6,$5B,$77,$DA; 24:911C
    .db $52,$D4,$5B,$77,$DA,$52,$D6,$5B,$77,$DA,$52,$D4,$5B,$77,$DF,$6D; 24:912C
    .db $DE,$DF,$DE,$D2,$DE,$D4,$D5,$DE,$5D,$DA,$34,$6D,$DD,$33,$52,$DB; 24:913C
    .db $6D,$DF,$DE,$D2,$DE,$DF,$D4,$D5,$DE,$EA,$34,$55,$D5,$65,$DA,$6D; 24:914C
    .db $D5,$D0,$D4,$DF,$DE,$D2,$67,$DE,$03,$93,$8F,$34,$A1,$07,$6D,$2A; 24:915C
    .db $25,$2F,$2E,$A4,$2F,$2E,$25,$20,$A1,$08,$26,$2A,$24,$2F,$A4,$33; 24:916C
    .db $2B,$34,$2C,$33,$2B,$26,$96,$8F,$5B,$2B,$2E,$93,$8F,$6D,$2A,$34; 24:917C
    .db $2C,$2E,$2A,$25,$35,$2D,$2C,$34,$2B,$95,$8F,$5D,$2E,$93,$8F,$6D; 24:918C
    .db $25,$97,$8F,$52,$2B,$91,$8F,$35,$55,$22,$65,$2E,$6D,$22,$2D,$21; 24:919C
    .db $2C,$34,$2B,$26,$63,$2B,$03,$53,$40,$00,$01,$BF,$91,$2D,$92,$A0; 24:91AC
    .db $92,$11,$93,$93,$88,$E6,$F3,$A0,$00,$92,$5B,$34,$26,$2A,$25,$20; 24:91BC
    .db $63,$24,$20,$65,$25,$63,$2A,$25,$65,$20,$5B,$24,$20,$A0,$00,$92; 24:91CC
    .db $52,$33,$26,$2A,$25,$20,$63,$24,$20,$65,$25,$5B,$2A,$63,$2A,$25; 24:91DC
    .db $65,$20,$5B,$24,$63,$24,$20,$65,$25,$63,$2A,$26,$65,$2B,$5B,$34; 24:91EC
    .db $2C,$33,$2B,$04,$52,$34,$22,$63,$2D,$22,$65,$20,$63,$24,$22,$65; 24:91FC
    .db $2D,$5B,$22,$20,$24,$63,$24,$20,$65,$22,$52,$22,$63,$2D,$22,$65; 24:920C
    .db $20,$63,$24,$22,$65,$2D,$5B,$22,$35,$2C,$34,$2B,$6D,$43,$33,$20; 24:921C
    .db $A8,$93,$88,$E6,$F3,$A0,$6D,$92,$5B,$34,$2E,$22,$2D,$21,$63,$2C; 24:922C
    .db $21,$65,$2D,$63,$22,$2D,$65,$21,$5B,$2C,$33,$2B,$A0,$6D,$92,$52; 24:923C
    .db $33,$D6,$DA,$D5,$D0,$63,$2D,$22,$65,$2E,$5B,$2F,$63,$2F,$2E,$65; 24:924C
    .db $22,$5B,$2D,$63,$2D,$22,$65,$2E,$63,$2F,$24,$65,$20,$5B,$20,$20; 24:925C
    .db $04,$52,$33,$2C,$63,$32,$2B,$33,$2C,$65,$22,$63,$2D,$2C,$32,$65; 24:926C
    .db $2B,$33,$5B,$2C,$22,$2D,$63,$2D,$22,$65,$2D,$52,$2C,$63,$32,$2B; 24:927C
    .db $33,$2C,$65,$22,$63,$2D,$2C,$32,$65,$2B,$5B,$33,$2C,$20,$20,$6D; 24:928C
    .db $43,$33,$20,$A8,$91,$8F,$A2,$04,$A1,$04,$63,$33,$2C,$20,$34,$65; 24:929C
    .db $2C,$A4,$A8,$A1,$07,$63,$32,$20,$33,$21,$65,$20,$A4,$63,$32,$20; 24:92AC
    .db $33,$2D,$65,$20,$A2,$04,$A1,$04,$63,$33,$2C,$20,$34,$65,$2C,$A4; 24:92BC
    .db $A8,$63,$33,$20,$34,$21,$65,$20,$5B,$24,$63,$33,$2F,$34,$2D,$65; 24:92CC
    .db $2F,$5B,$2E,$63,$33,$2E,$26,$34,$65,$2E,$63,$22,$2E,$65,$22,$63; 24:92DC
    .db $33,$22,$2A,$34,$65,$22,$5B,$2D,$A1,$02,$5B,$33,$21,$63,$21,$21; 24:92EC
    .db $65,$21,$A4,$A1,$02,$63,$21,$21,$65,$21,$A4,$A1,$02,$63,$33,$20; 24:92FC
    .db $20,$65,$20,$A4,$04,$53,$40,$00,$01,$1D,$93,$23,$94,$10,$95,$24; 24:930C
    .db $96,$93,$89,$E4,$F2,$62,$40,$33,$2A,$2A,$20,$2A,$6A,$2A,$66,$77; 24:931C
    .db $62,$40,$2A,$2B,$34,$2C,$64,$2D,$33,$2B,$6A,$77,$60,$20,$24,$6A; 24:932C
    .db $2F,$34,$62,$2C,$66,$77,$62,$33,$40,$2A,$2A,$20,$2A,$6A,$2A,$66; 24:933C
    .db $77,$62,$40,$2A,$2B,$34,$2C,$6A,$34,$2D,$33,$62,$2B,$6A,$77,$62; 24:934C
    .db $20,$66,$34,$2E,$77,$34,$6A,$2E,$60,$2E,$2E,$66,$77,$64,$40,$62; 24:935C
    .db $22,$2E,$2F,$2E,$2D,$33,$2B,$66,$34,$2D,$77,$62,$2C,$33,$64,$2B; 24:936C
    .db $62,$2A,$40,$2A,$2B,$34,$2C,$6A,$2D,$60,$2D,$2D,$66,$77,$64,$33; 24:937C
    .db $40,$62,$2F,$2A,$34,$2C,$2E,$2D,$2C,$66,$2D,$77,$6A,$77,$93,$8A; 24:938C
    .db $EF,$F1,$62,$33,$25,$64,$2A,$2B,$62,$34,$2C,$33,$2B,$2A,$34,$2D; 24:939C
    .db $77,$64,$2C,$62,$33,$2B,$34,$2C,$33,$2B,$2A,$2B,$77,$64,$2A,$62; 24:93AC
    .db $25,$34,$2E,$2D,$2C,$2F,$77,$64,$2E,$62,$2D,$64,$2E,$24,$66,$25; 24:93BC
    .db $93,$89,$EF,$F8,$64,$33,$2A,$34,$2E,$6A,$2C,$62,$2D,$77,$64,$2D; 24:93CC
    .db $62,$33,$2A,$34,$62,$2D,$6A,$2D,$62,$33,$40,$2B,$2B,$2B,$2B,$64; 24:93DC
    .db $34,$2C,$33,$62,$2B,$60,$34,$2C,$33,$2B,$62,$2A,$77,$20,$66,$2A; 24:93EC
    .db $64,$33,$2A,$34,$2E,$6A,$2C,$62,$2D,$77,$64,$2D,$62,$33,$2A,$34; 24:93FC
    .db $62,$2D,$2D,$64,$2A,$6A,$20,$62,$20,$40,$64,$20,$62,$20,$40,$25; 24:940C
    .db $25,$25,$6E,$25,$25,$25,$06,$93,$88,$E1,$F2,$66,$32,$2A,$64,$77; 24:941C
    .db $62,$77,$20,$66,$2F,$77,$20,$77,$2F,$64,$77,$93,$88,$E4,$F2,$33; 24:942C
    .db $60,$2A,$34,$2C,$62,$2F,$93,$88,$E1,$F2,$66,$32,$2A,$64,$77,$62; 24:943C
    .db $77,$20,$66,$2F,$77,$20,$77,$93,$8A,$E4,$F2,$33,$64,$2C,$20,$34; 24:944C
    .db $2C,$20,$93,$8A,$E1,$F2,$66,$32,$2C,$64,$77,$62,$77,$31,$2B,$32; 24:945C
    .db $66,$2C,$77,$64,$40,$25,$2A,$2B,$66,$2A,$77,$66,$2F,$77,$77,$77; 24:946C
    .db $20,$77,$6A,$2E,$93,$88,$EF,$62,$33,$D5,$64,$DA,$DB,$62,$34,$DC; 24:947C
    .db $33,$DB,$DA,$34,$DD,$77,$64,$DC,$62,$33,$DB,$34,$DC,$33,$DB,$DA; 24:948C
    .db $DB,$77,$64,$DA,$62,$D5,$93,$89,$EF,$F3,$34,$2C,$33,$2B,$2A,$34; 24:949C
    .db $2D,$77,$64,$2C,$33,$62,$2B,$64,$34,$2C,$2D,$66,$2E,$93,$89,$EF; 24:94AC
    .db $F8,$64,$33,$DA,$34,$DE,$6A,$DC,$62,$DD,$77,$64,$DD,$62,$33,$DA; 24:94BC
    .db $34,$62,$DD,$6A,$DD,$62,$33,$40,$DB,$DB,$DB,$DB,$64,$34,$DC,$33; 24:94CC
    .db $62,$DB,$60,$34,$DC,$33,$DB,$62,$DA,$77,$D0,$66,$DA,$64,$33,$DA; 24:94DC
    .db $34,$DE,$6A,$DC,$62,$DD,$77,$64,$DD,$62,$33,$DA,$34,$62,$DD,$DD; 24:94EC
    .db $64,$DA,$6A,$D0,$62,$D0,$40,$64,$D0,$62,$D0,$40,$D5,$D5,$D5,$6E; 24:94FC
    .db $D5,$D5,$D5,$06,$A1,$02,$A0,$DE,$95,$A4,$A0,$F6,$95,$91,$8F,$62; 24:950C
    .db $33,$2E,$92,$8F,$64,$2F,$91,$82,$60,$2F,$2F,$95,$8F,$6A,$2F,$91; 24:951C
    .db $8F,$62,$34,$2C,$A1,$02,$A0,$DE,$95,$A4,$A0,$F6,$95,$33,$2C,$32; 24:952C
    .db $2C,$33,$21,$32,$21,$33,$2D,$32,$2D,$33,$22,$32,$22,$A1,$02,$95; 24:953C
    .db $8F,$33,$6A,$2C,$91,$8F,$62,$2C,$22,$2E,$20,$34,$2C,$A4,$95,$8F; 24:954C
    .db $33,$6A,$2E,$91,$8F,$62,$2E,$20,$25,$2B,$34,$2E,$95,$8F,$33,$6A; 24:955C
    .db $2A,$91,$8F,$62,$2A,$34,$2C,$2C,$2C,$2A,$A1,$02,$95,$8F,$33,$6A; 24:956C
    .db $2D,$91,$8F,$62,$2D,$2F,$2F,$2A,$34,$2C,$A4,$A1,$02,$95,$8F,$33; 24:957C
    .db $6A,$2E,$91,$8F,$62,$2E,$20,$25,$2B,$34,$2E,$A4,$A1,$02,$33,$2A; 24:958C
    .db $34,$2A,$A4,$A1,$02,$33,$2F,$34,$2F,$A4,$A1,$02,$33,$2A,$34,$2A; 24:959C
    .db $A4,$A1,$02,$33,$2E,$34,$2E,$A4,$A1,$02,$33,$2A,$34,$2A,$A4,$A1; 24:95AC
    .db $02,$33,$2D,$34,$2D,$A4,$A1,$04,$33,$2E,$34,$2E,$A4,$A0,$0E,$96; 24:95BC
    .db $A1,$04,$33,$2A,$34,$2A,$A4,$A0,$0E,$96,$A1,$04,$33,$2E,$34,$2E; 24:95CC
    .db $A4,$06,$91,$8F,$62,$33,$25,$92,$8F,$64,$2A,$91,$82,$60,$2A,$2A; 24:95DC
    .db $95,$8F,$6A,$2A,$91,$8F,$62,$34,$2E,$A8,$91,$8F,$62,$33,$24,$92; 24:95EC
    .db $8F,$64,$20,$91,$82,$60,$20,$20,$95,$8F,$6A,$20,$91,$8F,$62,$34; 24:95FC
    .db $2D,$A8,$A1,$04,$33,$2A,$34,$2A,$A4,$A1,$04,$33,$2D,$34,$2D,$A4; 24:960C
    .db $A1,$04,$33,$20,$34,$20,$A4,$A8,$A1,$07,$A0,$4E,$96,$A4,$A0,$77; 24:961C
    .db $96,$A1,$07,$A0,$4E,$96,$A4,$86,$E0,$D1,$64,$2A,$40,$2A,$2A,$A1; 24:962C
    .db $03,$A0,$4E,$96,$A4,$A0,$77,$96,$A1,$07,$A0,$4E,$96,$A4,$A0,$77; 24:963C
    .db $96,$06,$91,$8A,$ED,$D0,$62,$22,$60,$22,$22,$86,$E0,$D1,$62,$2A; 24:964C
    .db $8A,$ED,$D0,$60,$22,$22,$91,$8A,$ED,$D0,$62,$22,$60,$22,$22,$86; 24:965C
    .db $E0,$D1,$62,$2A,$8A,$ED,$D0,$60,$22,$22,$A8,$62,$22,$60,$22,$22; 24:966C
    .db $86,$E0,$D1,$62,$2A,$8A,$ED,$D0,$60,$22,$22,$62,$22,$60,$22,$22; 24:967C
    .db $86,$E0,$D1,$60,$2A,$2A,$2A,$2A,$A8,$01,$9E,$96,$3E,$97,$DB,$97; 24:968C
    .db $53,$98,$93,$88,$E4,$F3,$33,$66,$2A,$64,$45,$60,$2A,$25,$20,$24; 24:969C
    .db $66,$2F,$64,$45,$60,$2F,$2E,$22,$2D,$66,$21,$64,$45,$60,$21,$2D; 24:96AC
    .db $22,$2E,$66,$2F,$64,$45,$60,$2F,$24,$20,$25,$34,$66,$2C,$64,$45; 24:96BC
    .db $60,$2C,$33,$2B,$26,$2A,$66,$25,$64,$45,$60,$25,$20,$24,$2F,$66; 24:96CC
    .db $2E,$64,$45,$60,$2E,$2F,$24,$20,$60,$25,$20,$25,$2A,$26,$2A,$26; 24:96DC
    .db $2B,$34,$2C,$33,$2B,$34,$2C,$21,$2D,$21,$2D,$22,$34,$64,$2E,$60; 24:96EC
    .db $77,$22,$2D,$21,$64,$2F,$60,$77,$2E,$22,$2D,$64,$24,$60,$77,$2F; 24:96FC
    .db $2E,$22,$64,$20,$60,$77,$24,$2F,$2E,$6A,$24,$60,$24,$24,$62,$24; 24:970C
    .db $2D,$33,$25,$34,$2D,$24,$2C,$33,$25,$34,$2C,$A1,$03,$24,$2D,$33; 24:971C
    .db $20,$34,$2D,$A4,$88,$24,$2D,$33,$25,$34,$2D,$24,$2D,$33,$25,$34; 24:972C
    .db $2D,$07,$93,$88,$E4,$F3,$32,$66,$DA,$64,$45,$60,$DA,$D5,$D0,$D4; 24:973C
    .db $66,$DF,$64,$45,$60,$DF,$DE,$D2,$DD,$66,$D1,$64,$45,$60,$D1,$DD; 24:974C
    .db $D2,$DE,$66,$DF,$64,$45,$60,$DF,$D4,$D0,$D5,$32,$66,$2A,$64,$45; 24:975C
    .db $60,$2A,$25,$20,$24,$66,$2F,$64,$45,$60,$2F,$2E,$22,$2D,$66,$21; 24:976C
    .db $64,$45,$60,$21,$2D,$22,$2E,$60,$2F,$2E,$2F,$24,$20,$24,$20,$25; 24:977C
    .db $2A,$25,$2A,$26,$2B,$26,$2B,$33,$2C,$34,$64,$DE,$60,$77,$D2,$DD; 24:978C
    .db $D1,$64,$DF,$60,$77,$DE,$D2,$DD,$64,$D4,$60,$77,$DF,$DE,$D2,$64; 24:979C
    .db $D0,$60,$77,$D4,$DF,$DE,$6A,$D4,$60,$D4,$D4,$62,$D4,$DD,$33,$D5; 24:97AC
    .db $34,$DD,$D4,$DC,$33,$D5,$34,$DC,$A1,$03,$D4,$DD,$33,$D0,$34,$DD; 24:97BC
    .db $A4,$88,$D4,$DD,$33,$D5,$34,$DD,$D4,$DD,$33,$D5,$34,$DD,$07,$A0; 24:97CC
    .db $2B,$98,$92,$8F,$33,$62,$2F,$2F,$97,$8F,$64,$34,$2F,$66,$77,$A0; 24:97DC
    .db $2B,$98,$A1,$02,$33,$62,$2A,$2A,$64,$34,$2A,$A4,$95,$8F,$64,$33; 24:97EC
    .db $21,$34,$21,$33,$2D,$34,$2D,$33,$22,$34,$22,$33,$2E,$34,$2E,$91; 24:97FC
    .db $83,$A1,$04,$60,$33,$24,$24,$24,$24,$64,$24,$A4,$A1,$02,$97,$8F; 24:980C
    .db $6A,$33,$21,$91,$83,$60,$21,$21,$A4,$98,$8F,$66,$21,$77,$07,$92; 24:981C
    .db $8F,$33,$62,$2A,$2A,$97,$8F,$64,$34,$2A,$66,$77,$92,$8F,$33,$62; 24:982C
    .db $2F,$2F,$97,$8F,$64,$34,$2F,$66,$77,$92,$8F,$33,$62,$21,$21,$97; 24:983C
    .db $8F,$64,$34,$21,$66,$77,$A8,$91,$A1,$08,$86,$E0,$D1,$62,$2A,$2A; 24:984C
    .db $64,$28,$40,$8C,$ED,$D0,$60,$24,$24,$24,$24,$A4,$A1,$04,$86,$E0; 24:985C
    .db $D1,$6A,$2A,$8A,$ED,$D0,$60,$24,$24,$A4,$8E,$A1,$07,$60,$22,$22; 24:986C
    .db $22,$22,$64,$2A,$A4,$60,$22,$8D,$22,$22,$8B,$22,$D1,$64,$86,$2A; 24:987C
    .db $07,$01,$96,$98,$09,$99,$83,$99,$41,$9A,$93,$8A,$EC,$F3,$A1,$02; 24:988C
    .db $33,$62,$2D,$2F,$66,$40,$60,$2F,$2F,$2F,$2F,$62,$2E,$20,$64,$40; 24:989C
    .db $66,$40,$62,$2F,$2A,$66,$40,$60,$25,$25,$25,$25,$6A,$20,$62,$2E; 24:98AC
    .db $66,$77,$A4,$62,$2F,$2D,$66,$40,$60,$2A,$2A,$2A,$2A,$62,$25,$2E; 24:98BC
    .db $64,$40,$66,$40,$62,$20,$2E,$66,$40,$60,$20,$20,$20,$20,$62,$2F; 24:98CC
    .db $2D,$64,$40,$62,$40,$2A,$20,$2F,$6A,$2D,$60,$2E,$2F,$62,$20,$64; 24:98DC
    .db $2A,$62,$20,$6A,$26,$60,$2A,$20,$66,$2A,$61,$2D,$2A,$34,$2D,$66; 24:98EC
    .db $33,$25,$64,$77,$66,$2A,$64,$77,$61,$20,$2F,$2E,$08,$93,$8A,$EC; 24:98FC
    .db $F3,$A1,$02,$32,$62,$2A,$33,$2D,$66,$40,$60,$2D,$2D,$2D,$2D,$62; 24:990C
    .db $21,$2E,$64,$40,$66,$40,$62,$2D,$2F,$66,$40,$60,$2E,$2E,$2E,$2E; 24:991C
    .db $6A,$2E,$62,$21,$66,$77,$A4,$62,$2D,$32,$2A,$66,$40,$33,$60,$2F; 24:992C
    .db $2F,$2F,$2F,$62,$2E,$21,$64,$40,$66,$40,$62,$2E,$2C,$66,$40,$60; 24:993C
    .db $2E,$2E,$2E,$2E,$62,$2D,$32,$2A,$64,$40,$62,$33,$40,$2F,$2E,$2D; 24:994C
    .db $32,$6A,$2A,$33,$60,$2C,$2D,$62,$2E,$64,$2F,$62,$2E,$6A,$20,$60; 24:995C
    .db $2F,$2E,$66,$2F,$61,$DD,$DA,$34,$DD,$66,$33,$D5,$64,$77,$66,$DA; 24:996C
    .db $64,$77,$61,$D0,$DF,$DE,$08,$92,$8F,$A1,$02,$33,$64,$2D,$60,$40; 24:997C
    .db $2D,$2F,$2A,$64,$25,$62,$40,$60,$2F,$2E,$64,$20,$62,$40,$60,$2F; 24:998C
    .db $2E,$62,$2F,$2E,$64,$40,$64,$2D,$62,$40,$60,$2A,$34,$2D,$64,$21; 24:999C
    .db $62,$40,$60,$33,$26,$2A,$64,$34,$2C,$62,$40,$60,$33,$26,$2A,$62; 24:99AC
    .db $26,$2A,$20,$2F,$A4,$91,$84,$60,$2D,$2D,$21,$2D,$2F,$2F,$2E,$2F; 24:99BC
    .db $2A,$2A,$25,$2A,$34,$64,$2D,$33,$60,$21,$21,$2C,$21,$2E,$2E,$22; 24:99CC
    .db $2E,$25,$25,$20,$25,$34,$64,$21,$33,$60,$2C,$2C,$32,$2B,$33,$2C; 24:99DC
    .db $2E,$2E,$22,$2E,$20,$20,$24,$20,$34,$64,$2C,$92,$8F,$6A,$2D,$60; 24:99EC
    .db $33,$2D,$2D,$6A,$2D,$60,$2D,$2D,$64,$2D,$34,$69,$2D,$60,$33,$2D; 24:99FC
    .db $62,$2D,$2D,$64,$34,$2D,$33,$64,$2C,$34,$69,$2C,$60,$33,$2C,$62; 24:9A0C
    .db $2C,$2C,$64,$34,$2C,$33,$64,$2D,$34,$69,$2D,$60,$33,$2D,$62,$2D; 24:9A1C
    .db $2D,$64,$34,$2D,$33,$64,$21,$34,$69,$21,$60,$33,$21,$62,$21,$21; 24:9A2C
    .db $34,$21,$33,$21,$08,$A1,$0C,$91,$8E,$E0,$62,$28,$28,$66,$40,$91; 24:9A3C
    .db $8C,$ED,$60,$25,$25,$25,$25,$A4,$A1,$02,$91,$8E,$E0,$64,$28,$69; 24:9A4C
    .db $40,$60,$28,$91,$8C,$ED,$64,$40,$62,$40,$60,$25,$25,$A4,$91,$8E; 24:9A5C
    .db $E0,$61,$25,$25,$25,$66,$40,$91,$8C,$ED,$60,$25,$25,$25,$25,$91; 24:9A6C
    .db $8E,$E0,$64,$28,$69,$40,$60,$28,$64,$40,$61,$25,$25,$25,$08,$01; 24:9A7C
    .db $94,$9A,$3C,$9B,$F7,$9B,$6A,$9C,$93,$8D,$EE,$F3,$A0,$0D,$9B,$61; 24:9A8C
    .db $2A,$2B,$6E,$2A,$6C,$77,$61,$20,$2F,$6E,$20,$6C,$77,$51,$2F,$77; 24:9A9C
    .db $2E,$77,$A0,$0D,$9B,$61,$2A,$2B,$6E,$2A,$51,$77,$6E,$42,$61,$2A; 24:9AAC
    .db $2A,$51,$34,$2D,$6C,$42,$93,$8E,$E6,$33,$61,$24,$2A,$34,$2D,$2A; 24:9ABC
    .db $6C,$20,$61,$40,$20,$2D,$20,$51,$2B,$77,$6C,$77,$65,$2D,$67,$2E; 24:9ACC
    .db $24,$61,$8F,$24,$77,$83,$77,$84,$77,$85,$86,$77,$87,$77,$88,$77; 24:9ADC
    .db $89,$77,$8A,$77,$8B,$77,$8C,$77,$8D,$77,$8E,$77,$8F,$77,$77,$77; 24:9AEC
    .db $77,$77,$77,$77,$77,$77,$77,$77,$77,$43,$43,$43,$42,$42,$42,$42; 24:9AFC
    .db $09,$51,$33,$2A,$6C,$77,$65,$2A,$67,$20,$2F,$51,$20,$6C,$77,$6E; 24:9B0C
    .db $2D,$20,$51,$34,$2C,$6C,$77,$65,$33,$2B,$67,$2B,$20,$61,$2A,$2B; 24:9B1C
    .db $6E,$2A,$51,$77,$6C,$77,$51,$34,$2D,$6E,$77,$2C,$33,$2B,$20,$A8; 24:9B2C
    .db $95,$89,$E7,$A0,$D0,$9B,$A1,$02,$32,$65,$26,$60,$42,$33,$65,$2D; 24:9B3C
    .db $60,$42,$65,$2F,$60,$42,$65,$2A,$60,$42,$A4,$A1,$02,$33,$65,$2C; 24:9B4C
    .db $60,$42,$65,$2E,$60,$42,$65,$20,$60,$42,$34,$65,$2C,$60,$42,$A4; 24:9B5C
    .db $A0,$D0,$9B,$33,$65,$2D,$60,$42,$65,$2F,$60,$42,$65,$2A,$60,$42; 24:9B6C
    .db $34,$65,$2C,$60,$42,$51,$2D,$E6,$8D,$6C,$33,$2B,$61,$40,$2B,$20; 24:9B7C
    .db $2B,$6C,$34,$2D,$61,$40,$2D,$33,$2B,$34,$2D,$51,$20,$6C,$77,$65; 24:9B8C
    .db $24,$67,$20,$2A,$61,$8F,$2A,$77,$83,$77,$84,$77,$85,$86,$77,$87; 24:9B9C
    .db $77,$88,$77,$89,$77,$8A,$77,$8B,$77,$8C,$77,$8D,$77,$8E,$77,$8F; 24:9BAC
    .db $77,$77,$77,$77,$77,$77,$77,$77,$77,$77,$77,$77,$43,$43,$43,$42; 24:9BBC
    .db $42,$42,$42,$09,$A1,$06,$33,$65,$2D,$60,$42,$65,$2F,$60,$42,$65; 24:9BCC
    .db $2A,$60,$42,$34,$65,$2C,$60,$42,$65,$2D,$60,$42,$33,$65,$2D,$60; 24:9BDC
    .db $42,$65,$2F,$60,$42,$65,$2A,$60,$42,$A4,$A8,$98,$8F,$33,$51,$2D; 24:9BEC
    .db $77,$2C,$77,$2C,$77,$2D,$77,$2D,$77,$2D,$77,$2F,$77,$20,$77,$95; 24:9BFC
    .db $88,$6C,$2D,$2D,$2D,$2D,$33,$6C,$2C,$2C,$2C,$6E,$2C,$32,$20,$33; 24:9C0C
    .db $6C,$2C,$2C,$2C,$6E,$2C,$20,$A1,$04,$6C,$2D,$A4,$2D,$2D,$2D,$6E; 24:9C1C
    .db $2D,$32,$2A,$A1,$04,$33,$6C,$2D,$A4,$2A,$2A,$2A,$2D,$96,$8F,$6C; 24:9C2C
    .db $20,$6E,$77,$91,$8A,$61,$20,$20,$96,$8F,$6C,$20,$6E,$77,$91,$8A; 24:9C3C
    .db $61,$2D,$20,$96,$8F,$6C,$20,$6E,$77,$91,$8A,$61,$20,$20,$6C,$20; 24:9C4C
    .db $20,$A1,$04,$94,$88,$6C,$2D,$A4,$98,$8F,$51,$2D,$77,$09,$53,$40; 24:9C5C
    .db $00,$01,$76,$9C,$24,$9D,$E2,$9D,$8B,$9E,$95,$8E,$E9,$F3,$6E,$34; 24:9C6C
    .db $40,$22,$2D,$2F,$61,$22,$22,$6E,$2D,$6E,$2F,$22,$6E,$42,$22,$2F; 24:9C7C
    .db $20,$25,$61,$20,$2F,$6E,$22,$2D,$6E,$42,$2F,$22,$2D,$61,$22,$22; 24:9C8C
    .db $6E,$2D,$6E,$2F,$22,$66,$22,$61,$43,$61,$2F,$51,$2D,$93,$8E,$EF; 24:9C9C
    .db $6E,$40,$33,$2C,$34,$2C,$33,$26,$61,$2A,$26,$2A,$20,$6E,$2F,$22; 24:9CAC
    .db $6E,$42,$32,$26,$33,$26,$25,$61,$20,$25,$20,$2F,$6C,$22,$6E,$42; 24:9CBC
    .db $33,$2E,$2F,$61,$20,$2C,$6E,$25,$26,$6C,$34,$2C,$66,$33,$2B,$34; 24:9CCC
    .db $6E,$2C,$6C,$2D,$61,$42,$40,$93,$8F,$EE,$F3,$6E,$33,$20,$66,$34; 24:9CDC
    .db $22,$6E,$22,$2D,$33,$6C,$26,$61,$20,$20,$66,$34,$2C,$6E,$2C,$6E; 24:9CEC
    .db $33,$26,$6C,$20,$6E,$20,$66,$25,$61,$26,$34,$2C,$66,$2D,$61,$2C; 24:9CFC
    .db $33,$26,$51,$20,$77,$66,$34,$22,$61,$2D,$22,$66,$2F,$61,$22,$2F; 24:9D0C
    .db $51,$20,$77,$64,$43,$53,$40,$00,$95,$8D,$E9,$F3,$6E,$34,$40,$2C; 24:9D1C
    .db $33,$2B,$34,$2D,$61,$2C,$2C,$6E,$33,$20,$6E,$34,$2D,$2C,$6E,$42; 24:9D2C
    .db $2C,$2D,$22,$2F,$61,$22,$2D,$6E,$2C,$33,$2B,$6E,$42,$34,$2D,$2C; 24:9D3C
    .db $33,$2B,$61,$34,$2C,$2C,$6E,$33,$20,$6E,$34,$2D,$2C,$66,$2C,$61; 24:9D4C
    .db $43,$61,$2D,$51,$33,$2B,$93,$8E,$EF,$6E,$40,$33,$DC,$34,$DC,$33; 24:9D5C
    .db $D6,$61,$DA,$D6,$DA,$D0,$6E,$DF,$D2,$6E,$42,$32,$D6,$33,$D6,$D5; 24:9D6C
    .db $61,$D0,$D5,$D0,$DF,$6C,$D2,$6E,$42,$33,$DE,$DF,$61,$D0,$DC,$6E; 24:9D7C
    .db $D5,$D6,$6C,$34,$DC,$66,$33,$DB,$34,$6E,$DC,$6E,$DD,$93,$8D,$EE; 24:9D8C
    .db $F3,$32,$2B,$33,$2C,$2D,$66,$34,$2C,$6E,$2C,$33,$26,$33,$6C,$20; 24:9D9C
    .db $61,$22,$22,$66,$33,$25,$6E,$25,$6E,$20,$6C,$22,$6E,$22,$66,$2F; 24:9DAC
    .db $61,$20,$25,$66,$26,$61,$25,$20,$66,$22,$61,$2F,$22,$6C,$2D,$32; 24:9DBC
    .db $2B,$66,$34,$2C,$61,$33,$2B,$34,$2C,$66,$2D,$61,$2C,$2D,$51,$2E; 24:9DCC
    .db $77,$64,$43,$53,$40,$00,$A1,$03,$95,$8F,$6C,$33,$2C,$91,$8F,$64; 24:9DDC
    .db $34,$2C,$61,$33,$2C,$40,$6E,$2C,$61,$2C,$92,$86,$6E,$34,$2C,$33; 24:9DEC
    .db $2C,$A4,$95,$8F,$6C,$33,$2C,$91,$8F,$6E,$34,$2C,$33,$2C,$92,$8F; 24:9DFC
    .db $6E,$20,$6C,$20,$6E,$20,$92,$88,$6E,$33,$2C,$34,$2C,$22,$20,$33; 24:9E0C
    .db $25,$34,$2C,$22,$25,$33,$2F,$26,$34,$2D,$2F,$22,$26,$34,$22,$20; 24:9E1C
    .db $33,$2C,$26,$34,$2C,$2E,$33,$2F,$25,$34,$2C,$2F,$33,$20,$2B,$34; 24:9E2C
    .db $2D,$2F,$20,$33,$20,$2A,$2B,$95,$8F,$66,$34,$2C,$91,$8F,$6E,$2C; 24:9E3C
    .db $95,$8F,$33,$66,$20,$91,$8F,$6E,$20,$95,$8F,$66,$2F,$91,$8F,$6E; 24:9E4C
    .db $2F,$6E,$20,$20,$40,$20,$A1,$02,$95,$8F,$66,$2F,$91,$8F,$6E,$2F; 24:9E5C
    .db $A4,$95,$8F,$66,$2C,$91,$8F,$6E,$2C,$6E,$20,$20,$40,$20,$A1,$02; 24:9E6C
    .db $2C,$2C,$40,$2C,$A4,$98,$8F,$51,$2C,$77,$64,$40,$53,$40,$00,$53; 24:9E7C
    .db $40,$00,$01,$97,$9E,$D6,$9E,$19,$9F,$2F,$9F,$93,$8F,$E7,$F3,$64; 24:9E8C
    .db $33,$26,$62,$42,$2A,$66,$77,$62,$42,$2D,$2E,$2F,$64,$20,$62,$2F; 24:9E9C
    .db $26,$77,$2A,$64,$2A,$42,$62,$40,$20,$77,$2A,$60,$77,$84,$77,$85; 24:9EAC
    .db $77,$86,$77,$87,$77,$88,$77,$89,$77,$8A,$77,$8B,$77,$8C,$77,$8D; 24:9EBC
    .db $77,$8E,$77,$77,$77,$62,$42,$53,$40,$00,$93,$8F,$E7,$F3,$64,$33; 24:9ECC
    .db $2D,$62,$42,$2C,$66,$77,$62,$42,$32,$26,$33,$2C,$2D,$64,$2E,$62; 24:9EDC
    .db $2D,$2C,$77,$2C,$64,$2C,$42,$62,$40,$32,$26,$77,$33,$2C,$60,$77; 24:9EEC
    .db $84,$77,$85,$77,$86,$77,$87,$77,$88,$77,$89,$77,$8A,$77,$8B,$77; 24:9EFC
    .db $8C,$77,$8D,$77,$8E,$77,$77,$77,$62,$42,$53,$40,$00,$91,$8F,$A1; 24:9F0C
    .db $0B,$62,$33,$2F,$34,$2F,$A4,$33,$2F,$2F,$77,$2F,$66,$2F,$64,$77; 24:9F1C
    .db $53,$40,$00,$91,$8F,$ED,$A1,$06,$D0,$62,$24,$24,$D1,$84,$2A,$D0; 24:9F2C
    .db $8F,$24,$A4,$D0,$24,$D1,$84,$2A,$66,$2A,$64,$40,$53,$40,$00,$01; 24:9F3C
    .db $54,$9F,$D2,$9F,$51,$A0,$E2,$A0,$93,$8C,$E6,$F1,$60,$32,$25,$25; 24:9F4C
    .db $25,$25,$2B,$2B,$2B,$2B,$26,$26,$26,$26,$25,$25,$25,$25,$66,$77; 24:9F5C
    .db $77,$60,$2C,$2C,$62,$42,$60,$2F,$2F,$62,$42,$60,$25,$25,$62,$42; 24:9F6C
    .db $60,$34,$2C,$2C,$62,$42,$6A,$33,$2B,$60,$2B,$2B,$62,$2B,$20,$64; 24:9F7C
    .db $42,$64,$26,$25,$20,$2F,$66,$25,$20,$60,$2C,$2C,$62,$42,$60,$2F; 24:9F8C
    .db $2F,$62,$42,$60,$25,$25,$62,$42,$60,$34,$2C,$2C,$62,$42,$6A,$33; 24:9F9C
    .db $2B,$60,$2B,$2B,$62,$2B,$34,$2D,$64,$42,$64,$22,$2E,$66,$2D,$A2; 24:9FAC
    .db $02,$66,$34,$21,$61,$41,$21,$21,$21,$21,$21,$66,$22,$61,$41,$22; 24:9FBC
    .db $22,$22,$22,$22,$A8,$0C,$93,$8C,$E6,$F1,$60,$32,$2F,$2F,$2F,$2F; 24:9FCC
    .db $20,$20,$20,$20,$24,$24,$24,$24,$2F,$2F,$2F,$2F,$66,$77,$77,$60; 24:9FDC
    .db $32,$25,$25,$62,$42,$60,$33,$2C,$2C,$62,$42,$60,$2F,$2F,$62,$42; 24:9FEC
    .db $60,$25,$25,$62                 ; 24:9FFC
