; re-c bank 19  file 0x26010  CPU $A000-$BFFF  (tools/disasm_all.py)

scroll_shadow0 = $02
cam_x_hi = $03
cam_y_lo = $04
cam_y_hi = $05
tmp_08 = $08
frame_ready = $09
tmp_0A = $0A
tmp_0B = $0B
jmp_trampoline = $10
tmp_11 = $11
tmp_20 = $20
tmp_21 = $21
tmp_22 = $22
tmp_2E = $2E
tmp_2F = $2F
tmp_30 = $30
tmp_31 = $31
main_state = $40
sub_state = $41
pad_new = $4B
irq_mode = $51
obj_x_lo = $70
obj_x_lo_s01 = $71
zp_72 = $72
zp_73 = $73
zp_82 = $82
zp_85 = $85
zp_86 = $86
zp_87 = $87
zp_88 = $88
zp_8C = $8C
zp_8D = $8D
zp_8E = $8E
zp_8F = $8F
obj_x_hi = $90
obj_y_lo = $B0
obj_y_lo_s01 = $B1
obj_y_lo_s0E = $BE
snd_queue_count = $0100
snd_queue_lock = $0101
snd_cur_music = $0102
snd_queue = $0103
oam_y = $0200
oam_tile = $0201
oam_attr = $0202
oam_x = $0203
obj_meta_id = $0300
obj_meta_id_s01 = $0301
obj_meta_id_s0D = $030D
obj_meta_id_s0E = $030E
obj_tile_ofs_s01 = $0321
obj_flags = $0380
obj_flags_s01 = $0381
obj_flags_s0E = $038E
player_hp_max = $03C0
player_hp = $03C1
obj_active = $04A0
obj_anim = $04E0
obj_anim_s01 = $04E1
obj_anim_s0D = $04ED
obj_anim_s0E = $04EE
obj_overlay_id = $0580
obj_overlay_id_s01 = $0581
obj_freeze = $05A0
obj_phase = $0600
ram_0617 = $0617
ram_0618 = $0618
ram_061F = $061F
obj_timer2 = $0620
obj_timer2_s03 = $0623
ram_0627 = $0627
ram_0628 = $0628
ram_062F = $062F
ram_06B2 = $06B2
orbit_level = $06C0
orbit_burst_timer = $06C1
pending_msg_id = $06C3
ram_06C5 = $06C5
ram_06C8 = $06C8
ram_06CD = $06CD
pending_msg_group = $06E3
title_mode = $06F0
title_step = $06F1
ram_06F2 = $06F2
state3_mode = $06F3
ram_06FC = $06FC
ram_06FD = $06FD
ram_06FE = $06FE
ram_06FF = $06FF
obj1_flash = $0710
equip_shield = $0714
use_item_sel = $0715
invuln_timer = $071A
opel_revive = $0743
text_msg_id = $07DE
text_group = $07DF
pal_ids = $07E0
pal_id_bg1 = $07E1
pal_id_bg2 = $07E2
pal_id_bg3 = $07E3
pal_id_spr0 = $07E4
pal_id_spr1 = $07E5
pal_id_spr2 = $07E6
pal_id_spr3 = $07E7
pal_nofade_mask = $07E8
chr_r0_play = $07F0
chr_r1_play = $07F1
chr_r2_play = $07F2
chr_r3_play = $07F3
chr_r4_play = $07F4
chr_r5_play = $07F5
wram_buf = $6000
wram_6001 = $6001
wram_6002 = $6002
wram_6003 = $6003
wram_6020 = $6020
wram_6021 = $6021
wram_6040 = $6040
wram_6060 = $6060
attr_shadow = $6100
pal_buf = $6140
ppu_queue = $6200
ppu_queue_1 = $6201
ppu_queue_2 = $6202
ppu_queue_3 = $6203
player_name = $6400
player_name_hi = $6410
player_name6 = $6418
sub_18_80DF = $80DF
SceneMetaDraw = $80E7
sub_18_8174 = $8174
sub_18_817C = $817C
WaitFrameReady = $8198
NtFill = $81A1
OamHideAll18 = $81D8
NtRectBlit = $81F0
MenuPalBlack = $82B3
MenuPalLoad = $82BE
MenuPalApply = $82F1
MenuPalFadeIn = $82FD
TitleFadeIn = $8310
TitleFadeOutStart = $8344
TitleFadeOut = $834A
PaletteBuild = $8C0E
Bank26_FadeA = $8C3D
Bank26_FadeB = $8C49
HudHpBar = $8CC0
sub_26_8D59 = $8D59
HudRedraw = $8E46
TempAddStep = $9861
TempToObjPos = $9897
ObjPosToTemp = $98A8
NameEntryRect = $9DD4
NameCellGrid = $9EC2
NameGlyphGrid = $9F42
Sub18_9FC2 = $9FC2
sub_30_C008 = $C008
PlaySound = $C125
WaitFrame = $C169
sub_30_C176 = $C176
OamClipTop = $C17D
sub_30_C188 = $C188
SetR6 = $C418
PpuQueueBlock = $C482
WaitPpuQueue = $C676
sub_30_C739 = $C739
sub_30_C867 = $C867
sub_30_C874 = $C874
ReadPads = $FE80
FarCall_C25D = $FF80
FarCall_B14_800E = $FF8C
FarCall_D354 = $FFCF

    .org $A000
    .db $04                             ; 19:A000
    STA cam_x_hi                        ; A001  85 03
    STA cam_y_hi                        ; A003  85 05
    JSR sub_19_A07C                     ; A005  20 7c a0
    LDA #$FF                            ; A008  a9 ff
    STA ram_06B2                        ; A00A  8d b2 06
    INC main_state                      ; A00D  e6 40
    LDA #$00                            ; A00F  a9 00
    JSR sub_19_A07C                     ; A011  20 7c a0
    RTS                                 ; A014  60
MainState3:  ; main state 3 frame (30:C939 ReadPadsMenu then JMP $A015): $06FF--; dispatch $06F3 via State3ModeTable
    LDA ram_06B2                        ; A015  ad b2 06
    BMI loc_19_A01D                     ; A018  30 03
    JSR sub_19_A07C                     ; A01A  20 7c a0
loc_19_A01D:
    LDA #$00                            ; A01D  a9 00
    STA frame_ready                     ; A01F  85 09
    DEC ram_06FF                        ; A021  ce ff 06
    LDA state3_mode                     ; A024  ad f3 06
    ASL A                               ; A027  0a
    TAY                                 ; A028  a8
    LDA State3ModeTable,Y               ; A029  b9 38 a0
    STA ram_06FC                        ; A02C  8d fc 06
    LDA $A039,Y                         ; A02F  b9 39 a0
    STA ram_06FD                        ; A032  8d fd 06
    JMP (ram_06FC)                      ; A035  6c fc 06
State3ModeTable:  ; ptr16[3] by $06F3: A03E 18:8310 18:834A (JMP ($06FC) at 19:A035)
    .dw State3SubDispatch               ; 19:A038
    .dw TitleFadeIn                     ; 19:A03A
    .dw TitleFadeOut                    ; 19:A03C
State3SubDispatch:  ; mode 0: dispatch $06F0 via State3SubTable (JMP ($06FC))
    LDA title_mode                      ; A03E  ad f0 06
    ASL A                               ; A041  0a
    TAY                                 ; A042  a8
    LDA State3SubTable,Y                ; A043  b9 52 a0
    STA ram_06FC                        ; A046  8d fc 06
    LDA $A053,Y                         ; A049  b9 53 a0
    STA ram_06FD                        ; A04C  8d fd 06
    JMP (ram_06FC)                      ; A04F  6c fc 06
State3SubTable:  ; ptr16[9] by $06F0: A79F ABB4 ABEA A0B5 A176 A653 A6DF 0000 0000
    .dw TitleOpening                    ; 19:A052
    .dw TitleStoryInit                  ; 19:A054
    .dw TitleIntroStory                 ; 19:A056
    .dw TitleStatusInit                 ; 19:A058
    .dw TitleStatusScreen               ; 19:A05A
    .dw TitleMenuInit                   ; 19:A05C
    .dw TitleMenu                       ; 19:A05E
    .dw $0000                           ; 19:A060
    .dw $0000                           ; 19:A062
TitleStepFadeOut:  ; s67: TitleFadeOutStart; $06FE = $1E; INC title_step (step filler in the opening/story tables)
    JSR TitleFadeOutStart               ; A064  20 44 83
    LDA #$1E                            ; A067  a9 1e
    STA ram_06FE                        ; A069  8d fe 06
    INC title_step                      ; A06C  ee f1 06
    RTS                                 ; A06F  60
sub_19_A070:
    JSR MenuPalBlack                    ; A070  20 b3 82
    LDA #$3C                            ; A073  a9 3c
    STA ram_06FE                        ; A075  8d fe 06
    INC title_step                      ; A078  ee f1 06
    RTS                                 ; A07B  60
sub_19_A07C:
    STA snd_queue                       ; A07C  8d 03 01
    LDA #$01                            ; A07F  a9 01
    STA snd_queue_lock                  ; A081  8d 01 01
    STA snd_queue_count                 ; A084  8d 00 01
    LDA #$00                            ; A087  a9 00
    STA snd_queue_lock                  ; A089  8d 01 01
    LDA #$FF                            ; A08C  a9 ff
    STA ram_06B2                        ; A08E  8d b2 06
    RTS                                 ; A091  60
sub_19_A092:
    LDA tmp_0A                          ; A092  a5 0a
    CMP tmp_0B                          ; A094  c5 0b
    BNE sub_19_A092                     ; A096  d0 fa
    JSR sub_18_8174                     ; A098  20 74 81
    LDA #$00                            ; A09B  a9 00
    STA tmp_0A                          ; A09D  85 0a
    STA tmp_0B                          ; A09F  85 0b
    JSR sub_18_817C                     ; A0A1  20 7c 81
    RTS                                 ; A0A4  60
sub_19_A0A5:
    LDA $8B                             ; A0A5  a5 8b
    SEC                                 ; A0A7  38
    SBC #$10                            ; A0A8  e9 10
    ASL A                               ; A0AA  0a
    ASL A                               ; A0AB  0a
    ASL A                               ; A0AC  0a
    ASL A                               ; A0AD  0a
    CLC                                 ; A0AE  18
    ADC $8A                             ; A0AF  65 8a
    STA zp_88                           ; A0B1  85 88
    TAY                                 ; A0B3  a8
    RTS                                 ; A0B4  60
TitleStatusInit:  ; s67: title_mode 3: CHR $3C/$36/$C7/$C7
    LDA #$00                            ; A0B5  a9 00
    STA ram_06B2                        ; A0B7  8d b2 06
    JSR MenuPalBlack                    ; A0BA  20 b3 82
    JSR OamHideAll18                    ; A0BD  20 d8 81
    JSR WaitFrameReady                  ; A0C0  20 98 81
    LDA #$00                            ; A0C3  a9 00
    STA cam_y_lo                        ; A0C5  85 04
    STA cam_y_hi                        ; A0C7  85 05
    LDA #$3C                            ; A0C9  a9 3c
    STA chr_r0_play                     ; A0CB  8d f0 07
    LDA #$36                            ; A0CE  a9 36
    STA chr_r1_play                     ; A0D0  8d f1 07
    LDA #$C7                            ; A0D3  a9 c7
    STA chr_r2_play                     ; A0D5  8d f2 07
    LDA #$C7                            ; A0D8  a9 c7
    STA chr_r3_play                     ; A0DA  8d f3 07
    LDA #$09                            ; A0DD  a9 09
    JSR MenuPalLoad                     ; A0DF  20 be 82
    LDA #$8B                            ; A0E2  a9 8b
    JSR MenuPalLoad                     ; A0E4  20 be 82
    JSR MenuPalApply                    ; A0E7  20 f1 82
    LDA #$0D                            ; A0EA  a9 0d
    JSR MenuPalLoad                     ; A0EC  20 be 82
    LDA #$0F                            ; A0EF  a9 0f
    STA $06DD                           ; A0F1  8d dd 06
    LDA #$00                            ; A0F4  a9 00
    STA ram_06FE                        ; A0F6  8d fe 06
    LDA $99D0                           ; A0F9  ad d0 99
    STA zp_8E                           ; A0FC  85 8e
    LDA $99D1                           ; A0FE  ad d1 99
    STA zp_8F                           ; A101  85 8f
    LDA #$00                            ; A103  a9 00
    STA zp_8C                           ; A105  85 8c
    LDA #$20                            ; A107  a9 20
    STA zp_8D                           ; A109  85 8d
    JSR NtRectBlit                      ; A10B  20 f0 81
    LDA NameEntryRect                   ; A10E  ad d4 9d
    STA zp_8E                           ; A111  85 8e
    LDA $9DD5                           ; A113  ad d5 9d
    STA zp_8F                           ; A116  85 8f
    LDA #$03                            ; A118  a9 03
    STA zp_8C                           ; A11A  85 8c
    LDA #$22                            ; A11C  a9 22
    STA zp_8D                           ; A11E  85 8d
    JSR NtRectBlit                      ; A120  20 f0 81
    LDA #$00                            ; A123  a9 00
    STA title_step                      ; A125  8d f1 06
    INC title_mode                      ; A128  ee f0 06
    LDA #$0D                            ; A12B  a9 0d
    STA ram_0617                        ; A12D  8d 17 06
    LDA #$00                            ; A130  a9 00
    STA ram_0627                        ; A132  8d 27 06
    LDX #$00                            ; A135  a2 00
loc_19_A137:
    LDA $A16A,X                         ; A137  bd 6a a1
    STA ram_0618,X                      ; A13A  9d 18 06
    LDA $A16E,X                         ; A13D  bd 6e a1
    STA $0610,X                         ; A140  9d 10 06
    LDA $A172,X                         ; A143  bd 72 a1
    STA $0608,X                         ; A146  9d 08 06
    LDA #$00                            ; A149  a9 00
    STA obj_timer2,X                    ; A14B  9d 20 06
    STA ram_0628,X                      ; A14E  9d 28 06
    STA obj_phase,X                     ; A151  9d 00 06
    INX                                 ; A154  e8
    CPX #$04                            ; A155  e0 04
    BNE loc_19_A137                     ; A157  d0 de
    LDA #$78                            ; A159  a9 78
    STA $06B4                           ; A15B  8d b4 06
    STA $06BB                           ; A15E  8d bb 06
    LDA #$10                            ; A161  a9 10
    STA $8B                             ; A163  85 8b
    LDA #$03                            ; A165  a9 03
    STA $8A                             ; A167  85 8a
    RTS                                 ; A169  60
    .db $08,$09,$0A,$08,$18,$D0,$B0,$D0,$08,$08,$57,$00; 19:A16A
TitleStatusScreen:  ; s38: title_mode 4 (green status screen + name entry); steps via JmpTable19_A193 by $06F1
    JSR OamHideAll18                    ; A176  20 d8 81
    JSR sub_19_A212                     ; A179  20 12 a2
    JSR sub_19_A1AA                     ; A17C  20 aa a1
    LDA title_step                      ; A17F  ad f1 06
    ASL A                               ; A182  0a
    TAY                                 ; A183  a8
    LDA JmpTable19_A193,Y               ; A184  b9 93 a1
    STA ram_06FC                        ; A187  8d fc 06
    LDA $A194,Y                         ; A18A  b9 94 a1
    STA ram_06FD                        ; A18D  8d fd 06
    JMP (ram_06FC)                      ; A190  6c fc 06
JmpTable19_A193:  ; s67: title_mode 4 status-screen steps [title_step]: 7 entries A1A1 A26B A27C A34E A3DD A3F6(name entry) A57C(done) (JUMP_TABLES)
    .dw sub_19_A1A1                     ; 19:A193
    .dw sub_19_A26B                     ; 19:A195
    .dw sub_19_A27C                     ; 19:A197
    .dw sub_19_A34E                     ; 19:A199
    .dw sub_19_A3DD                     ; 19:A19B
    .dw NameEntryInput                  ; 19:A19D
    .dw NameEntryDone                   ; 19:A19F
sub_19_A1A1:
    LDA #$57                            ; A1A1  a9 57
    STA ram_06B2                        ; A1A3  8d b2 06
    INC title_step                      ; A1A6  ee f1 06
    RTS                                 ; A1A9  60
sub_19_A1AA:
    LDA #$80                            ; A1AA  a9 80
    STA ram_06C5                        ; A1AC  8d c5 06
    LDA #$00                            ; A1AF  a9 00
    STA $06C6                           ; A1B1  8d c6 06
    LDA #$FF                            ; A1B4  a9 ff
    STA $06C4                           ; A1B6  8d c4 06
    LDA #$00                            ; A1B9  a9 00
    STA ram_06C8                        ; A1BB  8d c8 06
loc_19_A1BE:
    LDX ram_06C8                        ; A1BE  ae c8 06
    LDA ram_0618,X                      ; A1C1  bd 18 06
    STA zp_72                           ; A1C4  85 72
    LDA obj_timer2,X                    ; A1C6  bd 20 06
    STA zp_73                           ; A1C9  85 73
    LDA $0610,X                         ; A1CB  bd 10 06
    STA zp_88                           ; A1CE  85 88
    LDA $0608,X                         ; A1D0  bd 08 06
    STA $89                             ; A1D3  85 89
    JSR sub_18_80DF                     ; A1D5  20 df 80
    LDX ram_06C8                        ; A1D8  ae c8 06
    LDA obj_phase,X                     ; A1DB  bd 00 06
    BNE loc_19_A1EC                     ; A1DE  d0 0c
    LDA $06C2                           ; A1E0  ad c2 06
    STA ram_0628,X                      ; A1E3  9d 28 06
    INC obj_phase,X                     ; A1E6  fe 00 06
    JMP loc_19_A207                     ; A1E9  4c 07 a2
loc_19_A1EC:
    DEC ram_0628,X                      ; A1EC  de 28 06
    BPL loc_19_A207                     ; A1EF  10 16
    LDA #$00                            ; A1F1  a9 00
    STA obj_phase,X                     ; A1F3  9d 00 06
    INC obj_timer2,X                    ; A1F6  fe 20 06
    LDA obj_timer2,X                    ; A1F9  bd 20 06
    CMP orbit_level                     ; A1FC  cd c0 06
    BNE loc_19_A207                     ; A1FF  d0 06
    LDA orbit_burst_timer               ; A201  ad c1 06
    STA obj_timer2,X                    ; A204  9d 20 06
loc_19_A207:
    INC ram_06C8                        ; A207  ee c8 06
    LDA ram_06C8                        ; A20A  ad c8 06
    CMP #$04                            ; A20D  c9 04
    BNE loc_19_A1BE                     ; A20F  d0 ad
    RTS                                 ; A211  60
sub_19_A212:
    LDA $06BB                           ; A212  ad bb 06
    BNE loc_19_A218                     ; A215  d0 01
    RTS                                 ; A217  60
loc_19_A218:
    LDA ram_0627                        ; A218  ad 27 06
    ASL A                               ; A21B  0a
    ASL A                               ; A21C  0a
    ASL A                               ; A21D  0a
    TAY                                 ; A21E  a8
    LDA #$18                            ; A21F  a9 18
    STA ram_061F                        ; A221  8d 1f 06
    LDA #$07                            ; A224  a9 07
    STA ram_062F                        ; A226  8d 2f 06
    LDX #$58                            ; A229  a2 58
loc_19_A22B:
    LDA #$58                            ; A22B  a9 58
    STA oam_y,X                         ; A22D  9d 00 02
    LDA $9908,Y                         ; A230  b9 08 99
    STA oam_tile,X                      ; A233  9d 01 02
    LDA #$00                            ; A236  a9 00
    STA oam_attr,X                      ; A238  9d 02 02
    LDA ram_061F                        ; A23B  ad 1f 06
    STA oam_x,X                         ; A23E  9d 03 02
    CLC                                 ; A241  18
    ADC #$08                            ; A242  69 08
    STA ram_061F                        ; A244  8d 1f 06
    INY                                 ; A247  c8
    INX                                 ; A248  e8
    INX                                 ; A249  e8
    INX                                 ; A24A  e8
    INX                                 ; A24B  e8
    DEC ram_062F                        ; A24C  ce 2f 06
    BNE loc_19_A22B                     ; A24F  d0 da
    LDA ram_06FF                        ; A251  ad ff 06
    AND #$07                            ; A254  29 07
    BEQ loc_19_A259                     ; A256  f0 01
    RTS                                 ; A258  60
loc_19_A259:
    DEC ram_0617                        ; A259  ce 17 06
    INC ram_0627                        ; A25C  ee 27 06
    LDA $9908,Y                         ; A25F  b9 08 99
    CMP #$FF                            ; A262  c9 ff
    BNE loc_19_A267                     ; A264  d0 01
    RTS                                 ; A266  60
loc_19_A267:
    STA ram_0627                        ; A267  8d 27 06
    RTS                                 ; A26A  60
sub_19_A26B:
    LDA ram_0617                        ; A26B  ad 17 06
    CMP #$05                            ; A26E  c9 05
    BEQ loc_19_A273                     ; A270  f0 01
    RTS                                 ; A272  60
loc_19_A273:
    LDA #$1F                            ; A273  a9 1f
    STA ram_06B2                        ; A275  8d b2 06
    INC title_step                      ; A278  ee f1 06
    RTS                                 ; A27B  60
sub_19_A27C:
    LDA ram_0617                        ; A27C  ad 17 06
    BEQ loc_19_A282                     ; A27F  f0 01
    RTS                                 ; A281  60
loc_19_A282:
    JSR MenuPalFadeIn                   ; A282  20 fd 82
    INC title_step                      ; A285  ee f1 06
    LDA #$00                            ; A288  a9 00
    STA $92                             ; A28A  85 92
loc_19_A28C:
    LDX $92                             ; A28C  a6 92
    INC $92                             ; A28E  e6 92
    LDA $B7C3,X                         ; A290  bd c3 b7
    STA $06CB                           ; A293  8d cb 06
    TXA                                 ; A296  8a
    ASL A                               ; A297  0a
    TAY                                 ; A298  a8
    LDA $B7C7,Y                         ; A299  b9 c7 b7
    STA obj_x_hi                        ; A29C  85 90
    LDA $B7C8,Y                         ; A29E  b9 c8 b7
    STA $91                             ; A2A1  85 91
    LDA #$00                            ; A2A3  a9 00
    STA $93                             ; A2A5  85 93
    LDA #$84                            ; A2A7  a9 84
    STA $84                             ; A2A9  85 84
    LDA #$20                            ; A2AB  a9 20
    STA zp_85                           ; A2AD  85 85
    LDY $92                             ; A2AF  a4 92
    LDA $A349,Y                         ; A2B1  b9 49 a3
    STA $06B4                           ; A2B4  8d b4 06
    LDA #$27                            ; A2B7  a9 27
    STA $0220                           ; A2B9  8d 20 02
    LDA #$22                            ; A2BC  a9 22
    STA $0221                           ; A2BE  8d 21 02
    LDA #$03                            ; A2C1  a9 03
    STA $0222                           ; A2C3  8d 22 02
    LDA #$20                            ; A2C6  a9 20
    STA $0223                           ; A2C8  8d 23 02
    LDX #$17                            ; A2CB  a2 17
    LDA #$10                            ; A2CD  a9 10
loc_19_A2CF:
    STA $6018,X                         ; A2CF  9d 18 60
    DEX                                 ; A2D2  ca
    BPL loc_19_A2CF                     ; A2D3  10 fa
    LDA #$5A                            ; A2D5  a9 5a
    STA ram_06B2                        ; A2D7  8d b2 06
    JSR sub_19_A092                     ; A2DA  20 92 a0
    JSR sub_18_8174                     ; A2DD  20 74 81
    LDX tmp_0B                          ; A2E0  a6 0b
    LDA zp_85                           ; A2E2  a5 85
    STA ppu_queue,X                     ; A2E4  9d 00 62
    STA $6204,X                         ; A2E7  9d 04 62
    LDA $84                             ; A2EA  a5 84
    STA ppu_queue_1,X                   ; A2EC  9d 01 62
    CLC                                 ; A2EF  18
    ADC #$20                            ; A2F0  69 20
    STA $6205,X                         ; A2F2  9d 05 62
    LDA #$18                            ; A2F5  a9 18
    STA ppu_queue_2,X                   ; A2F7  9d 02 62
    STA $6206,X                         ; A2FA  9d 06 62
    STA ppu_queue_3,X                   ; A2FD  9d 03 62
    STA $6207,X                         ; A300  9d 07 62
    TXA                                 ; A303  8a
    CLC                                 ; A304  18
    ADC #$08                            ; A305  69 08
    AND #$1F                            ; A307  29 1f
    STA tmp_0B                          ; A309  85 0b
    JSR sub_18_817C                     ; A30B  20 7c 81
    JSR WaitFrameReady                  ; A30E  20 98 81
    JSR sub_19_A092                     ; A311  20 92 a0
    JSR sub_18_8174                     ; A314  20 74 81
    LDX tmp_0B                          ; A317  a6 0b
    LDA zp_85                           ; A319  a5 85
    CLC                                 ; A31B  18
    ADC #$08                            ; A31C  69 08
    STA ppu_queue,X                     ; A31E  9d 00 62
    STA $6204,X                         ; A321  9d 04 62
    LDA $84                             ; A324  a5 84
    STA ppu_queue_1,X                   ; A326  9d 01 62
    CLC                                 ; A329  18
    ADC #$20                            ; A32A  69 20
    STA $6205,X                         ; A32C  9d 05 62
    LDA #$18                            ; A32F  a9 18
    STA ppu_queue_2,X                   ; A331  9d 02 62
    STA $6206,X                         ; A334  9d 06 62
    STA ppu_queue_3,X                   ; A337  9d 03 62
    STA $6207,X                         ; A33A  9d 07 62
    TXA                                 ; A33D  8a
    CLC                                 ; A33E  18
    ADC #$08                            ; A33F  69 08
    AND #$1F                            ; A341  29 1f
    STA tmp_0B                          ; A343  85 0b
    JSR sub_18_817C                     ; A345  20 7c 81
    RTS                                 ; A348  60
    .db $00,$20,$20,$20,$01             ; 19:A349
sub_19_A34E:
    LDA ram_06FF                        ; A34E  ad ff 06
    AND #$01                            ; A351  29 01
    BEQ loc_19_A356                     ; A353  f0 01
    RTS                                 ; A355  60
loc_19_A356:
    LDA $06CB                           ; A356  ad cb 06
    BMI loc_19_A3BD                     ; A359  30 62
    LDY $93                             ; A35B  a4 93
    LDA (obj_x_hi),Y                    ; A35D  b1 90
    STA wram_6002                       ; A35F  8d 02 60
    INY                                 ; A362  c8
    LDA (obj_x_hi),Y                    ; A363  b1 90
    STA wram_6003                       ; A365  8d 03 60
    INY                                 ; A368  c8
    STY $93                             ; A369  84 93
    JSR sub_19_A092                     ; A36B  20 92 a0
    JSR sub_18_8174                     ; A36E  20 74 81
    LDX tmp_0B                          ; A371  a6 0b
    LDA zp_85                           ; A373  a5 85
    ORA #$C0                            ; A375  09 c0
    STA ppu_queue,X                     ; A377  9d 00 62
    CLC                                 ; A37A  18
    ADC #$08                            ; A37B  69 08
    STA $6204,X                         ; A37D  9d 04 62
    LDA $84                             ; A380  a5 84
    STA ppu_queue_1,X                   ; A382  9d 01 62
    STA $6205,X                         ; A385  9d 05 62
    LDA #$02                            ; A388  a9 02
    STA ppu_queue_2,X                   ; A38A  9d 02 62
    STA ppu_queue_3,X                   ; A38D  9d 03 62
    STA $6206,X                         ; A390  9d 06 62
    STA $6207,X                         ; A393  9d 07 62
    TXA                                 ; A396  8a
    CLC                                 ; A397  18
    ADC #$08                            ; A398  69 08
    AND #$1F                            ; A39A  29 1f
    STA tmp_0B                          ; A39C  85 0b
    INC $84                             ; A39E  e6 84
    JSR sub_18_817C                     ; A3A0  20 7c 81
    LDA #$27                            ; A3A3  a9 27
    STA $0220                           ; A3A5  8d 20 02
    LDA $0223                           ; A3A8  ad 23 02
    CLC                                 ; A3AB  18
    ADC #$08                            ; A3AC  69 08
    STA $0223                           ; A3AE  8d 23 02
    DEC $06CB                           ; A3B1  ce cb 06
    BMI loc_19_A3B7                     ; A3B4  30 01
    RTS                                 ; A3B6  60
loc_19_A3B7:
    LDA #$F0                            ; A3B7  a9 f0
    STA $0220                           ; A3B9  8d 20 02
    RTS                                 ; A3BC  60
loc_19_A3BD:
    DEC $06B4                           ; A3BD  ce b4 06
    BEQ loc_19_A3C3                     ; A3C0  f0 01
    RTS                                 ; A3C2  60
loc_19_A3C3:
    LDA $92                             ; A3C3  a5 92
    CMP #$04                            ; A3C5  c9 04
    BEQ loc_19_A3CC                     ; A3C7  f0 03
    JMP loc_19_A28C                     ; A3C9  4c 8c a2
loc_19_A3CC:
    LDA #$20                            ; A3CC  a9 20
    STA $06DD                           ; A3CE  8d dd 06
    LDA #$00                            ; A3D1  a9 00
    STA $614D                           ; A3D3  8d 4d 61
    INC state3_mode                     ; A3D6  ee f3 06
    INC title_step                      ; A3D9  ee f1 06
    RTS                                 ; A3DC  60
sub_19_A3DD:
    INC title_step                      ; A3DD  ee f1 06
    LDX #$05                            ; A3E0  a2 05
    LDA #$10                            ; A3E2  a9 10
loc_19_A3E4:
    STA player_name_hi,X                ; A3E4  9d 10 64
    STA player_name6,X                  ; A3E7  9d 18 64
    DEX                                 ; A3EA  ca
    BPL loc_19_A3E4                     ; A3EB  10 f7
    LDA #$00                            ; A3ED  a9 00
    STA $6420                           ; A3EF  8d 20 64
    STA $6423                           ; A3F2  8d 23 64
    RTS                                 ; A3F5  60
NameEntryInput:  ; s38: title_step 5; B=back cursor; START=finish (19:A4E3; all-blank name -> default from 19:A56C); A=type $06CC at $6420 (F0/F1/F2 = end/right/left)
    INC $6423                           ; A3F6  ee 23 64
    LDA #$49                            ; A3F9  a9 49
    STA $0220                           ; A3FB  8d 20 02
    LDA #$22                            ; A3FE  a9 22
    STA $0221                           ; A400  8d 21 02
    LDA #$03                            ; A403  a9 03
    STA $0222                           ; A405  8d 22 02
    LDA $6420                           ; A408  ad 20 64
    ASL A                               ; A40B  0a
    ASL A                               ; A40C  0a
    ASL A                               ; A40D  0a
    CLC                                 ; A40E  18
    ADC #$68                            ; A40F  69 68
    STA $0223                           ; A411  8d 23 02
    JSR NameEntryCursor                 ; A414  20 85 a5
    LDA pad_new                         ; A417  a5 4b
    AND #$40                            ; A419  29 40
    BEQ loc_19_A420                     ; A41B  f0 03
    JMP loc_19_A4BF                     ; A41D  4c bf a4
loc_19_A420:
    LDA pad_new                         ; A420  a5 4b
    AND #$10                            ; A422  29 10
    BEQ loc_19_A429                     ; A424  f0 03
    JMP loc_19_A4E3                     ; A426  4c e3 a4
loc_19_A429:
    LDA pad_new                         ; A429  a5 4b
    AND #$80                            ; A42B  29 80
    BNE loc_19_A430                     ; A42D  d0 01
    RTS                                 ; A42F  60
loc_19_A430:
    LDA $06CC                           ; A430  ad cc 06
    CMP #$F2                            ; A433  c9 f2
    BNE loc_19_A43A                     ; A435  d0 03
    JMP loc_19_A4BF                     ; A437  4c bf a4
loc_19_A43A:
    CMP #$F1                            ; A43A  c9 f1
    BNE loc_19_A441                     ; A43C  d0 03
    JMP loc_19_A4D0                     ; A43E  4c d0 a4
loc_19_A441:
    CMP #$F0                            ; A441  c9 f0
    BNE loc_19_A448                     ; A443  d0 03
    JMP loc_19_A4E3                     ; A445  4c e3 a4
loc_19_A448:
    LDA #$58                            ; A448  a9 58
    STA ram_06B2                        ; A44A  8d b2 06
    LDX $6420                           ; A44D  ae 20 64
    LDA $06CC                           ; A450  ad cc 06
    STA wram_6002                       ; A453  8d 02 60
    STA player_name_hi,X                ; A456  9d 10 64
    LDA ram_06CD                        ; A459  ad cd 06
    STA wram_6003                       ; A45C  8d 03 60
    STA player_name6,X                  ; A45F  9d 18 64
    TXA                                 ; A462  8a
    INX                                 ; A463  e8
    CPX #$06                            ; A464  e0 06
    BNE loc_19_A46A                     ; A466  d0 02
    LDX #$00                            ; A468  a2 00
loc_19_A46A:
    STX $6420                           ; A46A  8e 20 64
    CLC                                 ; A46D  18
    ADC $A4B1                           ; A46E  6d b1 a4
    STA zp_72                           ; A471  85 72
    LDA #$00                            ; A473  a9 00
    ADC $A4B2                           ; A475  6d b2 a4
    STA zp_73                           ; A478  85 73
    JSR sub_19_A092                     ; A47A  20 92 a0
    JSR sub_18_8174                     ; A47D  20 74 81
    LDX tmp_0B                          ; A480  a6 0b
    LDA zp_73                           ; A482  a5 73
    ORA #$C0                            ; A484  09 c0
    STA ppu_queue,X                     ; A486  9d 00 62
    CLC                                 ; A489  18
    ADC #$08                            ; A48A  69 08
    STA $6204,X                         ; A48C  9d 04 62
    LDA zp_72                           ; A48F  a5 72
    STA ppu_queue_1,X                   ; A491  9d 01 62
    STA $6205,X                         ; A494  9d 05 62
    LDA #$02                            ; A497  a9 02
    STA ppu_queue_2,X                   ; A499  9d 02 62
    STA $6206,X                         ; A49C  9d 06 62
    STA ppu_queue_3,X                   ; A49F  9d 03 62
    STA $6207,X                         ; A4A2  9d 07 62
    TXA                                 ; A4A5  8a
    CLC                                 ; A4A6  18
    ADC #$08                            ; A4A7  69 08
    AND #$1F                            ; A4A9  29 1f
    STA tmp_0B                          ; A4AB  85 0b
    JSR sub_18_817C                     ; A4AD  20 7c 81
    RTS                                 ; A4B0  60
    .db $0D,$21,$A9,$00,$8D,$23,$64,$A5,$05,$49,$01,$85,$05,$60; 19:A4B1
loc_19_A4BF:
    LDA #$00                            ; A4BF  a9 00
    STA $6423                           ; A4C1  8d 23 64
    LDX $6420                           ; A4C4  ae 20 64
    DEX                                 ; A4C7  ca
    BPL loc_19_A4CC                     ; A4C8  10 02
    LDX #$05                            ; A4CA  a2 05
loc_19_A4CC:
    STX $6420                           ; A4CC  8e 20 64
    RTS                                 ; A4CF  60
loc_19_A4D0:
    LDA #$00                            ; A4D0  a9 00
    STA $6423                           ; A4D2  8d 23 64
    LDX $6420                           ; A4D5  ae 20 64
    INX                                 ; A4D8  e8
    CPX #$06                            ; A4D9  e0 06
    BNE loc_19_A4DF                     ; A4DB  d0 02
    LDX #$00                            ; A4DD  a2 00
loc_19_A4DF:
    STX $6420                           ; A4DF  8e 20 64
    RTS                                 ; A4E2  60
loc_19_A4E3:
    LDX #$05                            ; A4E3  a2 05
loc_19_A4E5:
    LDA player_name6,X                  ; A4E5  bd 18 64
    CMP #$10                            ; A4E8  c9 10
    BNE loc_19_A4F2                     ; A4EA  d0 06
    DEX                                 ; A4EC  ca
    BPL loc_19_A4E5                     ; A4ED  10 f6
    JMP loc_19_A53B                     ; A4EF  4c 3b a5
loc_19_A4F2:
    INX                                 ; A4F2  e8
    STX $6420                           ; A4F3  8e 20 64
    STX $6421                           ; A4F6  8e 21 64
    LDY #$00                            ; A4F9  a0 00
    LDX #$00                            ; A4FB  a2 00
loc_19_A4FD:
    LDA player_name_hi,Y                ; A4FD  b9 10 64
    CMP #$76                            ; A500  c9 76
    BEQ loc_19_A511                     ; A502  f0 0d
    CMP #$75                            ; A504  c9 75
    BEQ loc_19_A520                     ; A506  f0 18
    LDA player_name6,Y                  ; A508  b9 18 64
    STA player_name,X                   ; A50B  9d 00 64
    JMP loc_19_A528                     ; A50E  4c 28 a5
loc_19_A511:
    LDA player_name6,Y                  ; A511  b9 18 64
    STA player_name,X                   ; A514  9d 00 64
    INX                                 ; A517  e8
    LDA #$F8                            ; A518  a9 f8
    STA player_name,X                   ; A51A  9d 00 64
    JMP loc_19_A528                     ; A51D  4c 28 a5
loc_19_A520:
    LDA player_name6,Y                  ; A520  b9 18 64
    ORA #$80                            ; A523  09 80
    STA player_name,X                   ; A525  9d 00 64
loc_19_A528:
    INX                                 ; A528  e8
    INY                                 ; A529  c8
    DEC $6421                           ; A52A  ce 21 64
    BNE loc_19_A4FD                     ; A52D  d0 ce
    LDA #$00                            ; A52F  a9 00
    STA player_name,X                   ; A531  9d 00 64
    JSR TitleFadeOutStart               ; A534  20 44 83
    INC title_step                      ; A537  ee f1 06
    RTS                                 ; A53A  60
loc_19_A53B:
    LDY #$00                            ; A53B  a0 00
    LDA $A56C,Y                         ; A53D  b9 6c a5
    STA $92                             ; A540  85 92
    LDA $A56D,Y                         ; A542  b9 6d a5
    STA $93                             ; A545  85 93
    LDY #$00                            ; A547  a0 00
    LDX #$00                            ; A549  a2 00
loc_19_A54B:
    LDA ($92),Y                         ; A54B  b1 92
    PHP                                 ; A54D  08
    INY                                 ; A54E  c8
    PLP                                 ; A54F  28
    BEQ loc_19_A559                     ; A550  f0 07
    STA player_name6,X                  ; A552  9d 18 64
    INX                                 ; A555  e8
    JMP loc_19_A54B                     ; A556  4c 4b a5
loc_19_A559:
    LDX #$00                            ; A559  a2 00
loc_19_A55B:
    LDA ($92),Y                         ; A55B  b1 92
    PHP                                 ; A55D  08
    INY                                 ; A55E  c8
    PLP                                 ; A55F  28
    BEQ loc_19_A569                     ; A560  f0 07
    STA player_name_hi,X                ; A562  9d 10 64
    INX                                 ; A565  e8
    JMP loc_19_A55B                     ; A566  4c 5b a5
loc_19_A569:
    JMP loc_19_A4E3                     ; A569  4c e3 a4
    .db $6E,$A5,$53,$20,$4E,$20,$4B,$20,$00,$20,$20,$20,$20,$20,$20,$00; 19:A56C
NameEntryDone:  ; s38: title_step 6 -> 19:A6F9 starts the game (main_state 0 written at 19:A6FD; gameplay $40=1 two frames later)
    JSR MenuPalBlack                    ; A57C  20 b3 82
    JSR WaitFrameReady                  ; A57F  20 98 81
    JMP StartNewGame                    ; A582  4c f9 a6
NameEntryCursor:  ; s38: pad_new bits 0-3 move grid cursor $8A (col 0-31 wrap) / $8B (row $10-$16 step 2); skipping $FF cells of 19:9EC2
    LDA pad_new                         ; A585  a5 4b
    AND #$0F                            ; A587  29 0f
    BEQ loc_19_A590                     ; A589  f0 05
    LDA #$4B                            ; A58B  a9 4b
    STA ram_06B2                        ; A58D  8d b2 06
loc_19_A590:
    LDA pad_new                         ; A590  a5 4b
    STA $06B6                           ; A592  8d b6 06
    ROR $06B6                           ; A595  6e b6 06
    BCC loc_19_A5B3                     ; A598  90 19
loc_19_A59A:
    INC $8A                             ; A59A  e6 8a
    LDA $8A                             ; A59C  a5 8a
    CMP #$20                            ; A59E  c9 20
    BNE loc_19_A5A4                     ; A5A0  d0 02
    LDA #$00                            ; A5A2  a9 00
loc_19_A5A4:
    STA $8A                             ; A5A4  85 8a
    JSR sub_19_A0A5                     ; A5A6  20 a5 a0
    LDA NameCellGrid,Y                  ; A5A9  b9 c2 9e
    CMP #$FF                            ; A5AC  c9 ff
    BEQ loc_19_A59A                     ; A5AE  f0 ea
    JMP loc_19_A60C                     ; A5B0  4c 0c a6
loc_19_A5B3:
    ROR $06B6                           ; A5B3  6e b6 06
    BCC loc_19_A5CF                     ; A5B6  90 17
loc_19_A5B8:
    DEC $8A                             ; A5B8  c6 8a
    LDA $8A                             ; A5BA  a5 8a
    BPL loc_19_A5C0                     ; A5BC  10 02
    LDA #$1F                            ; A5BE  a9 1f
loc_19_A5C0:
    STA $8A                             ; A5C0  85 8a
    JSR sub_19_A0A5                     ; A5C2  20 a5 a0
    LDA NameCellGrid,Y                  ; A5C5  b9 c2 9e
    CMP #$FF                            ; A5C8  c9 ff
    BEQ loc_19_A5B8                     ; A5CA  f0 ec
    JMP loc_19_A60C                     ; A5CC  4c 0c a6
loc_19_A5CF:
    ROR $06B6                           ; A5CF  6e b6 06
    BCC loc_19_A5EF                     ; A5D2  90 1b
loc_19_A5D4:
    INC $8B                             ; A5D4  e6 8b
    INC $8B                             ; A5D6  e6 8b
    LDA $8B                             ; A5D8  a5 8b
    CMP #$18                            ; A5DA  c9 18
    BNE loc_19_A5E0                     ; A5DC  d0 02
    LDA #$10                            ; A5DE  a9 10
loc_19_A5E0:
    STA $8B                             ; A5E0  85 8b
    JSR sub_19_A0A5                     ; A5E2  20 a5 a0
    LDA NameCellGrid,Y                  ; A5E5  b9 c2 9e
    CMP #$FF                            ; A5E8  c9 ff
    BEQ loc_19_A5D4                     ; A5EA  f0 e8
    JMP loc_19_A60C                     ; A5EC  4c 0c a6
loc_19_A5EF:
    ROR $06B6                           ; A5EF  6e b6 06
    BCC loc_19_A60C                     ; A5F2  90 18
loc_19_A5F4:
    DEC $8B                             ; A5F4  c6 8b
    DEC $8B                             ; A5F6  c6 8b
    LDA $8B                             ; A5F8  a5 8b
    CMP #$0E                            ; A5FA  c9 0e
    BNE loc_19_A600                     ; A5FC  d0 02
    LDA #$16                            ; A5FE  a9 16
loc_19_A600:
    STA $8B                             ; A600  85 8b
    JSR sub_19_A0A5                     ; A602  20 a5 a0
    LDA NameCellGrid,Y                  ; A605  b9 c2 9e
    CMP #$FF                            ; A608  c9 ff
    BEQ loc_19_A5F4                     ; A60A  f0 e8
loc_19_A60C:
    LDY $8B                             ; A60C  a4 8b
    INY                                 ; A60E  c8
    TYA                                 ; A60F  98
    ASL A                               ; A610  0a
    ASL A                               ; A611  0a
    ASL A                               ; A612  0a
    SEC                                 ; A613  38
    ADC #$00                            ; A614  69 00
    STA $0208                           ; A616  8d 08 02
    LDA #$22                            ; A619  a9 22
    STA $0209                           ; A61B  8d 09 02
    LDA #$03                            ; A61E  a9 03
    STA $020A                           ; A620  8d 0a 02
    LDA $8A                             ; A623  a5 8a
    ASL A                               ; A625  0a
    ASL A                               ; A626  0a
    ASL A                               ; A627  0a
    STA $020B                           ; A628  8d 0b 02
    JSR sub_19_A0A5                     ; A62B  20 a5 a0
    LDA NameCellGrid,Y                  ; A62E  b9 c2 9e
    STA $06CC                           ; A631  8d cc 06
    LDA NameGlyphGrid,Y                 ; A634  b9 42 9f
    STA ram_06CD                        ; A637  8d cd 06
    LDA cam_y_hi                        ; A63A  a5 05
    BNE loc_19_A63F                     ; A63C  d0 01
    RTS                                 ; A63E  60
loc_19_A63F:
    LDA ram_06CD                        ; A63F  ad cd 06
    CMP #$10                            ; A642  c9 10
    BNE loc_19_A647                     ; A644  d0 01
    RTS                                 ; A646  60
loc_19_A647:
    CMP #$0C                            ; A647  c9 0c
    BNE loc_19_A64C                     ; A649  d0 01
    RTS                                 ; A64B  60
loc_19_A64C:
    CLC                                 ; A64C  18
    ADC #$32                            ; A64D  69 32
    STA ram_06CD                        ; A64F  8d cd 06
    RTS                                 ; A652  60
TitleMenuInit:  ; s67: title_mode 5: CHR $1C/$1E/$73/$7B
    JSR MenuPalBlack                    ; A653  20 b3 82
    JSR WaitFrameReady                  ; A656  20 98 81
    LDA #$28                            ; A659  a9 28
    LDX #$FF                            ; A65B  a2 ff
    LDY #$00                            ; A65D  a0 00
    JSR NtFill                          ; A65F  20 a1 81
    LDA #$1C                            ; A662  a9 1c
    STA chr_r0_play                     ; A664  8d f0 07
    LDA #$1E                            ; A667  a9 1e
    STA chr_r1_play                     ; A669  8d f1 07
    LDA #$73                            ; A66C  a9 73
    STA chr_r3_play                     ; A66E  8d f3 07
    LDA #$7B                            ; A671  a9 7b
    STA chr_r5_play                     ; A673  8d f5 07
    LDA #$00                            ; A676  a9 00
    STA title_step                      ; A678  8d f1 06
    STA ram_06F2                        ; A67B  8d f2 06
    STA state3_mode                     ; A67E  8d f3 06
    STA scroll_shadow0                  ; A681  85 02
    STA cam_y_lo                        ; A683  85 04
    STA cam_x_hi                        ; A685  85 03
    STA cam_y_hi                        ; A687  85 05
    LDA #$08                            ; A689  a9 08
    STA cam_y_lo                        ; A68B  85 04
    JSR OamHideAll18                    ; A68D  20 d8 81
    JSR WaitFrameReady                  ; A690  20 98 81
    LDA #$00                            ; A693  a9 00
    STA ram_06FE                        ; A695  8d fe 06
    LDA $94BE                           ; A698  ad be 94
    STA zp_8E                           ; A69B  85 8e
    LDA $94BF                           ; A69D  ad bf 94
    STA zp_8F                           ; A6A0  85 8f
    LDA #$00                            ; A6A2  a9 00
    STA zp_8C                           ; A6A4  85 8c
    LDA #$20                            ; A6A6  a9 20
    STA zp_8D                           ; A6A8  85 8d
    JSR NtRectBlit                      ; A6AA  20 f0 81
    LDA #$0C                            ; A6AD  a9 0c
    JSR MenuPalLoad                     ; A6AF  20 be 82
    LDA #$8E                            ; A6B2  a9 8e
    JSR MenuPalLoad                     ; A6B4  20 be 82
    JSR MenuPalFadeIn                   ; A6B7  20 fd 82
    LDA $A6DD                           ; A6BA  ad dd a6
    STA zp_82                           ; A6BD  85 82
    LDA $A6DE                           ; A6BF  ad de a6
    STA $83                             ; A6C2  85 83
    LDA #$0E                            ; A6C4  a9 0e
    STA $84                             ; A6C6  85 84
    LDA #$96                            ; A6C8  a9 96
    STA $06B4                           ; A6CA  8d b4 06
    LDA #$02                            ; A6CD  a9 02
    STA $06B5                           ; A6CF  8d b5 06
    LDA #$0B                            ; A6D2  a9 0b
    STA ram_06B2                        ; A6D4  8d b2 06
    INC title_mode                      ; A6D7  ee f0 06
    JMP TitleMenu                       ; A6DA  4c df a6
    .db $C2,$98                         ; 19:A6DD
TitleMenu:  ; s67: title_mode 6: JmpTable19_A6F3[title_step]
    LDA title_step                      ; A6DF  ad f1 06
    ASL A                               ; A6E2  0a
    TAY                                 ; A6E3  a8
    LDA JmpTable19_A6F3,Y               ; A6E4  b9 f3 a6
    STA ram_06FC                        ; A6E7  8d fc 06
    LDA $A6F4,Y                         ; A6EA  b9 f4 a6
    STA ram_06FD                        ; A6ED  8d fd 06
    JMP (ram_06FC)                      ; A6F0  6c fc 06
JmpTable19_A6F3:  ; s67: title_mode 6 title-menu steps [title_step]: 3 entries A6FE / A749 TitleMenuInput / A796 TitleMenuRestart (JUMP_TABLES)
    .dw sub_19_A6FE                     ; 19:A6F3
    .dw TitleMenuInput                  ; 19:A6F5
    .dw TitleMenuRestart                ; 19:A6F7
StartNewGame:  ; s71: main_state=0 (NameEntryDone JMPs here) -> MainStateNewGame 30:C957
    LDA #$00                            ; A6F9  a9 00
    STA main_state                      ; A6FB  85 40
    RTS                                 ; A6FD  60
sub_19_A6FE:
    LDY #$00                            ; A6FE  a0 00
    LDA (zp_82),Y                       ; A700  b1 82
    TAX                                 ; A702  aa
    INY                                 ; A703  c8
    LDA (zp_82),Y                       ; A704  b1 82
    STA oam_y,X                         ; A706  9d 00 02
    INY                                 ; A709  c8
    LDA (zp_82),Y                       ; A70A  b1 82
    STA oam_tile,X                      ; A70C  9d 01 02
    INY                                 ; A70F  c8
    LDA (zp_82),Y                       ; A710  b1 82
    STA oam_attr,X                      ; A712  9d 02 02
    INY                                 ; A715  c8
    LDA (zp_82),Y                       ; A716  b1 82
    STA oam_x,X                         ; A718  9d 03 02
    LDA zp_82                           ; A71B  a5 82
    CLC                                 ; A71D  18
    ADC #$05                            ; A71E  69 05
    STA zp_82                           ; A720  85 82
    LDA $83                             ; A722  a5 83
    ADC #$00                            ; A724  69 00
    STA $83                             ; A726  85 83
    DEC $84                             ; A728  c6 84
    BNE sub_19_A6FE                     ; A72A  d0 d2
    INC title_step                      ; A72C  ee f1 06
    LDA #$00                            ; A72F  a9 00
    STA zp_82                           ; A731  85 82
    LDA #$AF                            ; A733  a9 af
    STA $02F0                           ; A735  8d f0 02
    LDA #$7E                            ; A738  a9 7e
    STA $02F1                           ; A73A  8d f1 02
    LDA #$00                            ; A73D  a9 00
    STA $02F2                           ; A73F  8d f2 02
    LDA $A77B                           ; A742  ad 7b a7
    STA $02F3                           ; A745  8d f3 02
    RTS                                 ; A748  60
TitleMenuInput:  ; s63: SELECT toggles $82 (cursor x 19:A77B); START -> 19:A77D; idle timer $06B4/$06B5
    LDA pad_new                         ; A749  a5 4b
    AND #$10                            ; A74B  29 10
    BNE TitleMenuStart                  ; A74D  d0 2e
    LDA pad_new                         ; A74F  a5 4b
    AND #$20                            ; A751  29 20
    BNE loc_19_A768                     ; A753  d0 13
    DEC $06B4                           ; A755  ce b4 06
    BEQ loc_19_A75B                     ; A758  f0 01
    RTS                                 ; A75A  60
loc_19_A75B:
    DEC $06B5                           ; A75B  ce b5 06
    BEQ loc_19_A761                     ; A75E  f0 01
    RTS                                 ; A760  60
loc_19_A761:
    JSR TitleFadeOutStart               ; A761  20 44 83
    INC title_step                      ; A764  ee f1 06
    RTS                                 ; A767  60
loc_19_A768:
    LDA #$4B                            ; A768  a9 4b
    STA ram_06B2                        ; A76A  8d b2 06
    LDA zp_82                           ; A76D  a5 82
    EOR #$01                            ; A76F  49 01
    STA zp_82                           ; A771  85 82
    TAY                                 ; A773  a8
    LDA $A77B,Y                         ; A774  b9 7b a7
    STA $02F3                           ; A777  8d f3 02
    RTS                                 ; A77A  60
    .db $40,$80                         ; 19:A77B
TitleMenuStart:  ; s63: $82=1 -> main_state=8 (continue) else title_mode=1 (new game)
    LDA #$00                            ; A77D  a9 00
    JSR sub_19_A07C                     ; A77F  20 7c a0
    LDA #$09                            ; A782  a9 09
    STA ram_06B2                        ; A784  8d b2 06
    LDA zp_82                           ; A787  a5 82
    BEQ loc_19_A790                     ; A789  f0 05
    LDA #$08                            ; A78B  a9 08
    STA main_state                      ; A78D  85 40
    RTS                                 ; A78F  60
loc_19_A790:
    LDA #$01                            ; A790  a9 01
    STA title_mode                      ; A792  8d f0 06
    RTS                                 ; A795  60
TitleMenuRestart:  ; s67: title-menu step 2: DEC main_state (3 -> 2
    DEC main_state                      ; A796  c6 40
    JSR MenuPalBlack                    ; A798  20 b3 82
    JSR WaitFrameReady                  ; A79B  20 98 81
    RTS                                 ; A79E  60
TitleOpening:  ; s67: title_mode 0: START/SELECT -> A7EE (A07C(0); TitleOpeningEnd) else JmpTable19_A7BC[title_step]
    LDA pad_new                         ; A79F  a5 4b
    AND #$30                            ; A7A1  29 30
    BEQ loc_19_A7A8                     ; A7A3  f0 03
    JMP loc_19_A7EE                     ; A7A5  4c ee a7
loc_19_A7A8:
    LDA title_step                      ; A7A8  ad f1 06
    ASL A                               ; A7AB  0a
    TAY                                 ; A7AC  a8
    LDA JmpTable19_A7BC,Y               ; A7AD  b9 bc a7
    STA ram_06FC                        ; A7B0  8d fc 06
    LDA $A7BD,Y                         ; A7B3  b9 bd a7
    STA ram_06FD                        ; A7B6  8d fd 06
    JMP (ram_06FC)                      ; A7B9  6c fc 06
JmpTable19_A7BC:  ; s67: title_mode 0 opening steps [title_step]: exactly 25 entries (ends at code A7EE); 9FC2 = TitleStepWait / A064 = TitleStepFadeOut fillers; last = ABAB TitleOpeningEnd (JUMP_TABLES)
    .dw sub_19_A070                     ; 19:A7BC
    .dw Sub18_9FC2                      ; 19:A7BE
    .dw OpeningDateCard                 ; 19:A7C0
    .dw Sub18_9FC2                      ; 19:A7C2
    .dw TitleStepFadeOut                ; 19:A7C4
    .dw Sub18_9FC2                      ; 19:A7C6
    .dw OpeningPage1                    ; 19:A7C8
    .dw OpeningPage1Anim                ; 19:A7CA
    .dw OpeningPage1Flash               ; 19:A7CC
    .dw TitleStepFadeOut                ; 19:A7CE
    .dw Sub18_9FC2                      ; 19:A7D0
    .dw OpeningPage2                    ; 19:A7D2
    .dw OpeningPage2Flip                ; 19:A7D4
    .dw OpeningPage2Flip                ; 19:A7D6
    .dw TitleStepFadeOut                ; 19:A7D8
    .dw Sub18_9FC2                      ; 19:A7DA
    .dw OpeningPage3                    ; 19:A7DC
    .dw OpeningPage3Anim                ; 19:A7DE
    .dw TitleStepFadeOut                ; 19:A7E0
    .dw Sub18_9FC2                      ; 19:A7E2
    .dw OpeningPage4                    ; 19:A7E4
    .dw OpeningPage4Anim                ; 19:A7E6
    .dw TitleStepFadeOut                ; 19:A7E8
    .dw Sub18_9FC2                      ; 19:A7EA
    .dw TitleOpeningEnd                 ; 19:A7EC
loc_19_A7EE:
    LDA #$00                            ; A7EE  a9 00
    JSR sub_19_A07C                     ; A7F0  20 7c a0
    JMP TitleOpeningEnd                 ; A7F3  4c ab ab
OpeningDateCard:  ; s69: opening step 2: NtFill $2000; date-card text 30x3 (DateCardRect) at $21A0 bias 0; BG set 0; fade-in; wait $78
    JSR MenuPalBlack                    ; A7F6  20 b3 82
    JSR OamHideAll18                    ; A7F9  20 d8 81
    LDA #$3C                            ; A7FC  a9 3c
    STA chr_r0_play                     ; A7FE  8d f0 07
    LDA #$34                            ; A801  a9 34
    STA chr_r1_play                     ; A803  8d f1 07
    LDA #$20                            ; A806  a9 20
    LDX #$10                            ; A808  a2 10
    LDY #$FF                            ; A80A  a0 ff
    JSR NtFill                          ; A80C  20 a1 81
    LDA DateCardRectPtr                 ; A80F  ad 62 b1
    STA zp_8E                           ; A812  85 8e
    LDA $B163                           ; A814  ad 63 b1
    STA zp_8F                           ; A817  85 8f
    LDA #$A0                            ; A819  a9 a0
    STA zp_8C                           ; A81B  85 8c
    LDA #$21                            ; A81D  a9 21
    STA zp_8D                           ; A81F  85 8d
    LDA #$00                            ; A821  a9 00
    STA ram_06FE                        ; A823  8d fe 06
    STA cam_y_lo                        ; A826  85 04
    STA cam_y_hi                        ; A828  85 05
    JSR NtRectBlit                      ; A82A  20 f0 81
    LDA #$00                            ; A82D  a9 00
    JSR MenuPalLoad                     ; A82F  20 be 82
    JSR MenuPalFadeIn                   ; A832  20 fd 82
    LDA #$78                            ; A835  a9 78
    STA ram_06FE                        ; A837  8d fe 06
    LDA #$0A                            ; A83A  a9 0a
    STA ram_06B2                        ; A83C  8d b2 06
    INC title_step                      ; A83F  ee f1 06
    RTS                                 ; A842  60
OpeningPage1:  ; s69: opening step 6: R2-R5=$7C R1=$16; picture/text page drawn at 19:A859; BG set 0
    JSR MenuPalBlack                    ; A843  20 b3 82
    LDA #$7C                            ; A846  a9 7c
    STA chr_r2_play                     ; A848  8d f2 07
    STA chr_r3_play                     ; A84B  8d f3 07
    STA chr_r4_play                     ; A84E  8d f4 07
    STA chr_r5_play                     ; A851  8d f5 07
    LDA #$16                            ; A854  a9 16
    STA chr_r1_play                     ; A856  8d f1 07
    LDA #$20                            ; A859  a9 20
    LDX #$10                            ; A85B  a2 10
    LDY #$FF                            ; A85D  a0 ff
    JSR NtFill                          ; A85F  20 a1 81
    LDA #$00                            ; A862  a9 00
    STA ram_06FE                        ; A864  8d fe 06
    LDA $847B                           ; A867  ad 7b 84
    STA zp_8E                           ; A86A  85 8e
    LDA $847C                           ; A86C  ad 7c 84
    STA zp_8F                           ; A86F  85 8f
    LDA #$42                            ; A871  a9 42
    STA zp_8C                           ; A873  85 8c
    LDA #$20                            ; A875  a9 20
    STA zp_8D                           ; A877  85 8d
    JSR NtRectBlit                      ; A879  20 f0 81
    LDA $8C37                           ; A87C  ad 37 8c
    STA zp_8E                           ; A87F  85 8e
    LDA $8C38                           ; A881  ad 38 8c
    STA zp_8F                           ; A884  85 8f
    LDA #$C0                            ; A886  a9 c0
    STA zp_8C                           ; A888  85 8c
    LDA #$23                            ; A88A  a9 23
    STA zp_8D                           ; A88C  85 8d
    JSR NtRectBlit                      ; A88E  20 f0 81
    LDA $B1C0                           ; A891  ad c0 b1
    STA zp_8E                           ; A894  85 8e
    LDA $B1C1                           ; A896  ad c1 b1
    STA zp_8F                           ; A899  85 8f
    LDA #$40                            ; A89B  a9 40
    STA zp_8C                           ; A89D  85 8c
    LDA #$22                            ; A89F  a9 22
    STA zp_8D                           ; A8A1  85 8d
    JSR NtRectBlit                      ; A8A3  20 f0 81
    LDA #$00                            ; A8A6  a9 00
    JSR MenuPalLoad                     ; A8A8  20 be 82
    LDA #$80                            ; A8AB  a9 80
    JSR MenuPalLoad                     ; A8AD  20 be 82
    JSR MenuPalFadeIn                   ; A8B0  20 fd 82
    LDA #$00                            ; A8B3  a9 00
    STA obj_phase                       ; A8B5  8d 00 06
    STA ram_0618                        ; A8B8  8d 18 06
    STA obj_timer2                      ; A8BB  8d 20 06
    STA ram_0628                        ; A8BE  8d 28 06
    STA $06B3                           ; A8C1  8d b3 06
    INC title_step                      ; A8C4  ee f1 06
    RTS                                 ; A8C7  60
    .db $00,$04,$08,$0C,$FF,$49,$49,$49,$4A,$00; 19:A8C8
OpeningPage1Anim:  ; s69: opening step 7: SceneMetaDraw ($72/$73 from $0618/$0620; pos $80/$10) until done
    JSR OamHideAll18                    ; A8D2  20 d8 81
    LDA ram_0618                        ; A8D5  ad 18 06
    STA zp_72                           ; A8D8  85 72
    LDA obj_timer2                      ; A8DA  ad 20 06
    STA zp_73                           ; A8DD  85 73
    LDA #$80                            ; A8DF  a9 80
    STA zp_88                           ; A8E1  85 88
    LDA #$10                            ; A8E3  a9 10
    STA $89                             ; A8E5  85 89
    JSR SceneMetaDraw                   ; A8E7  20 e7 80
    LDA obj_phase                       ; A8EA  ad 00 06
    BNE loc_19_A8FB                     ; A8ED  d0 0c
    LDA $06C2                           ; A8EF  ad c2 06
    STA ram_0628                        ; A8F2  8d 28 06
    INC obj_phase                       ; A8F5  ee 00 06
    JMP loc_19_A92C                     ; A8F8  4c 2c a9
loc_19_A8FB:
    DEC ram_0628                        ; A8FB  ce 28 06
    BPL loc_19_A92C                     ; A8FE  10 2c
    LDA #$00                            ; A900  a9 00
    STA obj_phase                       ; A902  8d 00 06
    LDY $06B3                           ; A905  ac b3 06
    LDA $A8C8,Y                         ; A908  b9 c8 a8
    CMP obj_timer2                      ; A90B  cd 20 06
    BNE loc_19_A919                     ; A90E  d0 09
    INC $06B3                           ; A910  ee b3 06
    LDA $A8CD,Y                         ; A913  b9 cd a8
    STA ram_06B2                        ; A916  8d b2 06
loc_19_A919:
    INC obj_timer2                      ; A919  ee 20 06
    LDA obj_timer2                      ; A91C  ad 20 06
    CMP orbit_level                     ; A91F  cd c0 06
    BNE loc_19_A92C                     ; A922  d0 08
    LDA #$78                            ; A924  a9 78
    STA ram_06FE                        ; A926  8d fe 06
    INC title_step                      ; A929  ee f1 06
loc_19_A92C:
    RTS                                 ; A92C  60
OpeningPage1Flash:  ; s69: opening step 8: BG set + sprite set ($06FF>>1)&1 (flash every 2 frames) for $06FE frames
    LDA ram_06FF                        ; A92D  ad ff 06
    LSR A                               ; A930  4a
    AND #$01                            ; A931  29 01
    PHA                                 ; A933  48
    JSR MenuPalLoad                     ; A934  20 be 82
    PLA                                 ; A937  68
    CLC                                 ; A938  18
    ADC #$80                            ; A939  69 80
    JSR MenuPalLoad                     ; A93B  20 be 82
    JSR MenuPalApply                    ; A93E  20 f1 82
    DEC ram_06FE                        ; A941  ce fe 06
    BEQ loc_19_A947                     ; A944  f0 01
    RTS                                 ; A946  60
loc_19_A947:
    LDA #$00                            ; A947  a9 00
    JSR MenuPalLoad                     ; A949  20 be 82
    JSR MenuPalApply                    ; A94C  20 f1 82
    INC title_step                      ; A94F  ee f1 06
    RTS                                 ; A952  60
OpeningPage2:  ; s69: opening step 11: draws $2000 (A95E) + $2800 (A9B3) pages; BG set 2 (AA05) + fade-in; $06FE=$F0
    JSR MenuPalBlack                    ; A953  20 b3 82
    JSR OamHideAll18                    ; A956  20 d8 81
    LDA #$10                            ; A959  a9 10
    STA chr_r1_play                     ; A95B  8d f1 07
    LDA #$20                            ; A95E  a9 20
    LDX #$10                            ; A960  a2 10
    LDY #$FF                            ; A962  a0 ff
    JSR NtFill                          ; A964  20 a1 81
    LDA #$80                            ; A967  a9 80
    STA ram_06FE                        ; A969  8d fe 06
    LDA $8607                           ; A96C  ad 07 86
    STA zp_8E                           ; A96F  85 8e
    LDA $8608                           ; A971  ad 08 86
    STA zp_8F                           ; A974  85 8f
    LDA #$42                            ; A976  a9 42
    STA zp_8C                           ; A978  85 8c
    LDA #$20                            ; A97A  a9 20
    STA zp_8D                           ; A97C  85 8d
    JSR NtRectBlit                      ; A97E  20 f0 81
    LDA #$00                            ; A981  a9 00
    STA ram_06FE                        ; A983  8d fe 06
    LDA $8C37                           ; A986  ad 37 8c
    STA zp_8E                           ; A989  85 8e
    LDA $8C38                           ; A98B  ad 38 8c
    STA zp_8F                           ; A98E  85 8f
    LDA #$C0                            ; A990  a9 c0
    STA zp_8C                           ; A992  85 8c
    LDA #$23                            ; A994  a9 23
    STA zp_8D                           ; A996  85 8d
    JSR NtRectBlit                      ; A998  20 f0 81
    LDA $B25A                           ; A99B  ad 5a b2
    STA zp_8E                           ; A99E  85 8e
    LDA $B25B                           ; A9A0  ad 5b b2
    STA zp_8F                           ; A9A3  85 8f
    LDA #$40                            ; A9A5  a9 40
    STA zp_8C                           ; A9A7  85 8c
    LDA #$22                            ; A9A9  a9 22
    STA zp_8D                           ; A9AB  85 8d
    JSR NtRectBlit                      ; A9AD  20 f0 81
    JSR WaitFrameReady                  ; A9B0  20 98 81
    LDA #$28                            ; A9B3  a9 28
    LDX #$10                            ; A9B5  a2 10
    LDY #$FF                            ; A9B7  a0 ff
    JSR NtFill                          ; A9B9  20 a1 81
    LDA #$80                            ; A9BC  a9 80
    STA ram_06FE                        ; A9BE  8d fe 06
    LDA $8793                           ; A9C1  ad 93 87
    STA zp_8E                           ; A9C4  85 8e
    LDA $8794                           ; A9C6  ad 94 87
    STA zp_8F                           ; A9C9  85 8f
    LDA #$42                            ; A9CB  a9 42
    STA zp_8C                           ; A9CD  85 8c
    LDA #$28                            ; A9CF  a9 28
    STA zp_8D                           ; A9D1  85 8d
    JSR NtRectBlit                      ; A9D3  20 f0 81
    LDA #$00                            ; A9D6  a9 00
    STA ram_06FE                        ; A9D8  8d fe 06
    LDA $8C37                           ; A9DB  ad 37 8c
    STA zp_8E                           ; A9DE  85 8e
    LDA $8C38                           ; A9E0  ad 38 8c
    STA zp_8F                           ; A9E3  85 8f
    LDA #$C0                            ; A9E5  a9 c0
    STA zp_8C                           ; A9E7  85 8c
    LDA #$2B                            ; A9E9  a9 2b
    STA zp_8D                           ; A9EB  85 8d
    JSR NtRectBlit                      ; A9ED  20 f0 81
    LDA $B25A                           ; A9F0  ad 5a b2
    STA zp_8E                           ; A9F3  85 8e
    LDA $B25B                           ; A9F5  ad 5b b2
    STA zp_8F                           ; A9F8  85 8f
    LDA #$40                            ; A9FA  a9 40
    STA zp_8C                           ; A9FC  85 8c
    LDA #$2A                            ; A9FE  a9 2a
    STA zp_8D                           ; AA00  85 8d
    JSR NtRectBlit                      ; AA02  20 f0 81
    LDA #$02                            ; AA05  a9 02
    JSR MenuPalLoad                     ; AA07  20 be 82
    JSR MenuPalFadeIn                   ; AA0A  20 fd 82
    LDA #$F0                            ; AA0D  a9 f0
    STA ram_06FE                        ; AA0F  8d fe 06
    INC title_step                      ; AA12  ee f1 06
    RTS                                 ; AA15  60
OpeningPage2Flip:  ; s69: opening steps 12+13: cam_y_hi = ($06FF>>4)&1 -> NT $2000/$2800 shown alternately every 16 frames
    LDA ram_06FF                        ; AA16  ad ff 06
    ROR A                               ; AA19  6a
    ROR A                               ; AA1A  6a
    ROR A                               ; AA1B  6a
    ROR A                               ; AA1C  6a
    AND #$01                            ; AA1D  29 01
    STA cam_y_hi                        ; AA1F  85 05
    DEC ram_06FE                        ; AA21  ce fe 06
    BEQ loc_19_AA27                     ; AA24  f0 01
    RTS                                 ; AA26  60
loc_19_AA27:
    LDA #$3C                            ; AA27  a9 3c
    STA ram_06FE                        ; AA29  8d fe 06
    INC title_step                      ; AA2C  ee f1 06
    RTS                                 ; AA2F  60
OpeningPage3:  ; s69: opening step 16: page 19:AA51 (BG set 3) + fade-in
    JSR MenuPalBlack                    ; AA30  20 b3 82
    LDA #$11                            ; AA33  a9 11
    STA chr_r3_play                     ; AA35  8d f3 07
    LDA #$01                            ; AA38  a9 01
    STA zp_72                           ; AA3A  85 72
    LDA #$00                            ; AA3C  a9 00
    STA zp_73                           ; AA3E  85 73
    LDA #$28                            ; AA40  a9 28
    STA $0610                           ; AA42  8d 10 06
    STA zp_88                           ; AA45  85 88
    LDA #$20                            ; AA47  a9 20
    STA $0608                           ; AA49  8d 08 06
    STA $89                             ; AA4C  85 89
    JSR SceneMetaDraw                   ; AA4E  20 e7 80
    LDA #$20                            ; AA51  a9 20
    LDX #$10                            ; AA53  a2 10
    LDY #$FF                            ; AA55  a0 ff
    JSR NtFill                          ; AA57  20 a1 81
    LDA #$00                            ; AA5A  a9 00
    STA cam_y_hi                        ; AA5C  85 05
    LDA #$80                            ; AA5E  a9 80
    STA ram_06FE                        ; AA60  8d fe 06
    LDA $891F                           ; AA63  ad 1f 89
    STA zp_8E                           ; AA66  85 8e
    LDA $8920                           ; AA68  ad 20 89
    STA zp_8F                           ; AA6B  85 8f
    LDA #$42                            ; AA6D  a9 42
    STA zp_8C                           ; AA6F  85 8c
    LDA #$20                            ; AA71  a9 20
    STA zp_8D                           ; AA73  85 8d
    JSR NtRectBlit                      ; AA75  20 f0 81
    LDA #$00                            ; AA78  a9 00
    STA ram_06FE                        ; AA7A  8d fe 06
    LDA $8C37                           ; AA7D  ad 37 8c
    STA zp_8E                           ; AA80  85 8e
    LDA $8C38                           ; AA82  ad 38 8c
    STA zp_8F                           ; AA85  85 8f
    LDA #$C0                            ; AA87  a9 c0
    STA zp_8C                           ; AA89  85 8c
    LDA #$23                            ; AA8B  a9 23
    STA zp_8D                           ; AA8D  85 8d
    JSR NtRectBlit                      ; AA8F  20 f0 81
    LDA $B337                           ; AA92  ad 37 b3
    STA zp_8E                           ; AA95  85 8e
    LDA $B338                           ; AA97  ad 38 b3
    STA zp_8F                           ; AA9A  85 8f
    LDA #$40                            ; AA9C  a9 40
    STA zp_8C                           ; AA9E  85 8c
    LDA #$22                            ; AAA0  a9 22
    STA zp_8D                           ; AAA2  85 8d
    JSR NtRectBlit                      ; AAA4  20 f0 81
    LDA #$03                            ; AAA7  a9 03
    JSR MenuPalLoad                     ; AAA9  20 be 82
    LDA #$88                            ; AAAC  a9 88
    JSR MenuPalLoad                     ; AAAE  20 be 82
    JSR MenuPalFadeIn                   ; AAB1  20 fd 82
    LDA #$02                            ; AAB4  a9 02
    STA ram_0628                        ; AAB6  8d 28 06
    LDA #$F0                            ; AAB9  a9 f0
    STA ram_06FE                        ; AABB  8d fe 06
    INC title_step                      ; AABE  ee f1 06
    RTS                                 ; AAC1  60
OpeningPage3Anim:  ; s69: opening step 17: SceneMetaDraw id 1 at x $0610 (+1 every 4 frames) y $0608 = the tower sprite; $06FE frames
    LDA ram_06FF                        ; AAC2  ad ff 06
    AND #$03                            ; AAC5  29 03
    BNE loc_19_AACC                     ; AAC7  d0 03
    INC $0610                           ; AAC9  ee 10 06
loc_19_AACC:
    LDA #$01                            ; AACC  a9 01
    STA zp_72                           ; AACE  85 72
    LDA #$00                            ; AAD0  a9 00
    STA zp_73                           ; AAD2  85 73
    LDA $0610                           ; AAD4  ad 10 06
    STA zp_88                           ; AAD7  85 88
    LDA $0608                           ; AAD9  ad 08 06
    STA $89                             ; AADC  85 89
    JSR SceneMetaDraw                   ; AADE  20 e7 80
    DEC ram_06FE                        ; AAE1  ce fe 06
    BEQ loc_19_AAE7                     ; AAE4  f0 01
    RTS                                 ; AAE6  60
loc_19_AAE7:
    DEC ram_0628                        ; AAE7  ce 28 06
    BEQ loc_19_AAED                     ; AAEA  f0 01
    RTS                                 ; AAEC  60
loc_19_AAED:
    INC title_step                      ; AAED  ee f1 06
    RTS                                 ; AAF0  60
OpeningPage4:  ; s69: opening step 20: page 19:AB1E (BG set 4) + fade-in
    JSR MenuPalBlack                    ; AAF1  20 b3 82
    LDA #$7D                            ; AAF4  a9 7d
    STA chr_r2_play                     ; AAF6  8d f2 07
    STA chr_r3_play                     ; AAF9  8d f3 07
    STA chr_r4_play                     ; AAFC  8d f4 07
    STA chr_r5_play                     ; AAFF  8d f5 07
    JSR OamHideAll18                    ; AB02  20 d8 81
    LDA #$02                            ; AB05  a9 02
    STA zp_72                           ; AB07  85 72
    LDA #$00                            ; AB09  a9 00
    STA zp_73                           ; AB0B  85 73
    LDA #$70                            ; AB0D  a9 70
    STA $0610                           ; AB0F  8d 10 06
    STA zp_88                           ; AB12  85 88
    LDA #$38                            ; AB14  a9 38
    STA $0608                           ; AB16  8d 08 06
    STA $89                             ; AB19  85 89
    JSR SceneMetaDraw                   ; AB1B  20 e7 80
    LDA #$20                            ; AB1E  a9 20
    LDX #$10                            ; AB20  a2 10
    LDY #$FF                            ; AB22  a0 ff
    JSR NtFill                          ; AB24  20 a1 81
    LDA #$80                            ; AB27  a9 80
    STA ram_06FE                        ; AB29  8d fe 06
    LDA $8AAB                           ; AB2C  ad ab 8a
    STA zp_8E                           ; AB2F  85 8e
    LDA $8AAC                           ; AB31  ad ac 8a
    STA zp_8F                           ; AB34  85 8f
    LDA #$42                            ; AB36  a9 42
    STA zp_8C                           ; AB38  85 8c
    LDA #$20                            ; AB3A  a9 20
    STA zp_8D                           ; AB3C  85 8d
    JSR NtRectBlit                      ; AB3E  20 f0 81
    LDA #$00                            ; AB41  a9 00
    STA ram_06FE                        ; AB43  8d fe 06
    LDA $8C5B                           ; AB46  ad 5b 8c
    STA zp_8E                           ; AB49  85 8e
    LDA $8C5C                           ; AB4B  ad 5c 8c
    STA zp_8F                           ; AB4E  85 8f
    LDA #$C0                            ; AB50  a9 c0
    STA zp_8C                           ; AB52  85 8c
    LDA #$23                            ; AB54  a9 23
    STA zp_8D                           ; AB56  85 8d
    JSR NtRectBlit                      ; AB58  20 f0 81
    LDA $B40D                           ; AB5B  ad 0d b4
    STA zp_8E                           ; AB5E  85 8e
    LDA $B40E                           ; AB60  ad 0e b4
    STA zp_8F                           ; AB63  85 8f
    LDA #$40                            ; AB65  a9 40
    STA zp_8C                           ; AB67  85 8c
    LDA #$22                            ; AB69  a9 22
    STA zp_8D                           ; AB6B  85 8d
    JSR NtRectBlit                      ; AB6D  20 f0 81
    LDA #$04                            ; AB70  a9 04
    JSR MenuPalLoad                     ; AB72  20 be 82
    JSR MenuPalFadeIn                   ; AB75  20 fd 82
    LDA #$28                            ; AB78  a9 28
    STA ram_06FE                        ; AB7A  8d fe 06
    INC title_step                      ; AB7D  ee f1 06
    RTS                                 ; AB80  60
OpeningPage4Anim:  ; s69: opening step 21: sprite step every 4 frames until $06FE runs out
    LDA ram_06FF                        ; AB81  ad ff 06
    AND #$03                            ; AB84  29 03
    BEQ loc_19_AB89                     ; AB86  f0 01
    RTS                                 ; AB88  60
loc_19_AB89:
    DEC $0608                           ; AB89  ce 08 06
    LDA #$02                            ; AB8C  a9 02
    STA zp_72                           ; AB8E  85 72
    LDA #$00                            ; AB90  a9 00
    STA zp_73                           ; AB92  85 73
    LDA $0610                           ; AB94  ad 10 06
    STA zp_88                           ; AB97  85 88
    LDA $0608                           ; AB99  ad 08 06
    STA $89                             ; AB9C  85 89
    JSR SceneMetaDraw                   ; AB9E  20 e7 80
    DEC ram_06FE                        ; ABA1  ce fe 06
    BEQ loc_19_ABA7                     ; ABA4  f0 01
    RTS                                 ; ABA6  60
loc_19_ABA7:
    INC title_step                      ; ABA7  ee f1 06
    RTS                                 ; ABAA  60
TitleOpeningEnd:  ; s67: TitleFadeOutStart; title_mode = 5
    JSR TitleFadeOutStart               ; ABAB  20 44 83
    LDA #$05                            ; ABAE  a9 05
    STA title_mode                      ; ABB0  8d f0 06
    RTS                                 ; ABB3  60
TitleStoryInit:  ; s67: title_mode 1: screen reset
    JSR MenuPalBlack                    ; ABB4  20 b3 82
    JSR WaitFrameReady                  ; ABB7  20 98 81
    LDA #$03                            ; ABBA  a9 03
    STA irq_mode                        ; ABBC  85 51
    LDA #$02                            ; ABBE  a9 02
    STA title_mode                      ; ABC0  8d f0 06
    LDA #$00                            ; ABC3  a9 00
    STA title_step                      ; ABC5  8d f1 06
    STA ram_06F2                        ; ABC8  8d f2 06
    STA state3_mode                     ; ABCB  8d f3 06
    STA frame_ready                     ; ABCE  85 09
    STA scroll_shadow0                  ; ABD0  85 02
    STA cam_y_lo                        ; ABD2  85 04
    STA cam_x_hi                        ; ABD4  85 03
    STA cam_y_hi                        ; ABD6  85 05
    LDA #$7E                            ; ABD8  a9 7e
    STA chr_r2_play                     ; ABDA  8d f2 07
    STA chr_r3_play                     ; ABDD  8d f3 07
    STA chr_r4_play                     ; ABE0  8d f4 07
    STA chr_r5_play                     ; ABE3  8d f5 07
    JSR OamHideAll18                    ; ABE6  20 d8 81
    RTS                                 ; ABE9  60
TitleIntroStory:  ; s38: title_mode 2 (intro story pages); START/SELECT ($4B&$30) -> 19:B15C skip -> title_mode 3
    LDA pad_new                         ; ABEA  a5 4b
    AND #$30                            ; ABEC  29 30
    BEQ loc_19_ABF3                     ; ABEE  f0 03
    JMP TitleStoryEnd                   ; ABF0  4c 5c b1
loc_19_ABF3:
    LDA title_step                      ; ABF3  ad f1 06
    ASL A                               ; ABF6  0a
    TAY                                 ; ABF7  a8
    LDA JmpTable19_AC07,Y               ; ABF8  b9 07 ac
    STA ram_06FC                        ; ABFB  8d fc 06
    LDA $AC08,Y                         ; ABFE  b9 08 ac
    STA ram_06FD                        ; AC01  8d fd 06
    JMP (ram_06FC)                      ; AC04  6c fc 06
JmpTable19_AC07:  ; s67: title_mode 2 story steps [title_step]: exactly 28 entries (ends at code AC3F); 9FC2/A064 fillers; last = B15C TitleStoryEnd (JUMP_TABLES)
    .dw sub_19_AC3F                     ; 19:AC07
    .dw sub_19_ACE9                     ; 19:AC09
    .dw sub_19_ACF9                     ; 19:AC0B
    .dw sub_19_AD27                     ; 19:AC0D
    .dw sub_19_AD5E                     ; 19:AC0F
    .dw sub_19_ADFF                     ; 19:AC11
    .dw sub_19_AE06                     ; 19:AC13
    .dw Sub18_9FC2                      ; 19:AC15
    .dw TitleStepFadeOut                ; 19:AC17
    .dw Sub18_9FC2                      ; 19:AC19
    .dw sub_19_AE3D                     ; 19:AC1B
    .dw sub_19_AEEA                     ; 19:AC1D
    .dw sub_19_AF14                     ; 19:AC1F
    .dw sub_19_AFCB                     ; 19:AC21
    .dw sub_19_AFD2                     ; 19:AC23
    .dw Sub18_9FC2                      ; 19:AC25
    .dw TitleStepFadeOut                ; 19:AC27
    .dw Sub18_9FC2                      ; 19:AC29
    .dw sub_19_B009                     ; 19:AC2B
    .dw sub_19_B092                     ; 19:AC2D
    .dw TitleStepFadeOut                ; 19:AC2F
    .dw Sub18_9FC2                      ; 19:AC31
    .dw sub_19_B0FB                     ; 19:AC33
    .dw Sub18_9FC2                      ; 19:AC35
    .dw Sub18_9FC2                      ; 19:AC37
    .dw TitleStepFadeOut                ; 19:AC39
    .dw Sub18_9FC2                      ; 19:AC3B
    .dw TitleStoryEnd                   ; 19:AC3D
sub_19_AC3F:
    LDA #$3C                            ; AC3F  a9 3c
    STA chr_r0_play                     ; AC41  8d f0 07
    LDA #$12                            ; AC44  a9 12
    STA chr_r1_play                     ; AC46  8d f1 07
    LDA #$7C                            ; AC49  a9 7c
    STA chr_r2_play                     ; AC4B  8d f2 07
    STA chr_r4_play                     ; AC4E  8d f4 07
    LDA #$7D                            ; AC51  a9 7d
    STA chr_r3_play                     ; AC53  8d f3 07
    STA chr_r5_play                     ; AC56  8d f5 07
    LDA #$01                            ; AC59  a9 01
    STA cam_y_hi                        ; AC5B  85 05
    LDA #$E8                            ; AC5D  a9 e8
    STA cam_y_lo                        ; AC5F  85 04
    LDA #$20                            ; AC61  a9 20
    LDX #$80                            ; AC63  a2 80
    LDY #$00                            ; AC65  a0 00
    JSR NtFill                          ; AC67  20 a1 81
    LDA #$28                            ; AC6A  a9 28
    LDX #$80                            ; AC6C  a2 80
    LDY #$00                            ; AC6E  a0 00
    JSR NtFill                          ; AC70  20 a1 81
    LDA #$80                            ; AC73  a9 80
    STA ram_06FE                        ; AC75  8d fe 06
    LDA $8C7F                           ; AC78  ad 7f 8c
    STA zp_8E                           ; AC7B  85 8e
    LDA $8C80                           ; AC7D  ad 80 8c
    STA zp_8F                           ; AC80  85 8f
    LDA #$14                            ; AC82  a9 14
    STA zp_8C                           ; AC84  85 8c
    LDA #$28                            ; AC86  a9 28
    STA zp_8D                           ; AC88  85 8d
    JSR NtRectBlit                      ; AC8A  20 f0 81
    LDA #$00                            ; AC8D  a9 00
    STA ram_06FE                        ; AC8F  8d fe 06
    LDA $8EAF                           ; AC92  ad af 8e
    STA zp_8E                           ; AC95  85 8e
    LDA $8EB0                           ; AC97  ad b0 8e
    STA zp_8F                           ; AC9A  85 8f
    LDA #$C0                            ; AC9C  a9 c0
    STA zp_8C                           ; AC9E  85 8c
    LDA #$2B                            ; ACA0  a9 2b
    STA zp_8D                           ; ACA2  85 8d
    JSR NtRectBlit                      ; ACA4  20 f0 81
    LDA #$05                            ; ACA7  a9 05
    JSR MenuPalLoad                     ; ACA9  20 be 82
    LDA #$89                            ; ACAC  a9 89
    JSR MenuPalLoad                     ; ACAE  20 be 82
    JSR MenuPalApply                    ; ACB1  20 f1 82
    INC title_step                      ; ACB4  ee f1 06
    LDX #$05                            ; ACB7  a2 05
loc_19_ACB9:
    LDA $ACDA,X                         ; ACB9  bd da ac
    STA ram_0618,X                      ; ACBC  9d 18 06
    LDA $ACDF,X                         ; ACBF  bd df ac
    STA $0610,X                         ; ACC2  9d 10 06
    LDA $ACE4,X                         ; ACC5  bd e4 ac
    STA $0608,X                         ; ACC8  9d 08 06
    LDA #$00                            ; ACCB  a9 00
    STA obj_phase,X                     ; ACCD  9d 00 06
    STA obj_timer2,X                    ; ACD0  9d 20 06
    STA ram_0628,X                      ; ACD3  9d 28 06
    DEX                                 ; ACD6  ca
    BPL loc_19_ACB9                     ; ACD7  10 e0
    RTS                                 ; ACD9  60
    .db $05,$06,$03,$03,$04,$A8,$D0,$F0,$F0,$F0,$F0,$F0,$30,$50,$70; 19:ACDA
sub_19_ACE9:
    LDA cam_y_lo                        ; ACE9  a5 04
    SEC                                 ; ACEB  38
    SBC #$08                            ; ACEC  e9 08
    STA cam_y_lo                        ; ACEE  85 04
    CMP #$68                            ; ACF0  c9 68
    BEQ loc_19_ACF5                     ; ACF2  f0 01
    RTS                                 ; ACF4  60
loc_19_ACF5:
    INC title_step                      ; ACF5  ee f1 06
    RTS                                 ; ACF8  60
sub_19_ACF9:
    LDA ram_0618                        ; ACF9  ad 18 06
    STA zp_72                           ; ACFC  85 72
    LDA obj_timer2                      ; ACFE  ad 20 06
    STA zp_73                           ; AD01  85 73
    LDA $0610                           ; AD03  ad 10 06
    STA zp_88                           ; AD06  85 88
    LDA $0608                           ; AD08  ad 08 06
    STA $89                             ; AD0B  85 89
    JSR SceneMetaDraw                   ; AD0D  20 e7 80
    LDA $0608                           ; AD10  ad 08 06
    CLC                                 ; AD13  18
    ADC #$08                            ; AD14  69 08
    STA $0608                           ; AD16  8d 08 06
    LDA cam_y_lo                        ; AD19  a5 04
    SEC                                 ; AD1B  38
    SBC #$08                            ; AD1C  e9 08
    STA cam_y_lo                        ; AD1E  85 04
    BEQ loc_19_AD23                     ; AD20  f0 01
    RTS                                 ; AD22  60
loc_19_AD23:
    INC title_step                      ; AD23  ee f1 06
    RTS                                 ; AD26  60
sub_19_AD27:
    LDA #$98                            ; AD27  a9 98
    STA $06C6                           ; AD29  8d c6 06
    LDA #$F0                            ; AD2C  a9 f0
    STA $06C4                           ; AD2E  8d c4 06
    LDA #$00                            ; AD31  a9 00
    STA ram_06FE                        ; AD33  8d fe 06
    LDA $0608                           ; AD36  ad 08 06
    SEC                                 ; AD39  38
    SBC #$08                            ; AD3A  e9 08
    STA $0608                           ; AD3C  8d 08 06
    LDA #$68                            ; AD3F  a9 68
    STA $0609                           ; AD41  8d 09 06
    LDA ram_0618                        ; AD44  ad 18 06
    STA zp_72                           ; AD47  85 72
    LDA #$00                            ; AD49  a9 00
    STA zp_73                           ; AD4B  85 73
    LDA $0610                           ; AD4D  ad 10 06
    STA zp_88                           ; AD50  85 88
    LDA $0608                           ; AD52  ad 08 06
    STA $89                             ; AD55  85 89
    JSR SceneMetaDraw                   ; AD57  20 e7 80
    INC title_step                      ; AD5A  ee f1 06
    RTS                                 ; AD5D  60
sub_19_AD5E:
    JSR OamHideAll18                    ; AD5E  20 d8 81
    LDA #$04                            ; AD61  a9 04
    STA ram_06C5                        ; AD63  8d c5 06
    LDA #$00                            ; AD66  a9 00
    STA pending_msg_id                  ; AD68  8d c3 06
loc_19_AD6B:
    LDX pending_msg_id                  ; AD6B  ae c3 06
    LDA ram_0618,X                      ; AD6E  bd 18 06
    STA zp_72                           ; AD71  85 72
    LDA obj_timer2,X                    ; AD73  bd 20 06
    STA zp_73                           ; AD76  85 73
    LDA $0610,X                         ; AD78  bd 10 06
    STA zp_88                           ; AD7B  85 88
    LDA $0608,X                         ; AD7D  bd 08 06
    STA $89                             ; AD80  85 89
    JSR sub_18_80DF                     ; AD82  20 df 80
    LDX pending_msg_id                  ; AD85  ae c3 06
    LDA obj_phase,X                     ; AD88  bd 00 06
    BNE loc_19_AD99                     ; AD8B  d0 0c
    LDA $06C2                           ; AD8D  ad c2 06
    STA ram_0628,X                      ; AD90  9d 28 06
    INC obj_phase,X                     ; AD93  fe 00 06
    JMP loc_19_ADB4                     ; AD96  4c b4 ad
loc_19_AD99:
    DEC ram_0628,X                      ; AD99  de 28 06
    BPL loc_19_ADB4                     ; AD9C  10 16
    LDA #$00                            ; AD9E  a9 00
    STA obj_phase,X                     ; ADA0  9d 00 06
    INC obj_timer2,X                    ; ADA3  fe 20 06
    LDA obj_timer2,X                    ; ADA6  bd 20 06
    CMP orbit_level                     ; ADA9  cd c0 06
    BNE loc_19_ADB4                     ; ADAC  d0 06
    LDA orbit_burst_timer               ; ADAE  ad c1 06
    STA obj_timer2,X                    ; ADB1  9d 20 06
loc_19_ADB4:
    LDA ram_06FF                        ; ADB4  ad ff 06
    AND #$01                            ; ADB7  29 01
    BEQ loc_19_ADE1                     ; ADB9  f0 26
    LDA $0610,X                         ; ADBB  bd 10 06
    SEC                                 ; ADBE  38
    SBC $ADF5,X                         ; ADBF  fd f5 ad
    STA $0610,X                         ; ADC2  9d 10 06
    PHA                                 ; ADC5  48
    LDA $0608,X                         ; ADC6  bd 08 06
    SEC                                 ; ADC9  38
    SBC $ADFA,X                         ; ADCA  fd fa ad
    STA $0608,X                         ; ADCD  9d 08 06
    PLA                                 ; ADD0  68
    CMP #$70                            ; ADD1  c9 70
    BCS loc_19_ADE1                     ; ADD3  b0 0c
    LDA $ACDF,X                         ; ADD5  bd df ac
    STA $0610,X                         ; ADD8  9d 10 06
    LDA $ACE4,X                         ; ADDB  bd e4 ac
    STA $0608,X                         ; ADDE  9d 08 06
loc_19_ADE1:
    INC pending_msg_id                  ; ADE1  ee c3 06
    LDA pending_msg_id                  ; ADE4  ad c3 06
    CMP #$05                            ; ADE7  c9 05
    BNE loc_19_AD6B                     ; ADE9  d0 80
    DEC ram_06FE                        ; ADEB  ce fe 06
    BEQ loc_19_ADF1                     ; ADEE  f0 01
    RTS                                 ; ADF0  60
loc_19_ADF1:
    INC title_step                      ; ADF1  ee f1 06
    RTS                                 ; ADF4  60
    .db $00,$00,$04,$02,$01,$00,$00,$01,$01,$01; 19:ADF5
sub_19_ADFF:
    JSR TitleFadeOutStart               ; ADFF  20 44 83
    INC title_step                      ; AE02  ee f1 06
    RTS                                 ; AE05  60
sub_19_AE06:
    JSR OamHideAll18                    ; AE06  20 d8 81
    LDA #$28                            ; AE09  a9 28
    LDX #$10                            ; AE0B  a2 10
    LDY #$FF                            ; AE0D  a0 ff
    JSR NtFill                          ; AE0F  20 a1 81
    LDA #$00                            ; AE12  a9 00
    STA ram_06FE                        ; AE14  8d fe 06
    LDA $B42F                           ; AE17  ad 2f b4
    STA zp_8E                           ; AE1A  85 8e
    LDA $B430                           ; AE1C  ad 30 b4
    STA zp_8F                           ; AE1F  85 8f
    LDA #$A0                            ; AE21  a9 a0
    STA zp_8C                           ; AE23  85 8c
    LDA #$29                            ; AE25  a9 29
    STA zp_8D                           ; AE27  85 8d
    JSR NtRectBlit                      ; AE29  20 f0 81
    LDA #$00                            ; AE2C  a9 00
    JSR MenuPalLoad                     ; AE2E  20 be 82
    JSR MenuPalFadeIn                   ; AE31  20 fd 82
    LDA #$00                            ; AE34  a9 00
    STA ram_06FE                        ; AE36  8d fe 06
    INC title_step                      ; AE39  ee f1 06
    RTS                                 ; AE3C  60
sub_19_AE3D:
    LDA #$20                            ; AE3D  a9 20
    LDX #$80                            ; AE3F  a2 80
    LDY #$00                            ; AE41  a0 00
    JSR NtFill                          ; AE43  20 a1 81
    LDA #$28                            ; AE46  a9 28
    LDX #$80                            ; AE48  a2 80
    LDY #$00                            ; AE4A  a0 00
    JSR NtFill                          ; AE4C  20 a1 81
    LDA #$00                            ; AE4F  a9 00
    STA cam_y_hi                        ; AE51  85 05
    STA cam_y_lo                        ; AE53  85 04
    LDA #$80                            ; AE55  a9 80
    STA ram_06FE                        ; AE57  8d fe 06
    LDA $8D73                           ; AE5A  ad 73 8d
    STA zp_8E                           ; AE5D  85 8e
    LDA $8D74                           ; AE5F  ad 74 8d
    STA zp_8F                           ; AE62  85 8f
    LDA #$C2                            ; AE64  a9 c2
    STA zp_8C                           ; AE66  85 8c
    LDA #$28                            ; AE68  a9 28
    STA zp_8D                           ; AE6A  85 8d
    JSR NtRectBlit                      ; AE6C  20 f0 81
    LDA #$00                            ; AE6F  a9 00
    STA ram_06FE                        ; AE71  8d fe 06
    LDA $8EAF                           ; AE74  ad af 8e
    STA zp_8E                           ; AE77  85 8e
    LDA $8EB0                           ; AE79  ad b0 8e
    STA zp_8F                           ; AE7C  85 8f
    LDA #$C0                            ; AE7E  a9 c0
    STA zp_8C                           ; AE80  85 8c
    LDA #$2B                            ; AE82  a9 2b
    STA zp_8D                           ; AE84  85 8d
    JSR NtRectBlit                      ; AE86  20 f0 81
    LDA #$07                            ; AE89  a9 07
    JSR MenuPalLoad                     ; AE8B  20 be 82
    LDA #$89                            ; AE8E  a9 89
    JSR MenuPalLoad                     ; AE90  20 be 82
    JSR MenuPalApply                    ; AE93  20 f1 82
    INC title_step                      ; AE96  ee f1 06
    LDX #$07                            ; AE99  a2 07
loc_19_AE9B:
    LDA $AEC2,X                         ; AE9B  bd c2 ae
    STA ram_0618,X                      ; AE9E  9d 18 06
    LDA $AECA,X                         ; AEA1  bd ca ae
    STA $0610,X                         ; AEA4  9d 10 06
    LDA $AED2,X                         ; AEA7  bd d2 ae
    STA $0608,X                         ; AEAA  9d 08 06
    LDA $AEDA,X                         ; AEAD  bd da ae
    STA obj_timer2,X                    ; AEB0  9d 20 06
    LDA $AEE2,X                         ; AEB3  bd e2 ae
    STA ram_0628,X                      ; AEB6  9d 28 06
    LDA #$00                            ; AEB9  a9 00
    STA obj_phase,X                     ; AEBB  9d 00 06
    DEX                                 ; AEBE  ca
    BPL loc_19_AE9B                     ; AEBF  10 da
    RTS                                 ; AEC1  60
    .db $04,$04,$04,$04,$04,$04,$04,$04,$60,$60,$63,$63,$70,$70,$73,$73; 19:AEC2
    .db $48,$50,$58,$60,$44,$4C,$54,$64,$01,$02,$01,$00,$02,$00,$01,$02; 19:AED2
    .db $03,$01,$00,$04,$02,$03,$01,$00 ; 19:AEE2
sub_19_AEEA:
    LDA cam_y_lo                        ; AEEA  a5 04
    CLC                                 ; AEEC  18
    ADC #$08                            ; AEED  69 08
    STA cam_y_lo                        ; AEEF  85 04
    CMP #$F0                            ; AEF1  c9 f0
    BEQ loc_19_AEF6                     ; AEF3  f0 01
    RTS                                 ; AEF5  60
loc_19_AEF6:
    LDA #$00                            ; AEF6  a9 00
    STA cam_y_lo                        ; AEF8  85 04
    INC cam_y_hi                        ; AEFA  e6 05
    LDA #$08                            ; AEFC  a9 08
    STA $06C6                           ; AEFE  8d c6 06
    LDA #$60                            ; AF01  a9 60
    STA $06C4                           ; AF03  8d c4 06
    LDA #$80                            ; AF06  a9 80
    STA $06CC                           ; AF08  8d cc 06
    LDA #$02                            ; AF0B  a9 02
    STA ram_06CD                        ; AF0D  8d cd 06
    INC title_step                      ; AF10  ee f1 06
    RTS                                 ; AF13  60
sub_19_AF14:
    JSR OamHideAll18                    ; AF14  20 d8 81
    LDA #$04                            ; AF17  a9 04
    STA ram_06C5                        ; AF19  8d c5 06
    LDA #$00                            ; AF1C  a9 00
    STA pending_msg_id                  ; AF1E  8d c3 06
loc_19_AF21:
    LDX pending_msg_id                  ; AF21  ae c3 06
    LDA ram_0618,X                      ; AF24  bd 18 06
    STA zp_72                           ; AF27  85 72
    LDA obj_timer2,X                    ; AF29  bd 20 06
    STA zp_73                           ; AF2C  85 73
    LDA $0610,X                         ; AF2E  bd 10 06
    STA zp_88                           ; AF31  85 88
    LDA $0608,X                         ; AF33  bd 08 06
    STA $89                             ; AF36  85 89
    JSR sub_18_80DF                     ; AF38  20 df 80
    LDX pending_msg_id                  ; AF3B  ae c3 06
    LDA obj_phase,X                     ; AF3E  bd 00 06
    BNE loc_19_AF4F                     ; AF41  d0 0c
    LDA $06C2                           ; AF43  ad c2 06
    STA ram_0628,X                      ; AF46  9d 28 06
    INC obj_phase,X                     ; AF49  fe 00 06
    JMP loc_19_AF6A                     ; AF4C  4c 6a af
loc_19_AF4F:
    DEC ram_0628,X                      ; AF4F  de 28 06
    BPL loc_19_AF6A                     ; AF52  10 16
    LDA #$00                            ; AF54  a9 00
    STA obj_phase,X                     ; AF56  9d 00 06
    INC obj_timer2,X                    ; AF59  fe 20 06
    LDA obj_timer2,X                    ; AF5C  bd 20 06
    CMP orbit_level                     ; AF5F  cd c0 06
    BNE loc_19_AF6A                     ; AF62  d0 06
    LDA orbit_burst_timer               ; AF64  ad c1 06
    STA obj_timer2,X                    ; AF67  9d 20 06
loc_19_AF6A:
    LDA ram_06FF                        ; AF6A  ad ff 06
    AND #$01                            ; AF6D  29 01
    BEQ loc_19_AF9E                     ; AF6F  f0 2d
    LDA $0610,X                         ; AF71  bd 10 06
    SEC                                 ; AF74  38
    SBC $AFBB,X                         ; AF75  fd bb af
    STA $0610,X                         ; AF78  9d 10 06
    PHA                                 ; AF7B  48
    LDA $0608,X                         ; AF7C  bd 08 06
    SEC                                 ; AF7F  38
    SBC $AFC3,X                         ; AF80  fd c3 af
    STA $0608,X                         ; AF83  9d 08 06
    PLA                                 ; AF86  68
    CMP #$08                            ; AF87  c9 08
    BCC loc_19_AF92                     ; AF89  90 07
    LDA $0608,X                         ; AF8B  bd 08 06
    CMP #$20                            ; AF8E  c9 20
    BCS loc_19_AF9E                     ; AF90  b0 0c
loc_19_AF92:
    LDA $AECA,X                         ; AF92  bd ca ae
    STA $0610,X                         ; AF95  9d 10 06
    LDA $AED2,X                         ; AF98  bd d2 ae
    STA $0608,X                         ; AF9B  9d 08 06
loc_19_AF9E:
    INC pending_msg_id                  ; AF9E  ee c3 06
    LDA pending_msg_id                  ; AFA1  ad c3 06
    CMP #$08                            ; AFA4  c9 08
    BEQ loc_19_AFAB                     ; AFA6  f0 03
    JMP loc_19_AF21                     ; AFA8  4c 21 af
loc_19_AFAB:
    DEC $06CC                           ; AFAB  ce cc 06
    BEQ loc_19_AFB1                     ; AFAE  f0 01
    RTS                                 ; AFB0  60
loc_19_AFB1:
    DEC ram_06CD                        ; AFB1  ce cd 06
    BEQ loc_19_AFB7                     ; AFB4  f0 01
    RTS                                 ; AFB6  60
loc_19_AFB7:
    INC title_step                      ; AFB7  ee f1 06
    RTS                                 ; AFBA  60
    .db $01,$01,$02,$03,$03,$02,$01,$01,$00,$01,$01,$00,$01,$00,$01,$00; 19:AFBB
sub_19_AFCB:
    JSR TitleFadeOutStart               ; AFCB  20 44 83
    INC title_step                      ; AFCE  ee f1 06
    RTS                                 ; AFD1  60
sub_19_AFD2:
    JSR OamHideAll18                    ; AFD2  20 d8 81
    LDA #$28                            ; AFD5  a9 28
    LDX #$10                            ; AFD7  a2 10
    LDY #$FF                            ; AFD9  a0 ff
    JSR NtFill                          ; AFDB  20 a1 81
    LDA #$00                            ; AFDE  a9 00
    STA ram_06FE                        ; AFE0  8d fe 06
    LDA $B505                           ; AFE3  ad 05 b5
    STA zp_8E                           ; AFE6  85 8e
    LDA $B506                           ; AFE8  ad 06 b5
    STA zp_8F                           ; AFEB  85 8f
    LDA #$A0                            ; AFED  a9 a0
    STA zp_8C                           ; AFEF  85 8c
    LDA #$29                            ; AFF1  a9 29
    STA zp_8D                           ; AFF3  85 8d
    JSR NtRectBlit                      ; AFF5  20 f0 81
    LDA #$00                            ; AFF8  a9 00
    JSR MenuPalLoad                     ; AFFA  20 be 82
    JSR MenuPalFadeIn                   ; AFFD  20 fd 82
    LDA #$00                            ; B000  a9 00
    STA ram_06FE                        ; B002  8d fe 06
    INC title_step                      ; B005  ee f1 06
    RTS                                 ; B008  60
sub_19_B009:
    JSR OamHideAll18                    ; B009  20 d8 81
    LDA #$20                            ; B00C  a9 20
    LDX #$10                            ; B00E  a2 10
    LDY #$FF                            ; B010  a0 ff
    JSR NtFill                          ; B012  20 a1 81
    LDA #$16                            ; B015  a9 16
    STA chr_r1_play                     ; B017  8d f1 07
    LDA #$00                            ; B01A  a9 00
    STA ram_06FE                        ; B01C  8d fe 06
    STA cam_y_lo                        ; B01F  85 04
    STA cam_y_hi                        ; B021  85 05
    LDA $8E67                           ; B023  ad 67 8e
    STA zp_8E                           ; B026  85 8e
    LDA $8E68                           ; B028  ad 68 8e
    STA zp_8F                           ; B02B  85 8f
    LDA #$CC                            ; B02D  a9 cc
    STA zp_8C                           ; B02F  85 8c
    LDA #$20                            ; B031  a9 20
    STA zp_8D                           ; B033  85 8d
    JSR NtRectBlit                      ; B035  20 f0 81
    LDA $8EF3                           ; B038  ad f3 8e
    STA zp_8E                           ; B03B  85 8e
    LDA $8EF4                           ; B03D  ad f4 8e
    STA zp_8F                           ; B040  85 8f
    LDA #$C0                            ; B042  a9 c0
    STA zp_8C                           ; B044  85 8c
    LDA #$23                            ; B046  a9 23
    STA zp_8D                           ; B048  85 8d
    JSR NtRectBlit                      ; B04A  20 f0 81
    LDA $B5DB                           ; B04D  ad db b5
    STA zp_8E                           ; B050  85 8e
    LDA $B5DC                           ; B052  ad dc b5
    STA zp_8F                           ; B055  85 8f
    LDA #$C0                            ; B057  a9 c0
    STA zp_8C                           ; B059  85 8c
    LDA #$21                            ; B05B  a9 21
    STA zp_8D                           ; B05D  85 8d
    JSR NtRectBlit                      ; B05F  20 f0 81
    LDA #$06                            ; B062  a9 06
    JSR MenuPalLoad                     ; B064  20 be 82
    LDA #$86                            ; B067  a9 86
    JSR MenuPalLoad                     ; B069  20 be 82
    JSR MenuPalFadeIn                   ; B06C  20 fd 82
    LDA #$02                            ; B06F  a9 02
    STA ram_06C8                        ; B071  8d c8 06
    LDA #$A0                            ; B074  a9 a0
    STA $06C9                           ; B076  8d c9 06
    LDA #$08                            ; B079  a9 08
    STA $06CA                           ; B07B  8d ca 06
    LDA #$07                            ; B07E  a9 07
    STA ram_0618                        ; B080  8d 18 06
    LDA #$00                            ; B083  a9 00
    STA obj_phase                       ; B085  8d 00 06
    STA obj_timer2                      ; B088  8d 20 06
    STA ram_0628                        ; B08B  8d 28 06
    INC title_step                      ; B08E  ee f1 06
    RTS                                 ; B091  60
sub_19_B092:
    JSR OamHideAll18                    ; B092  20 d8 81
    LDA ram_0618                        ; B095  ad 18 06
    STA zp_72                           ; B098  85 72
    LDA obj_timer2                      ; B09A  ad 20 06
    STA zp_73                           ; B09D  85 73
    LDA #$60                            ; B09F  a9 60
    STA zp_88                           ; B0A1  85 88
    LDA #$30                            ; B0A3  a9 30
    STA $89                             ; B0A5  85 89
    JSR SceneMetaDraw                   ; B0A7  20 e7 80
    LDA obj_phase                       ; B0AA  ad 00 06
    BNE loc_19_B0BB                     ; B0AD  d0 0c
    LDA $06CA                           ; B0AF  ad ca 06
    STA ram_0628                        ; B0B2  8d 28 06
    INC obj_phase                       ; B0B5  ee 00 06
    JMP loc_19_B0EB                     ; B0B8  4c eb b0
loc_19_B0BB:
    DEC ram_0628                        ; B0BB  ce 28 06
    BMI loc_19_B0C3                     ; B0BE  30 03
    JMP loc_19_B0EB                     ; B0C0  4c eb b0
loc_19_B0C3:
    LDA #$00                            ; B0C3  a9 00
    STA obj_phase                       ; B0C5  8d 00 06
    INC obj_timer2                      ; B0C8  ee 20 06
    LDA obj_timer2                      ; B0CB  ad 20 06
    CMP orbit_level                     ; B0CE  cd c0 06
    BEQ loc_19_B0D6                     ; B0D1  f0 03
    JMP loc_19_B0EB                     ; B0D3  4c eb b0
loc_19_B0D6:
    LDA #$00                            ; B0D6  a9 00
    STA obj_timer2                      ; B0D8  8d 20 06
    STA ram_0628                        ; B0DB  8d 28 06
    DEC $06CA                           ; B0DE  ce ca 06
    DEC $06CA                           ; B0E1  ce ca 06
    BPL loc_19_B0EB                     ; B0E4  10 05
    LDA #$00                            ; B0E6  a9 00
    STA $06CA                           ; B0E8  8d ca 06
loc_19_B0EB:
    DEC $06C9                           ; B0EB  ce c9 06
    BEQ loc_19_B0F1                     ; B0EE  f0 01
    RTS                                 ; B0F0  60
loc_19_B0F1:
    DEC ram_06C8                        ; B0F1  ce c8 06
    BEQ loc_19_B0F7                     ; B0F4  f0 01
    RTS                                 ; B0F6  60
loc_19_B0F7:
    INC title_step                      ; B0F7  ee f1 06
    RTS                                 ; B0FA  60
sub_19_B0FB:
    JSR OamHideAll18                    ; B0FB  20 d8 81
    LDA #$20                            ; B0FE  a9 20
    LDX #$10                            ; B100  a2 10
    LDY #$FF                            ; B102  a0 ff
    JSR NtFill                          ; B104  20 a1 81
    LDA #$00                            ; B107  a9 00
    STA ram_06FE                        ; B109  8d fe 06
    LDA $8E8B                           ; B10C  ad 8b 8e
    STA zp_8E                           ; B10F  85 8e
    LDA $8E8C                           ; B111  ad 8c 8e
    STA zp_8F                           ; B114  85 8f
    LDA #$CC                            ; B116  a9 cc
    STA zp_8C                           ; B118  85 8c
    LDA #$20                            ; B11A  a9 20
    STA zp_8D                           ; B11C  85 8d
    JSR NtRectBlit                      ; B11E  20 f0 81
    LDA $8F17                           ; B121  ad 17 8f
    STA zp_8E                           ; B124  85 8e
    LDA $8F18                           ; B126  ad 18 8f
    STA zp_8F                           ; B129  85 8f
    LDA #$C0                            ; B12B  a9 c0
    STA zp_8C                           ; B12D  85 8c
    LDA #$23                            ; B12F  a9 23
    STA zp_8D                           ; B131  85 8d
    JSR NtRectBlit                      ; B133  20 f0 81
    LDA $B6ED                           ; B136  ad ed b6
    STA zp_8E                           ; B139  85 8e
    LDA $B6EE                           ; B13B  ad ee b6
    STA zp_8F                           ; B13E  85 8f
    LDA #$00                            ; B140  a9 00
    STA zp_8C                           ; B142  85 8c
    LDA #$22                            ; B144  a9 22
    STA zp_8D                           ; B146  85 8d
    JSR NtRectBlit                      ; B148  20 f0 81
    LDA #$06                            ; B14B  a9 06
    JSR MenuPalLoad                     ; B14D  20 be 82
    JSR MenuPalFadeIn                   ; B150  20 fd 82
    LDA #$B4                            ; B153  a9 b4
    STA ram_06FE                        ; B155  8d fe 06
    INC title_step                      ; B158  ee f1 06
    RTS                                 ; B15B  60
TitleStoryEnd:  ; s67: title_mode = 3 (story end or START/SELECT skip)
    LDA #$03                            ; B15C  a9 03
    STA title_mode                      ; B15E  8d f0 06
    RTS                                 ; B161  60
DateCardRectPtr:  ; s69: ptr word -> 19:B164 NtRectBlit stream W=30 H=3 ('1997 October 1' / 'The END DAY'); ends at 19:B1C0
    .db $64,$B1,$1E,$03,$20,$20,$20,$20,$20,$20,$20,$20,$20,$31,$39,$39; 19:B162
    .db $37,$2C,$20,$4F,$63,$74,$6F,$62,$65,$72,$20,$31,$2C,$20,$20,$20; 19:B172
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B182
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B192
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$54,$68,$65,$20,$45; 19:B1A2
    .db $4E,$44,$20,$44,$41,$59,$20,$20,$20,$20,$20,$20,$20,$20,$C2,$B1; 19:B1B2
    .db $1E,$05,$20,$20,$53,$61,$76,$61,$67,$65,$20,$77,$61,$72,$20,$65; 19:B1C2
    .db $6E,$67,$75,$6C,$66,$73,$20,$74,$68,$65,$20,$77,$6F,$72,$6C,$64; 19:B1D2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B1E2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B1F2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$2E,$2E,$2E,$20; 19:B202
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B212
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B222
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$43,$69,$76; 19:B232
    .db $69,$6C,$69,$7A,$61,$74,$69,$6F,$6E,$20,$69,$73,$20,$64,$65,$73; 19:B242
    .db $74,$72,$6F,$79,$65,$64,$5F,$20,$5C,$B2,$1F,$07,$20,$20,$41,$6E; 19:B252
    .db $20,$65,$76,$6F,$6C,$75,$74,$69,$6F,$6E,$20,$68,$61,$64,$20,$74; 19:B262
    .db $61,$6B,$65,$6E,$20,$70,$6C,$61,$63,$65,$2E,$20,$20,$20,$20,$20; 19:B272
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B282
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$54,$68,$65,$20; 19:B292
    .db $65,$61,$72,$74,$68,$27,$73,$20,$61,$78,$69,$73,$20,$73,$68,$69; 19:B2A2
    .db $66,$74,$65,$64,$20,$61,$6E,$64,$20,$20,$20,$20,$20,$20,$20,$20; 19:B2B2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B2C2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$61,$6C,$6C,$20,$63,$72; 19:B2D2
    .db $65,$61,$74,$75,$72,$65,$73,$20,$62,$65,$63,$61,$6D,$65,$20,$6D; 19:B2E2
    .db $75,$74,$61,$74,$65,$64,$2E,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B2F2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B302
    .db $20,$20,$20,$20,$20,$20,$20,$20,$4C,$69,$66,$65,$20,$77,$6F,$75; 19:B312
    .db $6C,$64,$20,$6E,$65,$76,$65,$72,$20,$62,$65,$20,$74,$68,$65,$20; 19:B322
    .db $73,$61,$6D,$65,$5F,$39,$B3,$1E,$07,$20,$20,$54,$68,$6F,$73,$65; 19:B332
    .db $20,$73,$75,$72,$76,$69,$76,$69,$6E,$67,$20,$76,$6F,$77,$65,$64; 19:B342
    .db $20,$6E,$6F,$74,$20,$74,$6F,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B352
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B362
    .db $20,$20,$20,$20,$20,$20,$20,$72,$65,$70,$65,$61,$74,$20,$74,$68; 19:B372
    .db $65,$69,$72,$20,$6D,$69,$73,$74,$61,$6B,$65,$73,$20,$6F,$66,$20; 19:B382
    .db $74,$68,$65,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B392
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B3A2
    .db $20,$20,$20,$70,$61,$73,$74,$20,$61,$6E,$64,$20,$65,$72,$65,$63; 19:B3B2
    .db $74,$65,$64,$20,$61,$20,$67,$72,$65,$61,$74,$20,$20,$20,$20,$20; 19:B3C2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B3D2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$74; 19:B3E2
    .db $6F,$77,$65,$72,$20,$69,$6E,$20,$74,$68,$65,$20,$73,$6B,$79,$5F; 19:B3F2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$0F,$B4,$1E,$01,$20; 19:B402
    .db $20,$20,$20,$54,$6F,$20,$6F,$70,$70,$72,$65,$73,$73,$20,$65,$76; 19:B412
    .db $69,$6C,$20,$66,$6F,$72,$65,$76,$65,$72,$5F,$20,$20,$31,$B4,$1E; 19:B422
    .db $07,$20,$20,$20,$20,$20,$31,$30,$30,$20,$79,$65,$61,$72,$73,$20; 19:B432
    .db $68,$61,$76,$65,$20,$70,$61,$73,$73,$65,$64,$2E,$20,$20,$20,$20; 19:B442
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B452
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B462
    .db $20,$20,$4D,$6F,$73,$74,$20,$6F,$66,$20,$74,$68,$65,$20,$65,$61; 19:B472
    .db $72,$74,$68,$20,$68,$61,$73,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B482
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B492
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$62,$65; 19:B4A2
    .db $63,$6F,$6D,$65,$20,$64,$6F,$6D,$69,$6E,$61,$74,$65,$64,$20,$62; 19:B4B2
    .db $79,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B4C2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B4D2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$6D,$75,$74,$61,$74,$65; 19:B4E2
    .db $64,$20,$63,$72,$65,$61,$74,$75,$72,$65,$73,$2E,$20,$20,$20,$20; 19:B4F2
    .db $20,$20,$20,$07,$B5,$1E,$07,$20,$20,$20,$50,$65,$6F,$70,$6C,$65; 19:B502
    .db $20,$77,$6F,$72,$6B,$65,$64,$20,$74,$6F,$67,$65,$74,$68,$65,$72; 19:B512
    .db $20,$74,$6F,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B522
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B532
    .db $20,$20,$20,$20,$20,$20,$72,$65,$62,$75,$69,$6C,$64,$20,$74,$68; 19:B542
    .db $65,$69,$72,$20,$76,$69,$6C,$6C,$61,$67,$65,$73,$20,$61,$6E,$64; 19:B552
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B562
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B572
    .db $20,$20,$74,$68,$65,$69,$72,$20,$6C,$69,$76,$65,$73,$2C,$20,$62; 19:B582
    .db $75,$74,$20,$74,$68,$65,$79,$20,$73,$74,$69,$6C,$6C,$20,$20,$20; 19:B592
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B5A2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$6C,$69; 19:B5B2
    .db $76,$65,$64,$20,$69,$6E,$20,$66,$65,$61,$72,$2E,$20,$20,$20,$20; 19:B5C2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$DD,$B5,$1E,$09,$20,$20,$41; 19:B5D2
    .db $73,$20,$74,$68,$65,$79,$20,$66,$65,$61,$72,$65,$64,$20,$74,$68; 19:B5E2
    .db $65,$20,$72,$65,$62,$69,$72,$74,$68,$20,$20,$20,$20,$20,$20,$20; 19:B5F2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B602
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$6F,$66,$20,$65,$76; 19:B612
    .db $69,$6C,$2C,$20,$74,$68,$65,$79,$20,$72,$65,$6D,$65,$6D,$62,$65; 19:B622
    .db $72,$65,$64,$20,$74,$68,$65,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B632
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B642
    .db $20,$20,$20,$20,$20,$20,$20,$63,$6F,$6E,$73,$74,$72,$75,$63,$74; 19:B652
    .db $69,$6F,$6E,$20,$6F,$66,$20,$74,$68,$65,$20,$74,$6F,$77,$65,$72; 19:B662
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B672
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B682
    .db $20,$20,$20,$61,$6E,$64,$20,$6F,$66,$20,$69,$74,$27,$73,$20,$63; 19:B692
    .db $6F,$6E,$73,$65,$71,$75,$65,$6E,$63,$65,$73,$20,$20,$20,$20,$20; 19:B6A2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B6B2
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$6F; 19:B6C2
    .db $6E,$63,$65,$20,$61,$63,$74,$69,$76,$61,$74,$69,$6F,$6E,$20,$68; 19:B6D2
    .db $61,$64,$20,$62,$65,$67,$75,$6E,$2E,$20,$20,$EF,$B6,$1E,$07,$20; 19:B6E2
    .db $20,$20,$4F,$6E,$63,$65,$20,$65,$76,$69,$6C,$20,$65,$6D,$65,$72; 19:B6F2
    .db $67,$65,$64,$2C,$20,$77,$6F,$75,$6C,$64,$20,$20,$20,$20,$20,$20; 19:B702
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B712
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$74,$68; 19:B722
    .db $65,$79,$20,$73,$74,$69,$6C,$6C,$20,$73,$74,$61,$6E,$64,$20,$61; 19:B732
    .db $20,$63,$68,$61,$6E,$63,$65,$3F,$20,$20,$20,$20,$20,$20,$20,$20; 19:B742
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B752
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B762
    .db $20,$20,$20,$20,$20,$2E,$2E,$2E,$20,$20,$20,$20,$20,$20,$20,$20; 19:B772
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B782
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B792
    .db $20,$20,$20,$20,$20,$20,$54,$68,$65,$72,$65,$20,$77,$61,$73,$20; 19:B7A2
    .db $73,$74,$69,$6C,$6C,$20,$6F,$6E,$65,$20,$68,$6F,$70,$65,$5F,$20; 19:B7B2
    .db $20,$16,$16,$16,$16,$CF,$B7,$FF,$B7,$2F,$B8,$5F,$B8,$20,$50,$20; 19:B7C2
    .db $52,$20,$4F,$20,$47,$20,$52,$20,$41,$20,$4D,$20,$20,$20,$30,$20; 19:B7D2
    .db $32,$20,$35,$20,$36,$20,$20,$20,$66,$20,$75,$20,$6E,$20,$63,$20; 19:B7E2
    .db $74,$20,$69,$20,$6F,$20,$6E,$20,$61,$20,$6C,$20,$20,$20,$53,$20; 19:B7F2
    .db $59,$20,$53,$20,$54,$20,$45,$20,$4D,$20,$20,$20,$20,$20,$31,$20; 19:B802
    .db $30,$20,$32,$20,$34,$20,$20,$20,$66,$20,$75,$20,$6E,$20,$63,$20; 19:B812
    .db $74,$20,$69,$20,$6F,$20,$6E,$20,$61,$20,$6C,$20,$20,$20,$43,$20; 19:B822
    .db $4F,$20,$4E,$20,$44,$20,$49,$20,$54,$20,$49,$20,$4F,$20,$4E,$20; 19:B832
    .db $3A,$20,$20,$20,$20,$20,$20,$20,$47,$20,$52,$20,$45,$20,$45,$20; 19:B842
    .db $4E,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$49,$20; 19:B852
    .db $4E,$20,$50,$20,$55,$20,$54,$20,$20,$20,$43,$20,$4F,$20,$44,$20; 19:B862
    .db $45,$20,$20,$20,$4E,$20,$41,$20,$4D,$20,$45,$20,$20,$20,$20,$20; 19:B872
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B882
    .db $20,$20,$20                     ; 19:B892
StaleFill19_B895:  ; dead filler to B8FF: copy of 17:B895 rect ptr table
    .db $F4,$F3,$FF,$F5,$00,$0D,$FB,$FF,$F4,$00,$0D,$F5,$FF,$F2,$00,$0F; 19:B895
    .db $F3,$00,$0F,$F5,$F1,$00,$0F,$F4,$FF,$F2,$00,$0E,$F5,$FF,$FF,$F4; 19:B8A5
    .db $00,$0E,$F3,$00,$0F,$FA,$00,$0F,$FA,$00,$0F,$F2,$00,$0F,$F3,$00; 19:B8B5
    .db $0F,$F5,$00,$0F,$F4,$00,$0F,$F2,$00,$0F,$F4,$00,$00,$D2,$B8,$E6; 19:B8C5
    .db $B8,$68,$B9,$EA,$B9,$6C,$BA,$EE,$BA,$70,$BB,$F2,$BB,$74,$BC,$F6; 19:B8D5
    .db $BC,$10,$08,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B8E5
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:B8F5
PlayerDeath:  ; substate 3 handler (bank n=9); Opel Statue selected ($0715=$0A) -> OpelRevive
    LDA obj1_flash                      ; B900  ad 10 07
    AND #$0F                            ; B903  29 0f
    STA obj1_flash                      ; B905  8d 10 07
    JSR sub_30_C008                     ; B908  20 08 c0
    LDA #$00                            ; B90B  a9 00
    STA orbit_level,X                   ; B90D  9d c0 06
    STA irq_mode                        ; B910  85 51
    LDA use_item_sel                    ; B912  ad 15 07
    CMP #$0A                            ; B915  c9 0a
    BNE loc_19_B91C                     ; B917  d0 03
    JMP OpelRevive                      ; B919  4c b0 b9
loc_19_B91C:
    LDA #$00                            ; B91C  a9 00
    JSR PlaySound                       ; B91E  20 25 c1
    LDA #$00                            ; B921  a9 00
    STA orbit_level                     ; B923  8d c0 06
    LDA #$10                            ; B926  a9 10
    STA pal_nofade_mask                 ; B928  8d e8 07
    LDA #$1A                            ; B92B  a9 1a
    JSR SetR6                           ; B92D  20 18 c4
    LDA #$0A                            ; B930  a9 0a
    JSR Bank26_FadeA                    ; B932  20 3d 8c
    LDA #$3E                            ; B935  a9 3e
    JSR PlaySound                       ; B937  20 25 c1
    LDY #$02                            ; B93A  a0 02
    LDA #$00                            ; B93C  a9 00
loc_19_B93E:
    STA obj_active,Y                    ; B93E  99 a0 04
    INY                                 ; B941  c8
    CPY #$1F                            ; B942  c0 1f
    BNE loc_19_B93E                     ; B944  d0 f8
    JSR WaitFrame                       ; B946  20 69 c1
    LDA obj_flags_s01                   ; B949  ad 81 03
    AND #$FB                            ; B94C  29 fb
    STA obj_flags_s01                   ; B94E  8d 81 03
    LDA #$5C                            ; B951  a9 5c
    STA chr_r2_play                     ; B953  8d f2 07
    LDA #$BC                            ; B956  a9 bc
    STA obj_meta_id_s01                 ; B958  8d 01 03
    LDA #$40                            ; B95B  a9 40
    STA obj_anim_s01                    ; B95D  8d e1 04
    LDA #$4C                            ; B960  a9 4c
    STA obj_overlay_id_s01              ; B962  8d 81 05
    LDA #$00                            ; B965  a9 00
    STA obj_tile_ofs_s01                ; B967  8d 21 03
    JSR sub_19_BA03                     ; B96A  20 03 ba
    LDA #$BD                            ; B96D  a9 bd
    STA obj_meta_id_s01                 ; B96F  8d 01 03
    LDA #$40                            ; B972  a9 40
    STA obj_anim_s01                    ; B974  8d e1 04
    JSR sub_19_BA03                     ; B977  20 03 ba
    LDA #$AF                            ; B97A  a9 af
    STA pal_ids                         ; B97C  8d e0 07
    STA pal_id_bg1                      ; B97F  8d e1 07
    STA pal_id_bg2                      ; B982  8d e2 07
    STA pal_id_bg3                      ; B985  8d e3 07
    LDA #$4F                            ; B988  a9 4f
    STA pal_id_spr1                     ; B98A  8d e5 07
    STA pal_id_spr2                     ; B98D  8d e6 07
    STA pal_id_spr3                     ; B990  8d e7 07
    LDA #$00                            ; B993  a9 00
    STA pal_nofade_mask                 ; B995  8d e8 07
    LDA #$1A                            ; B998  a9 1a
    JSR SetR6                           ; B99A  20 18 c4
    JSR PaletteBuild                    ; B99D  20 0e 8c
    JSR WaitFrame                       ; B9A0  20 69 c1
    LDA #$10                            ; B9A3  a9 10
    JSR Bank26_FadeA                    ; B9A5  20 3d 8c
    JSR sub_19_BA0F                     ; B9A8  20 0f ba
    LDA #$02                            ; B9AB  a9 02
    STA main_state                      ; B9AD  85 40
    RTS                                 ; B9AF  60
OpelRevive:  ; $0743=$40; SFX $68; refill HP to max 4/frame; sub_state=6
    LDY #$40                            ; B9B0  a0 40
    STY opel_revive                     ; B9B2  8c 43 07
    JSR sub_30_C176                     ; B9B5  20 76 c1
    LDA #$68                            ; B9B8  a9 68
    JSR PlaySound                       ; B9BA  20 25 c1
loc_19_B9BD:
    JSR WaitFrame                       ; B9BD  20 69 c1
    JSR FarCall_B14_800E                ; B9C0  20 8c ff
    LDA #$04                            ; B9C3  a9 04
    STA jmp_trampoline                  ; B9C5  85 10
loc_19_B9C7:
    INC player_hp                       ; B9C7  ee c1 03
    LDA player_hp                       ; B9CA  ad c1 03
    CMP player_hp_max                   ; B9CD  cd c0 03
    BEQ loc_19_B9DC                     ; B9D0  f0 0a
    DEC jmp_trampoline                  ; B9D2  c6 10
    BNE loc_19_B9C7                     ; B9D4  d0 f1
    JSR sub_19_B9FB                     ; B9D6  20 fb b9
    JMP loc_19_B9BD                     ; B9D9  4c bd b9
loc_19_B9DC:
    JSR sub_19_B9FB                     ; B9DC  20 fb b9
    LDA #$04                            ; B9DF  a9 04
    JSR HudRedraw                       ; B9E1  20 46 8e
    LDA obj_flags_s01                   ; B9E4  ad 81 03
    ORA #$04                            ; B9E7  09 04
    STA obj_flags_s01                   ; B9E9  8d 81 03
    LDA #$FF                            ; B9EC  a9 ff
    STA invuln_timer                    ; B9EE  8d 1a 07
    LDY #$40                            ; B9F1  a0 40
    JSR sub_30_C176                     ; B9F3  20 76 c1
    LDA #$06                            ; B9F6  a9 06
    STA sub_state                       ; B9F8  85 41
    RTS                                 ; B9FA  60
sub_19_B9FB:
    LDA #$1A                            ; B9FB  a9 1a
    JSR SetR6                           ; B9FD  20 18 c4
    JMP HudHpBar                        ; BA00  4c c0 8c
sub_19_BA03:
    JSR WaitFrame                       ; BA03  20 69 c1
    JSR FarCall_B14_800E                ; BA06  20 8c ff
    DEC obj_anim_s01                    ; BA09  ce e1 04
    BNE sub_19_BA03                     ; BA0C  d0 f5
    RTS                                 ; BA0E  60
sub_19_BA0F:
    LDA #$F0                            ; BA0F  a9 f0
    JSR sub_30_C188                     ; BA11  20 88 c1
    LDA #$48                            ; BA14  a9 48
    STA pal_id_spr0                     ; BA16  8d e4 07
    LDA #$04                            ; BA19  a9 04
    JSR Bank26_FadeB                    ; BA1B  20 49 8c
    LDX #$1F                            ; BA1E  a2 1f
loc_19_BA20:
    LDA $BA45,X                         ; BA20  bd 45 ba
    STA oam_y,X                         ; BA23  9d 00 02
    DEX                                 ; BA26  ca
    BPL loc_19_BA20                     ; BA27  10 f7
    LDA #$35                            ; BA29  a9 35
    STA chr_r3_play                     ; BA2B  8d f3 07
    LDA #$50                            ; BA2E  a9 50
    STA tmp_2F                          ; BA30  85 2f
loc_19_BA32:
    JSR WaitFrame                       ; BA32  20 69 c1
    JSR ReadPads                        ; BA35  20 80 fe
    DEC tmp_2F                          ; BA38  c6 2f
    BEQ loc_19_BA40                     ; BA3A  f0 04
    LDA pad_new                         ; BA3C  a5 4b
    BEQ loc_19_BA32                     ; BA3E  f0 f2
loc_19_BA40:
    LDA #$02                            ; BA40  a9 02
    JMP Bank26_FadeA                    ; BA42  4c 3d 8c
    .db $60,$47,$00,$5C,$60,$41,$00,$64,$60,$4D,$00,$6C,$60,$45,$00,$74; 19:BA45
    .db $60,$4F,$00,$84,$60,$56,$00,$8C,$60,$45,$00,$94,$60,$52,$00,$9C; 19:BA55
sub_19_BA65:
    JSR WaitPpuQueue                    ; BA65  20 76 c6
    LDX #$1F                            ; BA68  a2 1f
loc_19_BA6A:
    LDA #$19                            ; BA6A  a9 19
    STA wram_buf,X                      ; BA6C  9d 00 60
    LDA #$1C                            ; BA6F  a9 1c
    STA wram_6020,X                     ; BA71  9d 20 60
    LDA #$FF                            ; BA74  a9 ff
    STA wram_6040,X                     ; BA76  9d 40 60
    LDA #$FF                            ; BA79  a9 ff
    STA wram_6060,X                     ; BA7B  9d 60 60
    STA $6080,X                         ; BA7E  9d 80 60
    DEX                                 ; BA81  ca
    BPL loc_19_BA6A                     ; BA82  10 e6
    LDA #$18                            ; BA84  a9 18
    STA wram_6001                       ; BA86  8d 01 60
    LDA #$1A                            ; BA89  a9 1a
    STA $601F                           ; BA8B  8d 1f 60
    LDA #$00                            ; BA8E  a9 00
    JSR PpuQueueBlock                   ; BA90  20 82 c4
    LDA #$1B                            ; BA93  a9 1b
    STA wram_6021                       ; BA95  8d 21 60
    LDA #$1D                            ; BA98  a9 1d
    STA $603F                           ; BA9A  8d 3f 60
    LDA #$01                            ; BA9D  a9 01
    JSR PpuQueueBlock                   ; BA9F  20 82 c4
    LDA #$02                            ; BAA2  a9 02
    JSR PpuQueueBlock                   ; BAA4  20 82 c4
    LDA #$03                            ; BAA7  a9 03
    JSR PpuQueueBlock                   ; BAA9  20 82 c4
    JSR WaitPpuQueue                    ; BAAC  20 76 c6
    LDX #$5F                            ; BAAF  a2 5f
loc_19_BAB1:
    LDA $BAD9,X                         ; BAB1  bd d9 ba
    STA wram_buf,X                      ; BAB4  9d 00 60
    DEX                                 ; BAB7  ca
    BPL loc_19_BAB1                     ; BAB8  10 f7
    LDA #$04                            ; BABA  a9 04
    JSR PpuQueueBlock                   ; BABC  20 82 c4
    LDA #$1A                            ; BABF  a9 1a
    JSR SetR6                           ; BAC1  20 18 c4
    JSR HudHpBar                        ; BAC4  20 c0 8c
    JSR sub_26_8D59                     ; BAC7  20 59 8d
    LDX #$05                            ; BACA  a2 05
loc_19_BACC:
    TXA                                 ; BACC  8a
    PHA                                 ; BACD  48
    JSR HudRedraw                       ; BACE  20 46 8e
    PLA                                 ; BAD1  68
    TAX                                 ; BAD2  aa
    DEX                                 ; BAD3  ca
    BPL loc_19_BACC                     ; BAD4  10 f6
    JMP WaitPpuQueue                    ; BAD6  4c 76 c6
    .db $FE,$1E,$80,$81,$82,$83,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 19:BAD9
    .db $20,$20,$20,$20,$20,$20,$20,$F1,$F2,$00,$01,$20,$20,$20,$20,$1F; 19:BAE9
    .db $FE,$1E,$84,$85,$86,$87,$8F,$8F,$8F,$8F,$8F,$8F,$8F,$93,$95,$94; 19:BAF9
    .db $90,$91,$92,$90,$91,$92,$20,$9C,$20,$00,$00,$00,$00,$00,$20,$1F; 19:BB09
    .db $FE,$1E,$20,$20,$20,$20,$9A,$9B,$00,$00,$00,$00,$00,$9D,$00,$00; 19:BB19
    .db $00,$00,$00,$20,$20,$F4,$FD,$00,$00,$00,$9D,$00,$00,$00,$20,$1F; 19:BB29
SubstCureMagic:  ; s60: substate 24 (magic 5 / stone cure): status cleared; 63-frame anim; msg 32.2
    JSR sub_30_C874                     ; BB39  20 74 c8
    LDA obj1_flash                      ; BB3C  ad 10 07
    AND #$F0                            ; BB3F  29 f0
    STA obj1_flash                      ; BB41  8d 10 07
    LDA #$00                            ; BB44  a9 00
    STA obj_freeze                      ; BB46  8d a0 05
    LDA #$40                            ; BB49  a9 40
    STA chr_r2_play                     ; BB4B  8d f2 07
    LDA #$42                            ; BB4E  a9 42
    STA chr_r3_play                     ; BB50  8d f3 07
    LDA obj_meta_id_s01                 ; BB53  ad 01 03
    PHA                                 ; BB56  48
    LDA #$02                            ; BB57  a9 02
    STA obj_meta_id                     ; BB59  8d 00 03
    LDA #$CB                            ; BB5C  a9 cb
    STA obj_overlay_id_s01              ; BB5E  8d 81 05
    LDA #$3F                            ; BB61  a9 3f
    STA obj_anim_s01                    ; BB63  8d e1 04
    LDA #$4C                            ; BB66  a9 4c
    STA obj_meta_id_s01                 ; BB68  8d 01 03
    LDA #$00                            ; BB6B  a9 00
    STA obj_tile_ofs_s01                ; BB6D  8d 21 03
    LDA #$25                            ; BB70  a9 25
    JSR PlaySound                       ; BB72  20 25 c1
loc_19_BB75:
    JSR WaitFrame                       ; BB75  20 69 c1
    JSR FarCall_B14_800E                ; BB78  20 8c ff
    DEC obj_anim_s01                    ; BB7B  ce e1 04
    BNE loc_19_BB75                     ; BB7E  d0 f5
    PLA                                 ; BB80  68
    STA obj_meta_id_s01                 ; BB81  8d 01 03
    JSR sub_30_C008                     ; BB84  20 08 c0
    LDA #$A7                            ; BB87  a9 a7
    STA obj_meta_id                     ; BB89  8d 00 03
    LDA #$10                            ; BB8C  a9 10
    STA sub_state                       ; BB8E  85 41
    LDA #$02                            ; BB90  a9 02
    STA pending_msg_id                  ; BB92  8d c3 06
    LDA #$20                            ; BB95  a9 20
    STA pending_msg_group               ; BB97  8d e3 06
    JMP sub_30_C739                     ; BB9A  4c 39 c7
SubstFormRevert:  ; s60: substate 25: PlayerMagic with status b7 (changed form) -> human graphics back
    JSR sub_30_C874                     ; BB9D  20 74 c8
    JSR WaitFrame                       ; BBA0  20 69 c1
    LDA #$40                            ; BBA3  a9 40
    LDY equip_shield                    ; BBA5  ac 14 07
    BEQ loc_19_BBAC                     ; BBA8  f0 02
    LDA #$41                            ; BBAA  a9 41
loc_19_BBAC:
    STA chr_r2_play                     ; BBAC  8d f2 07
    LDA #$5C                            ; BBAF  a9 5c
    STA chr_r3_play                     ; BBB1  8d f3 07
    LDA obj_flags_s01                   ; BBB4  ad 81 03
    ORA #$02                            ; BBB7  09 02
    STA obj_flags_s01                   ; BBB9  8d 81 03
    LDA #$02                            ; BBBC  a9 02
    STA obj_meta_id                     ; BBBE  8d 00 03
    LDA #$00                            ; BBC1  a9 00
    STA obj_tile_ofs_s01                ; BBC3  8d 21 03
    LDA #$D0                            ; BBC6  a9 d0
    STA obj_overlay_id_s01              ; BBC8  8d 81 05
    LDA #$1F                            ; BBCB  a9 1f
    STA obj_anim_s01                    ; BBCD  8d e1 04
    LDA #$4C                            ; BBD0  a9 4c
    STA obj_meta_id_s01                 ; BBD2  8d 01 03
    LDA obj1_flash                      ; BBD5  ad 10 07
    AND #$4F                            ; BBD8  29 4f
    STA obj1_flash                      ; BBDA  8d 10 07
    LDA #$39                            ; BBDD  a9 39
    JSR PlaySound                       ; BBDF  20 25 c1
    LDA #$F0                            ; BBE2  a9 f0
    JSR OamClipTop                      ; BBE4  20 7d c1
loc_19_BBE7:
    JSR WaitFrame                       ; BBE7  20 69 c1
    JSR FarCall_B14_800E                ; BBEA  20 8c ff
    DEC obj_anim_s01                    ; BBED  ce e1 04
    BNE loc_19_BBE7                     ; BBF0  d0 f5
    JSR sub_30_C008                     ; BBF2  20 08 c0
    LDA #$A7                            ; BBF5  a9 a7
    STA obj_meta_id                     ; BBF7  8d 00 03
    JSR FarCall_B14_800E                ; BBFA  20 8c ff
    LDA #$08                            ; BBFD  a9 08
    STA sub_state                       ; BBFF  85 41
    JMP sub_30_C739                     ; BC01  4c 39 c7
SwordRaisePose:  ; event act 1 cutscene: player meta $B8 + CHR R2=$5C R3=$42; jingle $93; 127-frame hold; sub_state=8
    JSR sub_30_C867                     ; BC04  20 67 c8
    JSR WaitFrame                       ; BC07  20 69 c1
    LDA #$5C                            ; BC0A  a9 5c
    STA chr_r2_play                     ; BC0C  8d f2 07
    LDA #$42                            ; BC0F  a9 42
    STA chr_r3_play                     ; BC11  8d f3 07
    LDA #$00                            ; BC14  a9 00
    STA orbit_level                     ; BC16  8d c0 06
    LDA obj_meta_id_s01                 ; BC19  ad 01 03
    PHA                                 ; BC1C  48
    LDA obj_overlay_id_s01              ; BC1D  ad 81 05
    PHA                                 ; BC20  48
    LDA #$B8                            ; BC21  a9 b8
    STA obj_meta_id_s01                 ; BC23  8d 01 03
    LDA #$4C                            ; BC26  a9 4c
    STA obj_overlay_id_s01              ; BC28  8d 81 05
    LDA #$7F                            ; BC2B  a9 7f
    STA obj_anim_s01                    ; BC2D  8d e1 04
    LDA snd_cur_music                   ; BC30  ad 02 01
    ORA #$80                            ; BC33  09 80
    STA snd_cur_music                   ; BC35  8d 02 01
    LDA #$93                            ; BC38  a9 93
    JSR PlaySound                       ; BC3A  20 25 c1
    LDA #$F0                            ; BC3D  a9 f0
    JSR OamClipTop                      ; BC3F  20 7d c1
loc_19_BC42:
    JSR WaitFrame                       ; BC42  20 69 c1
    JSR FarCall_B14_800E                ; BC45  20 8c ff
    DEC obj_anim_s01                    ; BC48  ce e1 04
    BNE loc_19_BC42                     ; BC4B  d0 f5
    JSR WaitFrame                       ; BC4D  20 69 c1
    PLA                                 ; BC50  68
    STA obj_overlay_id_s01              ; BC51  8d 81 05
    PLA                                 ; BC54  68
    STA obj_meta_id_s01                 ; BC55  8d 01 03
    JSR sub_30_C008                     ; BC58  20 08 c0
    LDA #$F0                            ; BC5B  a9 f0
    JSR OamClipTop                      ; BC5D  20 7d c1
    JSR FarCall_B14_800E                ; BC60  20 8c ff
    JSR WaitFrame                       ; BC63  20 69 c1
    LDA #$08                            ; BC66  a9 08
    STA sub_state                       ; BC68  85 41
    RTS                                 ; BC6A  60
SubstMesiaReunion:  ; s60: substate 27 (event act 30 trigger $A4): scene then sub_state=7 GiveItem
    LDA #$00                            ; BC6B  a9 00
    STA obj_phase                       ; BC6D  8d 00 06
    STA obj_timer2                      ; BC70  8d 20 06
    STA orbit_level                     ; BC73  8d c0 06
    STA obj1_flash                      ; BC76  8d 10 07
    LDA obj_flags_s01                   ; BC79  ad 81 03
    AND #$FB                            ; BC7C  29 fb
    STA obj_flags_s01                   ; BC7E  8d 81 03
    LDA #$1A                            ; BC81  a9 1a
    JSR SetR6                           ; BC83  20 18 c4
    JSR sub_30_C008                     ; BC86  20 08 c0
    LDX #$01                            ; BC89  a2 01
    LDA #$00                            ; BC8B  a9 00
    STA tmp_20                          ; BC8D  85 20
    LDA #$15                            ; BC8F  a9 15
    STA tmp_22                          ; BC91  85 22
    JSR sub_19_BCB0                     ; BC93  20 b0 bc
    JSR sub_19_BD91                     ; BC96  20 91 bd
    LDA #$1A                            ; BC99  a9 1a
    JSR SetR6                           ; BC9B  20 18 c4
    JSR PaletteBuild                    ; BC9E  20 0e 8c
    LDA #$1F                            ; BCA1  a9 1f
    STA obj_timer2_s03                  ; BCA3  8d 23 06
    LDA #$04                            ; BCA6  a9 04
    STA $057F                           ; BCA8  8d 7f 05
    LDA #$07                            ; BCAB  a9 07
    STA sub_state                       ; BCAD  85 41
    RTS                                 ; BCAF  60
sub_19_BCB0:
    LDA tmp_20                          ; BCB0  a5 20
    ASL A                               ; BCB2  0a
    ASL A                               ; BCB3  0a
    CLC                                 ; BCB4  18
    ADC tmp_20                          ; BCB5  65 20
    TAY                                 ; BCB7  a8
    LDA $BD59,Y                         ; BCB8  b9 59 bd
    CMP #$80                            ; BCBB  c9 80
    BEQ loc_19_BD00                     ; BCBD  f0 41
    CMP #$81                            ; BCBF  c9 81
    BNE loc_19_BCC4                     ; BCC1  d0 01
    RTS                                 ; BCC3  60
loc_19_BCC4:
    STA tmp_30                          ; BCC4  85 30
    LDA $BD5A,Y                         ; BCC6  b9 5a bd
    STA tmp_31                          ; BCC9  85 31
    LDA $BD5B,Y                         ; BCCB  b9 5b bd
    STA tmp_21                          ; BCCE  85 21
    LDA $BD5C,Y                         ; BCD0  b9 5c bd
    STA obj_meta_id,X                   ; BCD3  9d 00 03
    LDA $BD5D,Y                         ; BCD6  b9 5d bd
    STA obj_overlay_id,X                ; BCD9  9d 80 05
loc_19_BCDC:
    JSR WaitFrame                       ; BCDC  20 69 c1
    JSR ObjPosToTemp                    ; BCDF  20 a8 98
    JSR TempAddStep                     ; BCE2  20 61 98
    JSR TempToObjPos                    ; BCE5  20 97 98
    CPX #$01                            ; BCE8  e0 01
    BNE loc_19_BCF1                     ; BCEA  d0 05
    DEX                                 ; BCEC  ca
    JSR TempToObjPos                    ; BCED  20 97 98
    INX                                 ; BCF0  e8
loc_19_BCF1:
    DEC obj_anim,X                      ; BCF1  de e0 04
    JSR sub_19_BD51                     ; BCF4  20 51 bd
    DEC tmp_21                          ; BCF7  c6 21
    BNE loc_19_BCDC                     ; BCF9  d0 e1
    INC tmp_20                          ; BCFB  e6 20
    JMP sub_19_BCB0                     ; BCFD  4c b0 bc
loc_19_BD00:
    LDA $BD5B,Y                         ; BD00  b9 5b bd
    STA obj_anim,X                      ; BD03  9d e0 04
    LDA $BD5C,Y                         ; BD06  b9 5c bd
    STA obj_meta_id,X                   ; BD09  9d 00 03
    LDA $BD5D,Y                         ; BD0C  b9 5d bd
    STA obj_overlay_id,X                ; BD0F  9d 80 05
loc_19_BD12:
    JSR WaitFrame                       ; BD12  20 69 c1
    JSR sub_19_BD51                     ; BD15  20 51 bd
    DEC obj_anim,X                      ; BD18  de e0 04
    LDA obj_anim,X                      ; BD1B  bd e0 04
    CMP #$08                            ; BD1E  c9 08
    BNE loc_19_BD12                     ; BD20  d0 f0
    INC tmp_20                          ; BD22  e6 20
    TXA                                 ; BD24  8a
    PHA                                 ; BD25  48
    JSR ObjPosToTemp                    ; BD26  20 a8 98
    LDA tmp_22                          ; BD29  a5 22
    STA jmp_trampoline                  ; BD2B  85 10
    LDA #$C2                            ; BD2D  a9 c2
    STA tmp_11                          ; BD2F  85 11
    JSR FarCall_C25D                    ; BD31  20 80 ff
    LDX jmp_trampoline                  ; BD34  a6 10
    JSR TempToObjPos                    ; BD36  20 97 98
    LDA obj_y_lo,X                      ; BD39  b5 b0
    SEC                                 ; BD3B  38
    SBC #$18                            ; BD3C  e9 18
    STA obj_y_lo,X                      ; BD3E  95 b0
    LDA #$0D                            ; BD40  a9 0d
    STA obj_flags,X                     ; BD42  9d 80 03
    LDA #$FE                            ; BD45  a9 fe
    STA obj_meta_id,X                   ; BD47  9d 00 03
    INC tmp_22                          ; BD4A  e6 22
    PLA                                 ; BD4C  68
    TAX                                 ; BD4D  aa
    JMP sub_19_BCB0                     ; BD4E  4c b0 bc
sub_19_BD51:
    TXA                                 ; BD51  8a
    PHA                                 ; BD52  48
    JSR FarCall_B14_800E                ; BD53  20 8c ff
    PLA                                 ; BD56  68
    TAX                                 ; BD57  aa
    RTS                                 ; BD58  60
    .db $00,$FF,$10,$00,$08,$FF,$00,$30,$03,$0B,$80,$00,$17,$04,$4C,$01; 19:BD59
    .db $00,$20,$01,$09,$80,$00,$17,$04,$4C,$01,$00,$20,$01,$09,$80,$00; 19:BD69
    .db $17,$04,$4C,$01,$00,$20,$01,$09,$80,$00,$17,$04,$4C,$FF,$00,$30; 19:BD79
    .db $03,$0B,$00,$00,$01,$00,$08,$81 ; 19:BD89
sub_19_BD91:
    LDA #$10                            ; BD91  a9 10
    STA tmp_21                          ; BD93  85 21
loc_19_BD95:
    JSR WaitFrame                       ; BD95  20 69 c1
    INC zp_85                           ; BD98  e6 85
    INC zp_85                           ; BD9A  e6 85
    INC zp_85                           ; BD9C  e6 85
    INC zp_86                           ; BD9E  e6 86
    DEC zp_87                           ; BDA0  c6 87
    DEC zp_88                           ; BDA2  c6 88
    DEC zp_88                           ; BDA4  c6 88
    DEC zp_88                           ; BDA6  c6 88
    JSR FarCall_B14_800E                ; BDA8  20 8c ff
    DEC tmp_21                          ; BDAB  c6 21
    BNE loc_19_BD95                     ; BDAD  d0 e6
    LDA #$00                            ; BDAF  a9 00
    STA $04B5                           ; BDB1  8d b5 04
    STA $04B6                           ; BDB4  8d b6 04
    STA $04B7                           ; BDB7  8d b7 04
    LDA #$09                            ; BDBA  a9 09
    STA $0398                           ; BDBC  8d 98 03
    LDA #$08                            ; BDBF  a9 08
    STA tmp_21                          ; BDC1  85 21
loc_19_BDC3:
    LDA #$30                            ; BDC3  a9 30
    JSR sub_19_BDDC                     ; BDC5  20 dc bd
    LDY #$04                            ; BDC8  a0 04
    JSR WaitFrame                       ; BDCA  20 69 c1
    INC $C8                             ; BDCD  e6 c8
    JSR sub_19_BD51                     ; BDCF  20 51 bd
    DEC tmp_21                          ; BDD2  c6 21
    BNE loc_19_BDC3                     ; BDD4  d0 ed
    LDA #$00                            ; BDD6  a9 00
    STA $04B8                           ; BDD8  8d b8 04
    RTS                                 ; BDDB  60
sub_19_BDDC:
    LDY #$1F                            ; BDDC  a0 1f
loc_19_BDDE:
    STA pal_buf,Y                       ; BDDE  99 40 61
    DEY                                 ; BDE1  88
    BPL loc_19_BDDE                     ; BDE2  10 fa
    JSR WaitFrame                       ; BDE4  20 69 c1
    LDA #$1A                            ; BDE7  a9 1a
    JSR SetR6                           ; BDE9  20 18 c4
    JSR PaletteBuild                    ; BDEC  20 0e 8c
    JMP WaitFrame                       ; BDEF  4c 69 c1
SubstFinalSceneReset:  ; s60: substate 30 (AI $70 phase 10): clear slots 4-$1E; screen rebuild
    LDY #$04                            ; BDF2  a0 04
    LDA #$00                            ; BDF4  a9 00
loc_19_BDF6:
    STA obj_active,Y                    ; BDF6  99 a0 04
    INY                                 ; BDF9  c8
    CPY #$1F                            ; BDFA  c0 1f
    BNE loc_19_BDF6                     ; BDFC  d0 f8
    LDA #$00                            ; BDFE  a9 00
    STA irq_mode                        ; BE00  85 51
    STA tmp_2E                          ; BE02  85 2e
    STA obj_phase                       ; BE04  8d 00 06
    STA obj_timer2                      ; BE07  8d 20 06
    STA orbit_level                     ; BE0A  8d c0 06
    LDA #$47                            ; BE0D  a9 47
    STA chr_r4_play                     ; BE0F  8d f4 07
    LDA #$06                            ; BE12  a9 06
    STA pal_id_spr2                     ; BE14  8d e6 07
    LDA #$1C                            ; BE17  a9 1c
    STA pal_id_spr3                     ; BE19  8d e7 07
    LDA #$1A                            ; BE1C  a9 1a
    JSR SetR6                           ; BE1E  20 18 c4
    JSR PaletteBuild                    ; BE21  20 0e 8c
    LDA #$00                            ; BE24  a9 00
    STA obj1_flash                      ; BE26  8d 10 07
    JSR sub_30_C008                     ; BE29  20 08 c0
    LDY #$00                            ; BE2C  a0 00
loc_19_BE2E:
    LDA #$FF                            ; BE2E  a9 ff
    STA wram_buf,Y                      ; BE30  99 00 60
    DEY                                 ; BE33  88
    BNE loc_19_BE2E                     ; BE34  d0 f8
    LDA #$39                            ; BE36  a9 39
    JSR PpuQueueBlock                   ; BE38  20 82 c4
    LDA #$3A                            ; BE3B  a9 3a
    JSR PpuQueueBlock                   ; BE3D  20 82 c4
    LDA #$3B                            ; BE40  a9 3b
    JSR PpuQueueBlock                   ; BE42  20 82 c4
    LDA #$3C                            ; BE45  a9 3c
    JSR PpuQueueBlock                   ; BE47  20 82 c4
    LDY #$3F                            ; BE4A  a0 3f
    LDA #$00                            ; BE4C  a9 00
loc_19_BE4E:
    STA attr_shadow,Y                   ; BE4E  99 00 61
    DEY                                 ; BE51  88
    BPL loc_19_BE4E                     ; BE52  10 fa
    JSR sub_30_C739                     ; BE54  20 39 c7
    LDA #$0B                            ; BE57  a9 0b
    JSR SetR6                           ; BE59  20 18 c4
    LDY #$00                            ; BE5C  a0 00
loc_19_BE5E:
    LDA $9E00,Y                         ; BE5E  b9 00 9e
    STA wram_buf,Y                      ; BE61  99 00 60
    INY                                 ; BE64  c8
    BNE loc_19_BE5E                     ; BE65  d0 f7
    LDA #$45                            ; BE67  a9 45
    JSR PpuQueueBlock                   ; BE69  20 82 c4
    JSR WaitPpuQueue                    ; BE6C  20 76 c6
    LDA #$1A                            ; BE6F  a9 1a
    JSR SetR6                           ; BE71  20 18 c4
loc_19_BE74:
    DEC obj_anim_s01                    ; BE74  ce e1 04
    JSR WaitFrame                       ; BE77  20 69 c1
    LDA #$01                            ; BE7A  a9 01
    STA obj_meta_id_s01                 ; BE7C  8d 01 03
    LDA #$09                            ; BE7F  a9 09
    STA obj_overlay_id_s01              ; BE81  8d 81 05
    LDY #$01                            ; BE84  a0 01
    BIT obj_x_lo                        ; BE86  24 70
    BPL loc_19_BE96                     ; BE88  10 0c
    LDY #$FF                            ; BE8A  a0 ff
    LDA #$03                            ; BE8C  a9 03
    STA obj_meta_id_s01                 ; BE8E  8d 01 03
    LDA #$0B                            ; BE91  a9 0b
    STA obj_overlay_id_s01              ; BE93  8d 81 05
loc_19_BE96:
    TYA                                 ; BE96  98
    CLC                                 ; BE97  18
    ADC obj_x_lo                        ; BE98  65 70
    STA obj_x_lo                        ; BE9A  85 70
    STA obj_x_lo_s01                    ; BE9C  85 71
    JSR FarCall_B14_800E                ; BE9E  20 8c ff
    LDA obj_x_lo                        ; BEA1  a5 70
    CMP #$80                            ; BEA3  c9 80
    BNE loc_19_BE74                     ; BEA5  d0 cd
    LDA #$00                            ; BEA7  a9 00
    STA obj_meta_id_s01                 ; BEA9  8d 01 03
    LDA #$08                            ; BEAC  a9 08
    STA obj_overlay_id_s01              ; BEAE  8d 81 05
loc_19_BEB1:
    JSR WaitFrame                       ; BEB1  20 69 c1
    DEC obj_anim_s01                    ; BEB4  ce e1 04
    DEC obj_y_lo                        ; BEB7  c6 b0
    LDA obj_y_lo                        ; BEB9  a5 b0
    STA obj_y_lo_s01                    ; BEBB  85 b1
    JSR FarCall_B14_800E                ; BEBD  20 8c ff
    LDA obj_y_lo                        ; BEC0  a5 b0
    CMP #$78                            ; BEC2  c9 78
    BNE loc_19_BEB1                     ; BEC4  d0 eb
    LDA #$04                            ; BEC6  a9 04
    STA obj_meta_id_s01                 ; BEC8  8d 01 03
    LDA #$08                            ; BECB  a9 08
    STA obj_overlay_id_s01              ; BECD  8d 81 05
    LDA #$17                            ; BED0  a9 17
    STA obj_anim_s01                    ; BED2  8d e1 04
loc_19_BED5:
    JSR WaitFrame                       ; BED5  20 69 c1
    JSR FarCall_B14_800E                ; BED8  20 8c ff
    DEC obj_anim_s01                    ; BEDB  ce e1 04
    LDA obj_anim_s01                    ; BEDE  ad e1 04
    CMP #$08                            ; BEE1  c9 08
    BNE loc_19_BED5                     ; BEE3  d0 f0
    LDA #$00                            ; BEE5  a9 00
    STA obj_meta_id_s01                 ; BEE7  8d 01 03
    LDA #$5C                            ; BEEA  a9 5c
    JSR PlaySound                       ; BEEC  20 25 c1
    LDX #$00                            ; BEEF  a2 00
    JSR ObjPosToTemp                    ; BEF1  20 a8 98
    LDX #$0D                            ; BEF4  a2 0d
    STX jmp_trampoline                  ; BEF6  86 10
    LDA #$C2                            ; BEF8  a9 c2
    STA tmp_11                          ; BEFA  85 11
    JSR FarCall_C25D                    ; BEFC  20 80 ff
    JSR TempToObjPos                    ; BEFF  20 97 98
    LDA obj_y_lo,X                      ; BF02  b5 b0
    SEC                                 ; BF04  38
    SBC #$10                            ; BF05  e9 10
    STA obj_y_lo,X                      ; BF07  95 b0
    LDA #$09                            ; BF09  a9 09
    STA $038D                           ; BF0B  8d 8d 03
    LDA #$FE                            ; BF0E  a9 fe
    STA obj_meta_id_s0D                 ; BF10  8d 0d 03
    LDA #$08                            ; BF13  a9 08
    STA obj_anim_s0D                    ; BF15  8d ed 04
    JSR FarCall_B14_800E                ; BF18  20 8c ff
    JSR sub_19_BFC9                     ; BF1B  20 c9 bf
    LDY #$10                            ; BF1E  a0 10
    JSR sub_30_C176                     ; BF20  20 76 c1
    LDA #$10                            ; BF23  a9 10
    STA obj_anim_s0D                    ; BF25  8d ed 04
    JSR FarCall_B14_800E                ; BF28  20 8c ff
    JSR sub_19_BFC9                     ; BF2B  20 c9 bf
    LDY #$10                            ; BF2E  a0 10
    JSR sub_30_C176                     ; BF30  20 76 c1
    LDA #$68                            ; BF33  a9 68
    STA $7E                             ; BF35  85 7e
    LDA #$C8                            ; BF37  a9 c8
    STA obj_y_lo_s0E                    ; BF39  85 be
    LDA #$00                            ; BF3B  a9 00
    STA $9E                             ; BF3D  85 9e
    STA $DE                             ; BF3F  85 de
    LDX #$0E                            ; BF41  a2 0e
    STX jmp_trampoline                  ; BF43  86 10
    LDA #$C2                            ; BF45  a9 c2
    STA tmp_11                          ; BF47  85 11
    JSR FarCall_C25D                    ; BF49  20 80 ff
    LDA #$2C                            ; BF4C  a9 2c
    STA obj_meta_id_s0E                 ; BF4E  8d 0e 03
    LDA #$09                            ; BF51  a9 09
    STA obj_flags_s0E                   ; BF53  8d 8e 03
loc_19_BF56:
    JSR WaitFrame                       ; BF56  20 69 c1
    DEC obj_anim_s0E                    ; BF59  ce ee 04
    DEC obj_y_lo_s0E                    ; BF5C  c6 be
    JSR FarCall_B14_800E                ; BF5E  20 8c ff
    LDA obj_y_lo_s0E                    ; BF61  a5 be
    CMP #$78                            ; BF63  c9 78
    BNE loc_19_BF56                     ; BF65  d0 ef
    LDA #$2D                            ; BF67  a9 2d
    STA obj_meta_id_s0E                 ; BF69  8d 0e 03
    JSR FarCall_B14_800E                ; BF6C  20 8c ff
    JSR sub_19_BFC9                     ; BF6F  20 c9 bf
    LDY #$10                            ; BF72  a0 10
    JSR sub_30_C176                     ; BF74  20 76 c1
    JSR sub_19_BFC9                     ; BF77  20 c9 bf
    JSR sub_19_BFC9                     ; BF7A  20 c9 bf
    LDA #$2E                            ; BF7D  a9 2e
    STA obj_meta_id_s0E                 ; BF7F  8d 0e 03
    LDA #$02                            ; BF82  a9 02
    STA obj_meta_id_s01                 ; BF84  8d 01 03
    LDA #$0A                            ; BF87  a9 0a
    STA obj_overlay_id_s01              ; BF89  8d 81 05
loc_19_BF8C:
    DEC tmp_08                          ; BF8C  c6 08
    LDA tmp_08                          ; BF8E  a5 08
    AND #$18                            ; BF90  29 18
    BNE loc_19_BFA1                     ; BF92  d0 0d
    LDA #$76                            ; BF94  a9 76
    JSR PlaySound                       ; BF96  20 25 c1
    LDA #$16                            ; BF99  a9 16
    JSR sub_19_BDDC                     ; BF9B  20 dc bd
    JMP loc_19_BFA4                     ; BF9E  4c a4 bf
loc_19_BFA1:
    JSR WaitFrame                       ; BFA1  20 69 c1
loc_19_BFA4:
    JSR FarCall_B14_800E                ; BFA4  20 8c ff
    DEC obj_anim_s01                    ; BFA7  ce e1 04
    DEC obj_anim_s0E                    ; BFAA  ce ee 04
    INC obj_y_lo                        ; BFAD  e6 b0
    INC obj_y_lo_s01                    ; BFAF  e6 b1
    LDA obj_y_lo_s01                    ; BFB1  a5 b1
    AND #$07                            ; BFB3  29 07
    BEQ loc_19_BFB9                     ; BFB5  f0 02
    INC obj_y_lo_s0E                    ; BFB7  e6 be
loc_19_BFB9:
    LDA obj_y_lo_s0E                    ; BFB9  a5 be
    CMP #$E0                            ; BFBB  c9 e0
    BNE loc_19_BF8C                     ; BFBD  d0 cd
    LDA #$20                            ; BFBF  a9 20
    JSR Bank26_FadeA                    ; BFC1  20 3d 8c
FinalSceneToEnding:  ; s63: end of SubstFinalSceneReset: main_state=4 (only writer)
    LDA #$04                            ; BFC4  a9 04
    STA main_state                      ; BFC6  85 40
    RTS                                 ; BFC8  60
sub_19_BFC9:
    LDA tmp_2E                          ; BFC9  a5 2e
    PHA                                 ; BFCB  48
    ASL A                               ; BFCC  0a
    TAY                                 ; BFCD  a8
    LDA EventMsgTable19,Y               ; BFCE  b9 e8 bf
    STA text_group                      ; BFD1  8d df 07
    LDA $BFE9,Y                         ; BFD4  b9 e9 bf
    STA text_msg_id                     ; BFD7  8d de 07
    LDA #$29                            ; BFDA  a9 29
    JSR PlaySound                       ; BFDC  20 25 c1
    JSR FarCall_D354                    ; BFDF  20 cf ff
    PLA                                 ; BFE2  68
    STA tmp_2E                          ; BFE3  85 2e
    INC tmp_2E                          ; BFE5  e6 2e
    RTS                                 ; BFE7  60
EventMsgTable19:  ; 5 x (group msg) indexed by $2E
    .db $20,$1D,$1B,$0F,$1B,$10,$1B,$11,$1B,$12,$00,$81,$81,$00,$00,$00; 19:BFE8
    .db $00,$00,$81,$00,$FF,$FF,$FF,$FF ; 19:BFF8
