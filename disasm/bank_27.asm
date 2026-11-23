; re-c bank 27  file 0x36010  CPU $A000-$BFFF  (tools/disasm_all.py)

ppumask_shadow = $01
tmp_08 = $08
frame_ready = $09
jmp_trampoline = $10
tmp_11 = $11
tmp_12 = $12
tmp_13 = $13
tmp_1F = $1F
tmp_20 = $20
tmp_21 = $21
tmp_23 = $23
tmp_2B = $2B
tmp_2C = $2C
tmp_2D = $2D
tmp_2E = $2E
tmp_2F = $2F
tmp_30 = $30
tmp_31 = $31
pos_x_lo = $34
pos_x_hi = $35
pos_y_lo = $36
pos_y_hi = $37
zp_3C = $3C
zp_3D = $3D
zp_3E = $3E
zp_3F = $3F
sub_state = $41
pad_prev = $43
pad_dir = $49
pad_new = $4B
irq_mode = $51
chr_r0_alt = $58
area_id = $6C
area_entry = $6D
obj_x_lo = $70
obj_x_lo_s01 = $71
obj_x_hi = $90
obj_y_lo = $B0
obj_y_lo_s01 = $B1
obj_y_hi = $D0
obj_meta_id = $0300
obj_meta_id_s01 = $0301
obj_meta_id_s0D = $030D
obj_tile_ofs = $0320
obj_flags2 = $0340
obj_meta_dir = $0360
obj_flags = $0380
obj_flags_s0E = $038E
obj_coll = $03A0
obj_coll_s0D = $03AD
player_hp_max = $03C0
player_hp = $03C1
obj_shot_id = $0440
obj_bg_mask = $0460
obj_timer = $0480
obj_timer_s1F = $049F
obj_active = $04A0
obj_active_s0D = $04AD
obj_active_s0E = $04AE
obj_anim = $04E0
obj_anim_s01 = $04E1
obj_anim_s0D = $04ED
obj_anim_s0E = $04EE
obj_elem_drop = $0500
ride_platform_slot = $0541
obj_spawn_param = $0560
obj_spawn_param_s01 = $0561
obj_overlay_id = $0580
obj_overlay_id_s01 = $0581
obj_freeze = $05A0
obj_screen_x = $05C0
obj_screen_y = $05E0
obj_phase = $0600
obj_phase_s03 = $0603
obj_timer2 = $0620
obj_timer2_s03 = $0623
obj_timer2_s0D = $062D
obj_timer2_s0E = $062E
obj_act_timer = $0640
player_bg_attr = $0641
obj_aux = $0660
obj_param1 = $0680
obj_param1_s0D = $068D
obj_param1_s0E = $068E
obj_param2 = $06A0
orbit_level = $06C0
pending_msg_id = $06C3
ram_06CD = $06CD
obj_behav = $06E0
pending_msg_group = $06E3
mp_cur = $0708
mp_max = $0709
obj1_flash = $0710
magic_sel = $0712
equip_accessory = $0716
ram_07D5 = $07D5
ram_07D6 = $07D6
boss_cam_lock = $07D7
text_msg_id = $07DE
text_group = $07DF
pal_ids = $07E0
pal_id_spr3 = $07E7
pal_nofade_mask = $07E8
chr_r0_play = $07F0
chr_r5_play = $07F5
wram_buf = $6000
wram_6001 = $6001
wram_6060 = $6060
attr_shadow = $6100
visited_map = $62F0
inventory = $6430
story_flags = $6480
story_flags_F7F8 = $657F
DirToTarget = $8409
ObjStepVector = $8480
PaletteBuild = $8C0E
Bank26_FadeB = $8C49
HudHpBar = $8CC0
HudRedraw = $8E46
SpawnShot = $972D
Rand64 = $97D7
TempAddOffset = $9824
TempAddStep = $9861
TempToObjPos = $9897
ObjPosToTemp = $98A8
ObjDespawn = $98C7
ObjBecomeNext = $98CD
ObjMoveSlide = $98D7
ObjBgProbe = $9A30
SpendMp = $9AE8
sub_26_9B07 = $9B07
PlayerUpdate = $9B45
PlayerMoveLocked = $9D81
PlayerMove = $9D9A
BossAiE0 = $A477
sub_15_A5EE = $A5EE
BossAiE2 = $A675
BossAiE3 = $A95D
sub_15_AB3E = $AB3E
AiTalkEventE5 = $ABDA
BossAiE6 = $AC7B
AiBoss88 = $ADD4
BossAiE8 = $AF48
BossAiE9 = $B0B6
BossAiEA = $B130
AiBowOfTruth9E = $B2DA
sub_15_B728 = $B728
BossDefeated = $B807
BitMask = $C000
PlaySound = $C125
WaitFrame = $C169
SetR6 = $C418
SetR7 = $C427
PpuQueueBlock = $C482
WaitPpuQueue = $C676
sub_30_C739 = $C739
MarkScreenVisited = $C75C
sub_30_C831 = $C831
FarCall_C25D = $FF80
FarCall_B14_800E = $FF8C
FarCall_EB91 = $FF9D
FarCall_D354 = $FFCF

    .org $A000
    .db $80,$06                         ; 27:A000
loc_27_A002:
    PLA                                 ; A002  68
    STA obj_timer,X                     ; A003  9d 80 04
    PLA                                 ; A006  68
    STA obj_flags2,X                    ; A007  9d 40 03
    PLA                                 ; A00A  68
    STA obj_meta_dir,X                  ; A00B  9d 60 03
    RTS                                 ; A00E  60
    .db $04,$04,$04,$05,$04,$06,$04,$04 ; 27:A00F
DiagToCardinal:  ; s44: u8[8] FF 02 02 02 FF 06 06 06 (forced move: up/down ignored)
    .db $FF,$02,$02,$02,$FF,$06,$06,$06 ; 27:A017
loc_27_A01F:
    JMP MagicFailSound                  ; A01F  4c bb a1
PlayerMagic:  ; s44: A button; Y=$0712 selected magic; dispatch JmpTable27_A072 (see REVERSE Player section)
    LDY magic_sel                       ; A022  ac 12 07
    BNE loc_27_A030                     ; A025  d0 09
    LDA equip_accessory                 ; A027  ad 16 07
    CMP #$12                            ; A02A  c9 12
    BNE loc_27_A030                     ; A02C  d0 02
    LDY #$09                            ; A02E  a0 09
loc_27_A030:
    STY jmp_trampoline                  ; A030  84 10
    LDA MagicButtonSel,Y                ; A032  b9 92 a0
    TAY                                 ; A035  a8
    LDA a:pad_prev,Y                    ; A036  b9 43 00
    BMI loc_27_A03C                     ; A039  30 01
    RTS                                 ; A03B  60
loc_27_A03C:
    LDA obj1_flash                      ; A03C  ad 10 07
    AND #$0F                            ; A03F  29 0f
    CMP #$04                            ; A041  c9 04
    BEQ loc_27_A01F                     ; A043  f0 da
    LDA jmp_trampoline                  ; A045  a5 10
    CMP #$08                            ; A047  c9 08
    BEQ loc_27_A054                     ; A049  f0 09
    CMP #$09                            ; A04B  c9 09
    BEQ loc_27_A054                     ; A04D  f0 05
    LDA obj_timer2                      ; A04F  ad 20 06
    BNE loc_27_A01F                     ; A052  d0 cb
loc_27_A054:
    LDA #$19                            ; A054  a9 19
    LDY obj1_flash                      ; A056  ac 10 07
    BPL loc_27_A05E                     ; A059  10 03
    JMP loc_27_A1AD                     ; A05B  4c ad a1
loc_27_A05E:
    LDA jmp_trampoline                  ; A05E  a5 10
    AND #$0F                            ; A060  29 0f
    ASL A                               ; A062  0a
    TAY                                 ; A063  a8
    LDA JmpTable27_A072,Y               ; A064  b9 72 a0
    STA jmp_trampoline                  ; A067  85 10
    LDA $A073,Y                         ; A069  b9 73 a0
    STA tmp_11                          ; A06C  85 11
    JMP (jmp_trampoline)                ; A06E  6c 10 00
    .db $EA                             ; 27:A071
JmpTable27_A072:  ; s67: magic cast handlers [$10&$0F = $0712 or 9 with accessory $12]: 16 entries A0A2 A0A3 A0D7 A18E A1A1 A17D A0F4 A161 MagicFlight MagicJump + 6x A0A2 (RTS) (JUMP_TABLES)
    .dw sub_27_A0A2                     ; 27:A072
    .dw sub_27_A0A3                     ; 27:A074
    .dw sub_27_A0D7                     ; 27:A076
    .dw sub_27_A18E                     ; 27:A078
    .dw sub_27_A1A1                     ; 27:A07A
    .dw sub_27_A17D                     ; 27:A07C
    .dw sub_27_A0F4                     ; 27:A07E
    .dw sub_27_A161                     ; 27:A080
    .dw MagicFlight                     ; 27:A082
    .dw MagicJump                       ; 27:A084
    .dw sub_27_A0A2                     ; 27:A086
    .dw sub_27_A0A2                     ; 27:A088
    .dw sub_27_A0A2                     ; 27:A08A
    .dw sub_27_A0A2                     ; 27:A08C
    .dw sub_27_A0A2                     ; 27:A08E
    .dw sub_27_A0A2                     ; 27:A090
MagicButtonSel:  ; s44: u8[12] per magic: $08 = pad_new edge; $00 = pad_prev held (offset from $43)
    .db $08,$00,$08,$08,$08,$08,$00,$08,$00,$08,$00,$00,$00,$00,$00,$00; 27:A092
sub_27_A0A2:
    RTS                                 ; A0A2  60
sub_27_A0A3:
    JSR sub_27_A1B0                     ; A0A3  20 b0 a1
    LDA mp_cur                          ; A0A6  ad 08 07
    CMP #$02                            ; A0A9  c9 02
    BCS loc_27_A0B0                     ; A0AB  b0 03
    JMP loc_27_A15C                     ; A0AD  4c 5c a1
loc_27_A0B0:
    JSR sub_27_A0B3                     ; A0B0  20 b3 a0
sub_27_A0B3:
    LDA player_hp                       ; A0B3  ad c1 03
    CMP player_hp_max,X                 ; A0B6  dd c0 03
    BEQ loc_27_A0D6                     ; A0B9  f0 1b
    CLC                                 ; A0BB  18
    ADC #$01                            ; A0BC  69 01
    STA player_hp                       ; A0BE  8d c1 03
    CMP player_hp_max,X                 ; A0C1  dd c0 03
    BEQ loc_27_A0CA                     ; A0C4  f0 04
    AND #$03                            ; A0C6  29 03
    BNE loc_27_A0D6                     ; A0C8  d0 0c
loc_27_A0CA:
    JSR HudHpBar                        ; A0CA  20 c0 8c
    LDA #$02                            ; A0CD  a9 02
    JSR SpendMp                         ; A0CF  20 e8 9a
    LDA #$12                            ; A0D2  a9 12
    BNE loc_27_A0E6                     ; A0D4  d0 10
loc_27_A0D6:
    RTS                                 ; A0D6  60
sub_27_A0D7:
    JSR sub_27_A1B0                     ; A0D7  20 b0 a1
    LDA $04AC                           ; A0DA  ad ac 04
    BNE loc_27_A0D6                     ; A0DD  d0 f7
    LDA #$04                            ; A0DF  a9 04
    JSR SpendMp                         ; A0E1  20 e8 9a
    LDA #$10                            ; A0E4  a9 10
loc_27_A0E6:
    STA jmp_trampoline                  ; A0E6  85 10
    LDA obj_meta_id_s01                 ; A0E8  ad 01 03
    AND #$03                            ; A0EB  29 03
    ASL A                               ; A0ED  0a
    TAY                                 ; A0EE  a8
    LDA jmp_trampoline                  ; A0EF  a5 10
    JMP SpawnShot                       ; A0F1  4c 2d 97
sub_27_A0F4:
    LDA obj_timer2                      ; A0F4  ad 20 06
    BEQ loc_27_A0FC                     ; A0F7  f0 03
    JMP MagicFailSound                  ; A0F9  4c bb a1
loc_27_A0FC:
    LDA mp_cur                          ; A0FC  ad 08 07
    BEQ loc_27_A15C                     ; A0FF  f0 5b
    LDA tmp_08                          ; A101  a5 08
    AND #$07                            ; A103  29 07
    BNE loc_27_A10C                     ; A105  d0 05
    LDA #$01                            ; A107  a9 01
    JSR SpendMp                         ; A109  20 e8 9a
loc_27_A10C:
    LDA #$11                            ; A10C  a9 11
    STA jmp_trampoline                  ; A10E  85 10
    BNE loc_27_A0E6                     ; A110  d0 d4
MagicFlight:  ; s44: magic 8: jump + hover at the peak while A held; SpendMp(1)/16 f
    BIT obj1_flash                      ; A112  2c 10 07
    BVS loc_27_A142                     ; A115  70 2b
    LDA tmp_08                          ; A117  a5 08
    AND #$0F                            ; A119  29 0f
    BNE loc_27_A122                     ; A11B  d0 05
    LDA #$01                            ; A11D  a9 01
    JSR SpendMp                         ; A11F  20 e8 9a
loc_27_A122:
    LDA obj_timer2,X                    ; A122  bd 20 06
    BNE loc_27_A133                     ; A125  d0 0c
    LDA mp_cur                          ; A127  ad 08 07
    BEQ loc_27_A15C                     ; A12A  f0 30
    LDA #$34                            ; A12C  a9 34
    STA obj_bg_mask,X                   ; A12E  9d 60 04
    BNE loc_27_A14D                     ; A131  d0 1a
loc_27_A133:
    LDA obj_timer2,X                    ; A133  bd 20 06
    CMP #$12                            ; A136  c9 12
    BNE loc_27_A13D                     ; A138  d0 03
    INC obj_timer2,X                    ; A13A  fe 20 06
loc_27_A13D:
    LDA pad_dir                         ; A13D  a5 49
    STA obj_act_timer,X                 ; A13F  9d 40 06
loc_27_A142:
    RTS                                 ; A142  60
MagicJump:  ; s44: magic 9: ground jump; $0620=$20; sound $30; verified s45
    BIT obj1_flash                      ; A143  2c 10 07
    BVS loc_27_A142                     ; A146  70 fa
    LDA obj_timer2,X                    ; A148  bd 20 06
    BNE loc_27_A142                     ; A14B  d0 f5
loc_27_A14D:
    LDA #$20                            ; A14D  a9 20
    STA obj_timer2,X                    ; A14F  9d 20 06
    LDA pad_dir                         ; A152  a5 49
    STA obj_act_timer,X                 ; A154  9d 40 06
    LDA #$30                            ; A157  a9 30
    JMP PlaySound                       ; A159  4c 25 c1
loc_27_A15C:
    LDA #$1C                            ; A15C  a9 1c
    STA sub_state                       ; A15E  85 41
    RTS                                 ; A160  60
sub_27_A161:
    LDA area_id                         ; A161  a5 6c
    AND #$F8                            ; A163  29 f8
    CMP #$58                            ; A165  c9 58
    BEQ MagicFailSound                  ; A167  f0 52
    JSR sub_27_A1B0                     ; A169  20 b0 a1
    LDA obj_timer2,X                    ; A16C  bd 20 06
    BEQ loc_27_A172                     ; A16F  f0 01
    RTS                                 ; A171  60
loc_27_A172:
    LDA mp_cur                          ; A172  ad 08 07
    CMP #$14                            ; A175  c9 14
    BCC loc_27_A15C                     ; A177  90 e3
    LDA #$17                            ; A179  a9 17
    BNE loc_27_A1AD                     ; A17B  d0 30
sub_27_A17D:
    LDA obj1_flash                      ; A17D  ad 10 07
    AND #$0F                            ; A180  29 0f
    BEQ loc_27_A18D                     ; A182  f0 09
    LDA #$18                            ; A184  a9 18
    JSR SpendMp                         ; A186  20 e8 9a
    LDA #$18                            ; A189  a9 18
    BNE loc_27_A1AD                     ; A18B  d0 20
loc_27_A18D:
    RTS                                 ; A18D  60
sub_27_A18E:
    LDA area_id                         ; A18E  a5 6c
    AND #$F8                            ; A190  29 f8
    CMP #$58                            ; A192  c9 58
    BEQ MagicFailSound                  ; A194  f0 25
    LDA mp_cur                          ; A196  ad 08 07
    CMP #$08                            ; A199  c9 08
    BCC loc_27_A15C                     ; A19B  90 bf
    LDA #$15                            ; A19D  a9 15
    BNE loc_27_A1AD                     ; A19F  d0 0c
sub_27_A1A1:
    JSR sub_27_A1B0                     ; A1A1  20 b0 a1
    LDA mp_cur                          ; A1A4  ad 08 07
    CMP #$14                            ; A1A7  c9 14
    BCC loc_27_A15C                     ; A1A9  90 b1
    LDA #$14                            ; A1AB  a9 14
loc_27_A1AD:
    STA sub_state                       ; A1AD  85 41
    RTS                                 ; A1AF  60
sub_27_A1B0:
    LDA obj1_flash                      ; A1B0  ad 10 07
    AND #$0F                            ; A1B3  29 0f
    CMP #$04                            ; A1B5  c9 04
    BNE loc_27_A1C0                     ; A1B7  d0 07
    PLA                                 ; A1B9  68
    PLA                                 ; A1BA  68
MagicFailSound:  ; s44: PlaySound($4E)
    LDA #$4E                            ; A1BB  a9 4e
    JMP PlaySound                       ; A1BD  4c 25 c1
loc_27_A1C0:
    RTS                                 ; A1C0  60
    .db $8A,$48,$20,$8C,$FF,$68,$AA,$60 ; 27:A1C1
PlayerFacing:  ; s44: slot 1 meta low 2 bits = facing; FacingKeep keeps facing within 45 deg
    LDA area_id                         ; A1C9  a5 6c
    CMP #$5F                            ; A1CB  c9 5f
    BNE loc_27_A1DD                     ; A1CD  d0 0e
    LDA obj_phase                       ; A1CF  ad 00 06
    BEQ loc_27_A1DD                     ; A1D2  f0 09
    LDA obj_meta_id_s01                 ; A1D4  ad 01 03
    AND #$FC                            ; A1D7  29 fc
    STA obj_meta_id_s01                 ; A1D9  8d 01 03
    RTS                                 ; A1DC  60
loc_27_A1DD:
    LDA obj_behav                       ; A1DD  ad e0 06
    CMP #$FF                            ; A1E0  c9 ff
    BEQ loc_27_A1F9                     ; A1E2  f0 15
    AND #$07                            ; A1E4  29 07
    EOR #$FF                            ; A1E6  49 ff
    CLC                                 ; A1E8  18
    ADC #$01                            ; A1E9  69 01
    CLC                                 ; A1EB  18
    ADC obj_meta_id,X                   ; A1EC  7d 00 03
    CLC                                 ; A1EF  18
    ADC obj_meta_id,X                   ; A1F0  7d 00 03
    AND #$07                            ; A1F3  29 07
    TAY                                 ; A1F5  a8
    LDA FacingKeep,Y                    ; A1F6  b9 11 a2
loc_27_A1F9:
    BNE loc_27_A210                     ; A1F9  d0 15
    LDA obj_meta_dir                    ; A1FB  ad 60 03
    STA obj_meta_dir,X                  ; A1FE  9d 60 03
    LSR A                               ; A201  4a
    AND #$03                            ; A202  29 03
    STA jmp_trampoline                  ; A204  85 10
    LDA #$FC                            ; A206  a9 fc
    AND obj_meta_id,X                   ; A208  3d 00 03
    ORA jmp_trampoline                  ; A20B  05 10
    STA obj_meta_id,X                   ; A20D  9d 00 03
loc_27_A210:
    RTS                                 ; A210  60
FacingKeep:  ; s44: u8[8] 01 01 00 00 00 00 00 01 indexed (2*facing - dir)&7
    .db $01,$01,$00,$00,$00,$00,$00,$01 ; 27:A211
sub_27_A219:
    LDA obj_phase_s03                   ; A219  ad 03 06
    BEQ loc_27_A221                     ; A21C  f0 03
    DEC obj_phase_s03                   ; A21E  ce 03 06
loc_27_A221:
    STX jmp_trampoline                  ; A221  86 10
    LDX #$00                            ; A223  a2 00
    JSR ObjPosToTemp                    ; A225  20 a8 98
    LDX jmp_trampoline                  ; A228  a6 10
    LDA obj_meta_id_s01                 ; A22A  ad 01 03
    AND #$03                            ; A22D  29 03
    ASL A                               ; A22F  0a
    TAY                                 ; A230  a8
    LDA $A242,Y                         ; A231  b9 42 a2
    STA tmp_30                          ; A234  85 30
    LDA $A243,Y                         ; A236  b9 43 a2
    STA tmp_31                          ; A239  85 31
    JSR TempAddOffset                   ; A23B  20 24 98
    JMP TempToObjPos                    ; A23E  4c 97 98
    .db $60,$00,$FC,$04,$00,$00,$04,$FC,$00; 27:A241
sub_27_A24A:
    JSR sub_27_A2B4                     ; A24A  20 b4 a2
    LDA #$00                            ; A24D  a9 00
    STA obj_timer2,X                    ; A24F  9d 20 06
    STA orbit_level,X                   ; A252  9d c0 06
    LDA #$A7                            ; A255  a9 a7
    STA obj_meta_id,X                   ; A257  9d 00 03
    LDA #$00                            ; A25A  a9 00
    STA obj_meta_dir,X                  ; A25C  9d 60 03
    LDA pad_new                         ; A25F  a5 4b
    AND #$40                            ; A261  29 40
    BEQ loc_27_A275                     ; A263  f0 10
    LDA obj_phase,X                     ; A265  bd 00 06
    CMP #$0C                            ; A268  c9 0c
    BCC loc_27_A270                     ; A26A  90 04
    CMP #$11                            ; A26C  c9 11
    BNE loc_27_A275                     ; A26E  d0 05
loc_27_A270:
    LDA #$12                            ; A270  a9 12
    STA obj_phase,X                     ; A272  9d 00 06
loc_27_A275:
    LDA #$00                            ; A275  a9 00
    STA obj_meta_id_s01                 ; A277  8d 01 03
    LDA obj_phase,X                     ; A27A  bd 00 06
    BEQ loc_27_A28B                     ; A27D  f0 0c
    STA obj_anim_s01                    ; A27F  8d e1 04
    LDA #$04                            ; A282  a9 04
    STA obj_meta_id_s01                 ; A284  8d 01 03
    DEC obj_phase,X                     ; A287  de 00 06
    RTS                                 ; A28A  60
loc_27_A28B:
    LDY pad_dir                         ; A28B  a4 49
    BMI loc_27_A2AB                     ; A28D  30 1c
    LDA $A2AC,Y                         ; A28F  b9 ac a2
    CLC                                 ; A292  18
    ADC obj_x_lo,X                      ; A293  75 70
    STA obj_x_lo,X                      ; A295  95 70
    LDY obj_x_lo,X                      ; A297  b4 70
    CPY #$27                            ; A299  c0 27
    BCS loc_27_A29F                     ; A29B  b0 02
    INY                                 ; A29D  c8
    INY                                 ; A29E  c8
loc_27_A29F:
    CPY #$98                            ; A29F  c0 98
    BCC loc_27_A2A5                     ; A2A1  90 02
    DEY                                 ; A2A3  88
    DEY                                 ; A2A4  88
loc_27_A2A5:
    TYA                                 ; A2A5  98
    STA obj_x_lo,X                      ; A2A6  95 70
    DEC obj_anim_s01                    ; A2A8  ce e1 04
loc_27_A2AB:
    RTS                                 ; A2AB  60
    .db $00,$02,$02,$02,$00,$FE,$FE,$FE ; 27:A2AC
sub_27_A2B4:
    LDA obj_y_lo,X                      ; A2B4  b5 b0
    CMP #$98                            ; A2B6  c9 98
    BCS loc_27_A2E8                     ; A2B8  b0 2e
    CMP #$58                            ; A2BA  c9 58
    BCC loc_27_A2BF                     ; A2BC  90 01
    RTS                                 ; A2BE  60
loc_27_A2BF:
    LDA obj_flags_s0E                   ; A2BF  ad 8e 03
    ORA #$04                            ; A2C2  09 04
    STA obj_flags_s0E                   ; A2C4  8d 8e 03
    LDA #$53                            ; A2C7  a9 53
    STA obj_active_s0E                  ; A2C9  8d ae 04
    LDA #$01                            ; A2CC  a9 01
    STA pending_msg_id                  ; A2CE  8d c3 06
    LDA #$03                            ; A2D1  a9 03
    STA pending_msg_group               ; A2D3  8d e3 06
    LDA #$80                            ; A2D6  a9 80
    STA obj_coll_s0D                    ; A2D8  8d ad 03
    LDA #$05                            ; A2DB  a9 05
    STA $06ED                           ; A2DD  8d ed 06
    LDA #$57                            ; A2E0  a9 57
    STA ram_06CD                        ; A2E2  8d cd 06
    JMP loc_27_A2FC                     ; A2E5  4c fc a2
loc_27_A2E8:
    LDA #$02                            ; A2E8  a9 02
    STA pending_msg_id                  ; A2EA  8d c3 06
    LDA #$03                            ; A2ED  a9 03
    STA pending_msg_group               ; A2EF  8d e3 06
    LDA #$4D                            ; A2F2  a9 4d
    STA chr_r5_play                     ; A2F4  8d f5 07
    LDA #$80                            ; A2F7  a9 80
    STA obj_coll_s0D                    ; A2F9  8d ad 03
loc_27_A2FC:
    LDA #$10                            ; A2FC  a9 10
    STA sub_state                       ; A2FE  85 41
    LDA ram_06CD                        ; A300  ad cd 06
    STA obj_meta_id_s0D                 ; A303  8d 0d 03
    LDA #$03                            ; A306  a9 03
    STA obj_active                      ; A308  8d a0 04
    LDA #$30                            ; A30B  a9 30
    STA obj_active_s0D                  ; A30D  8d ad 04
    JSR FarCall_B14_800E                ; A310  20 8c ff
    RTS                                 ; A313  60
ObjAiTable:  ; ptr16[128] object AI handlers indexed by obj_active&$7F (0 = none); ids $60-$6F run in bank 15 (SetR7($0F))
    .dw $0000                           ; 27:A314
    .dw PlayerUpdate                    ; 27:A316
    .dw PlayerMoveLocked                ; 27:A318
    .dw PlayerMove                      ; 27:A31A
    .dw sub_27_B2FC                     ; 27:A31C
    .dw sub_27_A219                     ; 27:A31E
    .dw sub_27_B93E                     ; 27:A320
    .dw sub_27_A85F                     ; 27:A322
    .dw sub_27_B7F6                     ; 27:A324
    .dw $0000                           ; 27:A326
    .dw sub_27_A70E                     ; 27:A328
    .dw sub_27_A24A                     ; 27:A32A
    .dw sub_27_A5CB                     ; 27:A32C
    .dw sub_27_A5ED                     ; 27:A32E
    .dw AiShopEmpty                     ; 27:A330
    .dw AiSetSubstate                   ; 27:A332
    .dw AiStraightMover                 ; 27:A334
    .dw AiDirShot                       ; 27:A336
    .dw sub_27_B174                     ; 27:A338
    .dw sub_27_B235                     ; 27:A33A
    .dw AiCountdown                     ; 27:A33C
    .dw sub_27_B27A                     ; 27:A33E
    .dw sub_27_B0F6                     ; 27:A340
    .dw sub_27_B11B                     ; 27:A342
    .dw sub_27_B2D5                     ; 27:A344
    .dw sub_27_B2E2                     ; 27:A346
    .dw sub_27_B2F4                     ; 27:A348
    .dw sub_27_B12D                     ; 27:A34A
    .dw sub_27_B2A4                     ; 27:A34C
    .dw sub_27_B02C                     ; 27:A34E
    .dw sub_27_B094                     ; 27:A350
    .dw sub_27_B0A2                     ; 27:A352
    .dw AiWalkPause                     ; 27:A354
    .dw AiShooterWalk                   ; 27:A356
    .dw AiDormant                       ; 27:A358
    .dw $0000                           ; 27:A35A
    .dw AiChase                         ; 27:A35C
    .dw AiChaseOnscreen                 ; 27:A35E
    .dw AiShooterChase                  ; 27:A360
    .dw AiShooterChaseOnscreen          ; 27:A362
    .dw AiAimShooter                    ; 27:A364
    .dw AiBurrower                      ; 27:A366
    .dw AiLunger                        ; 27:A368
    .dw AI_TrapChest                    ; 27:A36A
    .dw AiBurst                         ; 27:A36C
    .dw $0000                           ; 27:A36E
    .dw AiSpawnChildLunger              ; 27:A370
    .dw sub_27_B048                     ; 27:A372
    .dw AiNpcWander                     ; 27:A374
    .dw sub_27_AE16                     ; 27:A376
    .dw AiPathWalk                      ; 27:A378
    .dw sub_27_A4C6                     ; 27:A37A
    .dw sub_27_B318                     ; 27:A37C
    .dw $0000                           ; 27:A37E
    .dw sub_27_B070                     ; 27:A380
    .dw $0000                           ; 27:A382
    .dw sub_27_AFA9                     ; 27:A384
    .dw sub_27_A519                     ; 27:A386
    .dw sub_27_A555                     ; 27:A388
    .dw $0000                           ; 27:A38A
    .dw sub_27_AFF5                     ; 27:A38C
    .dw $0000                           ; 27:A38E
    .dw $0000                           ; 27:A390
    .dw sub_27_A414                     ; 27:A392
    .dw AiWander16                      ; 27:A394
    .dw AiShellOnHit                    ; 27:A396
    .dw $0000                           ; 27:A398
    .dw $0000                           ; 27:A39A
    .dw AiDasher                        ; 27:A39C
    .dw AiHomingFlyer                   ; 27:A39E
    .dw $0000                           ; 27:A3A0
    .dw $0000                           ; 27:A3A2
    .dw sub_27_B55A                     ; 27:A3A4
    .dw $0000                           ; 27:A3A6
    .dw $0000                           ; 27:A3A8
    .dw $0000                           ; 27:A3AA
    .dw AiFanTurret                     ; 27:A3AC
    .dw AiFanTurret                     ; 27:A3AE
    .dw AiTurret                        ; 27:A3B0
    .dw $0000                           ; 27:A3B2
    .dw sub_27_A660                     ; 27:A3B4
    .dw sub_27_A6C5                     ; 27:A3B6
    .dw sub_27_A753                     ; 27:A3B8
    .dw sub_27_A7CD                     ; 27:A3BA
    .dw sub_27_A802                     ; 27:A3BC
    .dw sub_27_A616                     ; 27:A3BE
    .dw $0000                           ; 27:A3C0
    .dw sub_27_B0BE                     ; 27:A3C2
    .dw AiOrbiter                       ; 27:A3C4
    .dw $0000                           ; 27:A3C6
    .dw $0000                           ; 27:A3C8
    .dw $0000                           ; 27:A3CA
    .dw AiPatrolGunner                  ; 27:A3CC
    .dw AiDrifter                       ; 27:A3CE
    .dw AiWaitClear                     ; 27:A3D0
    .dw sub_27_A41A                     ; 27:A3D2
    .dw BossAiE0                        ; 27:A3D4
    .dw sub_15_A5EE                     ; 27:A3D6
    .dw BossAiE2                        ; 27:A3D8
    .dw BossAiE3                        ; 27:A3DA
    .dw sub_15_AB3E                     ; 27:A3DC
    .dw AiTalkEventE5                   ; 27:A3DE
    .dw BossAiE6                        ; 27:A3E0
    .dw AiBoss88                        ; 27:A3E2
    .dw BossAiE8                        ; 27:A3E4
    .dw BossAiE9                        ; 27:A3E6
    .dw BossAiEA                        ; 27:A3E8
    .dw AiBowOfTruth9E                  ; 27:A3EA
    .dw sub_15_B728                     ; 27:A3EC
    .dw sub_15_B728                     ; 27:A3EE
    .dw sub_15_B728                     ; 27:A3F0
    .dw BossDefeated                    ; 27:A3F2
    .dw Ai70PhaseDispatch               ; 27:A3F4
    .dw $0000                           ; 27:A3F6
    .dw $0000                           ; 27:A3F8
    .dw $0000                           ; 27:A3FA
    .dw ObjAiExplode                    ; 27:A3FC
    .dw $0000                           ; 27:A3FE
    .dw sub_27_A4DC                     ; 27:A400
    .dw sub_27_A4E0                     ; 27:A402
    .dw sub_27_A914                     ; 27:A404
    .dw sub_27_A90C                     ; 27:A406
    .dw $0000                           ; 27:A408
    .dw sub_27_B778                     ; 27:A40A
    .dw sub_27_B7F6                     ; 27:A40C
    .dw sub_27_B9AA                     ; 27:A40E
    .dw $0000                           ; 27:A410
    .dw sub_27_A419                     ; 27:A412
sub_27_A414:
    LDA #$07                            ; A414  a9 07
    STA irq_mode                        ; A416  85 51
    RTS                                 ; A418  60
sub_27_A419:
    RTS                                 ; A419  60
sub_27_A41A:
    LDA obj_phase,X                     ; A41A  bd 00 06
    BNE loc_27_A422                     ; A41D  d0 03
    JSR sub_27_A48C                     ; A41F  20 8c a4
loc_27_A422:
    LDY #$0D                            ; A422  a0 0d
loc_27_A424:
    LDA a:obj_y_hi,Y                    ; A424  b9 d0 00
    CMP obj_y_hi                        ; A427  c5 d0
    BNE loc_27_A43B                     ; A429  d0 10
    LDA obj_active,Y                    ; A42B  b9 a0 04
    BEQ loc_27_A43B                     ; A42E  f0 0b
    LDA obj_spawn_param,Y               ; A430  b9 60 05
    CMP #$51                            ; A433  c9 51
    BEQ loc_27_A48B                     ; A435  f0 54
    CMP #$50                            ; A437  c9 50
    BEQ loc_27_A48B                     ; A439  f0 50
loc_27_A43B:
    INY                                 ; A43B  c8
    CPY #$1F                            ; A43C  c0 1f
    BNE loc_27_A424                     ; A43E  d0 e4
    LDA #$FF                            ; A440  a9 ff
    STA visited_map                     ; A442  8d f0 62
    STA $62F1                           ; A445  8d f1 62
    STA $62F2                           ; A448  8d f2 62
    STA $62F3                           ; A44B  8d f3 62
    STA $62F4                           ; A44E  8d f4 62
    STA $62F5                           ; A451  8d f5 62
    LDA #$5D                            ; A454  a9 5d
    JSR PlaySound                       ; A456  20 25 c1
sub_27_A459:
    TXA                                 ; A459  8a
    PHA                                 ; A45A  48
    LDA #$00                            ; A45B  a9 00
    STA obj_active,X                    ; A45D  9d a0 04
    LDA #$FF                            ; A460  a9 ff
    STA visited_map                     ; A462  8d f0 62
    LDA zp_3C                           ; A465  a5 3c
    STA pos_x_lo                        ; A467  85 34
    LDA zp_3D                           ; A469  a5 3d
    STA pos_x_hi                        ; A46B  85 35
    LDA zp_3E                           ; A46D  a5 3e
    STA pos_y_lo                        ; A46F  85 36
    LDA zp_3F                           ; A471  a5 3f
    STA pos_y_hi                        ; A473  85 37
    LDA ppumask_shadow                  ; A475  a5 01
    AND #$E7                            ; A477  29 e7
    STA ppumask_shadow                  ; A479  85 01
    JSR WaitFrame                       ; A47B  20 69 c1
    LDA #$01                            ; A47E  a9 01
    JSR sub_30_C831                     ; A480  20 31 c8
    LDA ppumask_shadow                  ; A483  a5 01
    ORA #$18                            ; A485  09 18
    STA ppumask_shadow                  ; A487  85 01
    PLA                                 ; A489  68
    TAX                                 ; A48A  aa
loc_27_A48B:
    RTS                                 ; A48B  60
sub_27_A48C:
    INC obj_phase,X                     ; A48C  fe 00 06
    LDA area_id                         ; A48F  a5 6c
    SEC                                 ; A491  38
    SBC #$5D                            ; A492  e9 5d
    EOR #$FF                            ; A494  49 ff
    CLC                                 ; A496  18
    ADC #$01                            ; A497  69 01
    TAY                                 ; A499  a8
    LDA area_id                         ; A49A  a5 6c
    CMP #$5C                            ; A49C  c9 5c
    BNE loc_27_A4B2                     ; A49E  d0 12
    LDA inventory                       ; A4A0  ad 30 64
    CMP #$04                            ; A4A3  c9 04
    BNE loc_27_A4AA                     ; A4A5  d0 03
    JSR sub_27_A459                     ; A4A7  20 59 a4
loc_27_A4AA:
    LDA #$00                            ; A4AA  a9 00
    STA obj_active,X                    ; A4AC  9d a0 04
    PLA                                 ; A4AF  68
    PLA                                 ; A4B0  68
    RTS                                 ; A4B1  60
loc_27_A4B2:
    LDA obj_y_lo                        ; A4B2  a5 b0
    CMP #$10                            ; A4B4  c9 10
    BCS loc_27_A4B9                     ; A4B6  b0 01
    DEY                                 ; A4B8  88
loc_27_A4B9:
    CPY #$04                            ; A4B9  c0 04
    BEQ loc_27_A4C5                     ; A4BB  f0 08
    LDA #$FF                            ; A4BD  a9 ff
    STA visited_map,Y                   ; A4BF  99 f0 62
    INY                                 ; A4C2  c8
    BNE loc_27_A4B9                     ; A4C3  d0 f4
loc_27_A4C5:
    RTS                                 ; A4C5  60
sub_27_A4C6:
    INC obj_phase,X                     ; A4C6  fe 00 06
    LDA obj_phase,X                     ; A4C9  bd 00 06
    CMP #$40                            ; A4CC  c9 40
    BNE loc_27_A4DB                     ; A4CE  d0 0b
    INC obj_timer2,X                    ; A4D0  fe 20 06
    LDA obj_flags,X                     ; A4D3  bd 80 03
    AND #$FB                            ; A4D6  29 fb
    STA obj_flags,X                     ; A4D8  9d 80 03
loc_27_A4DB:
    RTS                                 ; A4DB  60
sub_27_A4DC:
    LDA #$A3                            ; A4DC  a9 a3
    BNE loc_27_A4E2                     ; A4DE  d0 02
sub_27_A4E0:
    LDA #$99                            ; A4E0  a9 99
loc_27_A4E2:
    STA obj_overlay_id_s01              ; A4E2  8d 81 05
    LDA tmp_08                          ; A4E5  a5 08
    AND #$0F                            ; A4E7  29 0f
    BNE loc_27_A502                     ; A4E9  d0 17
    LDA #$31                            ; A4EB  a9 31
    JSR PlaySound                       ; A4ED  20 25 c1
    LDA #$02                            ; A4F0  a9 02
    STA obj_freeze                      ; A4F2  8d a0 05
    LDA mp_cur                          ; A4F5  ad 08 07
    BEQ loc_27_A502                     ; A4F8  f0 08
    DEC mp_cur                          ; A4FA  ce 08 07
    LDA #$04                            ; A4FD  a9 04
    JSR HudRedraw                       ; A4FF  20 46 8e
loc_27_A502:
    LDA #$FF                            ; A502  a9 ff
    LDY pad_new                         ; A504  a4 4b
    BEQ loc_27_A50A                     ; A506  f0 02
    LDA #$FD                            ; A508  a9 fd
loc_27_A50A:
    CLC                                 ; A50A  18
    ADC obj_anim,X                      ; A50B  7d e0 04
    STA obj_anim,X                      ; A50E  9d e0 04
    BCS loc_27_A518                     ; A511  b0 05
    LDA #$00                            ; A513  a9 00
    STA obj_active,X                    ; A515  9d a0 04
loc_27_A518:
    RTS                                 ; A518  60
sub_27_A519:
    LDA obj_timer_s1F,X                 ; A519  bd 9f 04
    BNE loc_27_A525                     ; A51C  d0 07
    STA obj_active,X                    ; A51E  9d a0 04
    STA boss_cam_lock                   ; A521  8d d7 07
    RTS                                 ; A524  60
loc_27_A525:
    LDA obj_phase,X                     ; A525  bd 00 06
    BNE loc_27_A53F                     ; A528  d0 15
    LDA obj_flags,X                     ; A52A  bd 80 03
    BMI loc_27_A53E                     ; A52D  30 0f
    LDA obj_screen_y,X                  ; A52F  bd e0 05
    CMP #$C0                            ; A532  c9 c0
    BCS loc_27_A53E                     ; A534  b0 08
    INC obj_phase,X                     ; A536  fe 00 06
    LDA #$FF                            ; A539  a9 ff
    STA boss_cam_lock                   ; A53B  8d d7 07
loc_27_A53E:
    RTS                                 ; A53E  60
loc_27_A53F:
    LDA ram_07D5                        ; A53F  ad d5 07
    BNE loc_27_A54F                     ; A542  d0 0b
    LDA #$FF                            ; A544  a9 ff
    STA boss_cam_lock                   ; A546  8d d7 07
    LDA #$05                            ; A549  a9 05
    STA obj_freeze                      ; A54B  8d a0 05
    RTS                                 ; A54E  60
loc_27_A54F:
    LDA #$10                            ; A54F  a9 10
    STA boss_cam_lock                   ; A551  8d d7 07
    RTS                                 ; A554  60
sub_27_A555:
    LDA obj_timer_s1F,X                 ; A555  bd 9f 04
    BNE loc_27_A55E                     ; A558  d0 04
    STA obj_active,X                    ; A55A  9d a0 04
    RTS                                 ; A55D  60
loc_27_A55E:
    LDA obj_phase,X                     ; A55E  bd 00 06
    CMP #$03                            ; A561  c9 03
    BNE loc_27_A576                     ; A563  d0 11
    LDA #$00                            ; A565  a9 00
    STA obj_timer_s1F,X                 ; A567  9d 9f 04
    STA $04A1,X                         ; A56A  9d a1 04
    LDA #$7D                            ; A56D  a9 7d
    STA tmp_11                          ; A56F  85 11
    STX jmp_trampoline                  ; A571  86 10
    JMP FarCall_C25D                    ; A573  4c 80 ff
loc_27_A576:
    LDA obj_meta_id_s01                 ; A576  ad 01 03
    CLC                                 ; A579  18
    ADC #$02                            ; A57A  69 02
    AND #$03                            ; A57C  29 03
    ASL A                               ; A57E  0a
    STA obj_meta_dir,X                  ; A57F  9d 60 03
    LDA obj_timer2,X                    ; A582  bd 20 06
    BEQ loc_27_A58B                     ; A585  f0 04
    DEC obj_timer2,X                    ; A587  de 20 06
    RTS                                 ; A58A  60
loc_27_A58B:
    LDA obj_flags2,X                    ; A58B  bd 40 03
    BPL loc_27_A5BF                     ; A58E  10 2f
    LDA $D1                             ; A590  a5 d1
    CMP #$01                            ; A592  c9 01
    BNE loc_27_A5BF                     ; A594  d0 29
    LDA obj_y_lo_s01                    ; A596  a5 b1
    CMP #$B0                            ; A598  c9 b0
    BCS loc_27_A5BF                     ; A59A  b0 23
    CMP #$28                            ; A59C  c9 28
    BCC loc_27_A5BF                     ; A59E  90 1f
    LDA #$FF                            ; A5A0  a9 ff
    STA player_hp_max,X                 ; A5A2  9d c0 03
    LDA #$10                            ; A5A5  a9 10
    STA obj_timer2,X                    ; A5A7  9d 20 06
    INC obj_phase,X                     ; A5AA  fe 00 06
    LDA obj_phase,X                     ; A5AD  bd 00 06
    CLC                                 ; A5B0  18
    ADC #$0F                            ; A5B1  69 0f
    STA pending_msg_id                  ; A5B3  8d c3 06
    LDA #$10                            ; A5B6  a9 10
    STA pending_msg_group               ; A5B8  8d e3 06
    LDA #$10                            ; A5BB  a9 10
    STA sub_state                       ; A5BD  85 41
loc_27_A5BF:
    RTS                                 ; A5BF  60
AiShopEmpty:  ; s60: AI $0E: sub_state=$24
    LDA #$24                            ; A5C0  a9 24
    STA sub_state                       ; A5C2  85 41
    RTS                                 ; A5C4  60
AiSetSubstate:  ; s60: AI $0F: sub_state=obj_timer2 ($0620; shop types $40-$48 -> $20-$23)
    LDA obj_timer2,X                    ; A5C5  bd 20 06
    STA sub_state                       ; A5C8  85 41
    RTS                                 ; A5CA  60
sub_27_A5CB:
    LDA obj_spawn_param_s01             ; A5CB  ad 61 05
    ORA player_bg_attr                  ; A5CE  0d 41 06
    BNE loc_27_A5D7                     ; A5D1  d0 04
    INC obj_active,X                    ; A5D3  fe a0 04
    RTS                                 ; A5D6  60
loc_27_A5D7:
    LDA #$10                            ; A5D7  a9 10
    STA obj_timer2,X                    ; A5D9  9d 20 06
    LDA #$4C                            ; A5DC  a9 4c
    STA obj_meta_id,X                   ; A5DE  9d 00 03
    STX jmp_trampoline                  ; A5E1  86 10
    LDX #$00                            ; A5E3  a2 00
    JSR ObjPosToTemp                    ; A5E5  20 a8 98
    LDX jmp_trampoline                  ; A5E8  a6 10
    JMP TempToObjPos                    ; A5EA  4c 97 98
sub_27_A5ED:
    LDA #$8A                            ; A5ED  a9 8a
    STA obj_meta_id,X                   ; A5EF  9d 00 03
    LDA obj_timer2,X                    ; A5F2  bd 20 06
    BEQ loc_27_A5FB                     ; A5F5  f0 04
    DEC obj_timer2,X                    ; A5F7  de 20 06
    RTS                                 ; A5FA  60
loc_27_A5FB:
    LDA #$30                            ; A5FB  a9 30
    STA obj_active,X                    ; A5FD  9d a0 04
    LDY #$04                            ; A600  a0 04
    LDA area_id                         ; A602  a5 6c
    CMP #$68                            ; A604  c9 68
    BNE loc_27_A609                     ; A606  d0 01
    INY                                 ; A608  c8
loc_27_A609:
    STY pending_msg_id                  ; A609  8c c3 06
    LDA #$0C                            ; A60C  a9 0c
    STA pending_msg_group               ; A60E  8d e3 06
    LDA #$10                            ; A611  a9 10
    STA sub_state                       ; A613  85 41
    RTS                                 ; A615  60
sub_27_A616:
    LDA area_id                         ; A616  a5 6c
    CMP #$51                            ; A618  c9 51
    BEQ loc_27_A624                     ; A61A  f0 08
    CMP #$60                            ; A61C  c9 60
    BNE loc_27_A65F                     ; A61E  d0 3f
    LDA area_entry                      ; A620  a5 6d
    BNE loc_27_A65F                     ; A622  d0 3b
loc_27_A624:
    LDA obj_x_lo,X                      ; A624  b5 70
    BNE loc_27_A62A                     ; A626  d0 02
    DEC obj_x_hi,X                      ; A628  d6 90
loc_27_A62A:
    DEC obj_x_lo,X                      ; A62A  d6 70
    DEC obj_anim,X                      ; A62C  de e0 04
    STX jmp_trampoline                  ; A62F  86 10
    JSR ObjPosToTemp                    ; A631  20 a8 98
    LDX #$00                            ; A634  a2 00
    JSR TempToObjPos                    ; A636  20 97 98
    INX                                 ; A639  e8
    JSR TempToObjPos                    ; A63A  20 97 98
    LDX jmp_trampoline                  ; A63D  a6 10
    LDA #$00                            ; A63F  a9 00
    STA obj_phase                       ; A641  8d 00 06
    STA obj_timer2                      ; A644  8d 20 06
    STA orbit_level                     ; A647  8d c0 06
    LDA obj_x_lo,X                      ; A64A  b5 70
    CMP #$80                            ; A64C  c9 80
    BNE loc_27_A65F                     ; A64E  d0 0f
    LDA obj_x_hi,X                      ; A650  b5 90
    CMP #$06                            ; A652  c9 06
    BNE loc_27_A65F                     ; A654  d0 09
    LDA #$88                            ; A656  a9 88
    STA obj_y_lo                        ; A658  85 b0
    LDA #$FF                            ; A65A  a9 ff
    STA obj_active,X                    ; A65C  9d a0 04
loc_27_A65F:
    RTS                                 ; A65F  60
sub_27_A660:
    LDA obj_timer2,X                    ; A660  bd 20 06
    BNE loc_27_A67A                     ; A663  d0 15
    LDA $04CD                           ; A665  ad cd 04
    STA tmp_11                          ; A668  85 11
    LDA #$0D                            ; A66A  a9 0d
    STA jmp_trampoline                  ; A66C  85 10
    JSR FarCall_C25D                    ; A66E  20 80 ff
    LDA #$70                            ; A671  a9 70
    STA obj_act_timer,X                 ; A673  9d 40 06
    INC obj_timer2,X                    ; A676  fe 20 06
    RTS                                 ; A679  60
loc_27_A67A:
    CMP #$01                            ; A67A  c9 01
    BNE loc_27_A69E                     ; A67C  d0 20
    DEC obj_act_timer,X                 ; A67E  de 40 06
    BEQ loc_27_A689                     ; A681  f0 06
    LDA #$05                            ; A683  a9 05
    STA obj_freeze                      ; A685  8d a0 05
    RTS                                 ; A688  60
loc_27_A689:
    LDA #$C0                            ; A689  a9 c0
    STA obj_meta_id,X                   ; A68B  9d 00 03
    INC obj_timer2,X                    ; A68E  fe 20 06
    LDA #$11                            ; A691  a9 11
    STA sub_state                       ; A693  85 41
    STX obj_timer2_s03                  ; A695  8e 23 06
    LDA #$10                            ; A698  a9 10
    STA obj_act_timer,X                 ; A69A  9d 40 06
    RTS                                 ; A69D  60
loc_27_A69E:
    LDA obj_flags,X                     ; A69E  bd 80 03
    BMI loc_27_A6C4                     ; A6A1  30 21
    LDA #$3B                            ; A6A3  a9 3b
    STA obj_meta_id,X                   ; A6A5  9d 00 03
    DEC obj_anim,X                      ; A6A8  de e0 04
    DEC obj_anim,X                      ; A6AB  de e0 04
    DEC obj_timer,X                     ; A6AE  de 80 04
    LDA #$06                            ; A6B1  a9 06
    STA obj_meta_dir,X                  ; A6B3  9d 60 03
    LDA #$07                            ; A6B6  a9 07
    STA obj_flags2,X                    ; A6B8  9d 40 03
    JSR ObjMoveSlide                    ; A6BB  20 d7 98
    BCC loc_27_A6C4                     ; A6BE  90 04
    LDA #$FF                            ; A6C0  a9 ff
    STA obj_x_hi,X                      ; A6C2  95 90
loc_27_A6C4:
    RTS                                 ; A6C4  60
sub_27_A6C5:
    JSR sub_27_A6FB                     ; A6C5  20 fb a6
    DEC obj_anim,X                      ; A6C8  de e0 04
    LDA obj_flags,X                     ; A6CB  bd 80 03
    BMI loc_27_A6FA                     ; A6CE  30 2a
    LDA $62F1                           ; A6D0  ad f1 62
    BNE loc_27_A6EE                     ; A6D3  d0 19
    LDA area_id                         ; A6D5  a5 6c
    CMP #$03                            ; A6D7  c9 03
    BNE loc_27_A6EE                     ; A6D9  d0 13
    LDA obj_active_s0D                  ; A6DB  ad ad 04
    CMP #$08                            ; A6DE  c9 08
    BEQ loc_27_A6EE                     ; A6E0  f0 0c
    LDA $04CD                           ; A6E2  ad cd 04
    STA tmp_11                          ; A6E5  85 11
    LDA #$0D                            ; A6E7  a9 0d
    STA jmp_trampoline                  ; A6E9  85 10
    JSR FarCall_C25D                    ; A6EB  20 80 ff
loc_27_A6EE:
    LDA obj_anim,X                      ; A6EE  bd e0 04
    AND #$1F                            ; A6F1  29 1f
    BNE loc_27_A6FA                     ; A6F3  d0 05
    LDA #$37                            ; A6F5  a9 37
    JMP PlaySound                       ; A6F7  4c 25 c1
loc_27_A6FA:
    RTS                                 ; A6FA  60
sub_27_A6FB:
    LDY orbit_level,X                   ; A6FB  bc c0 06
    LDA story_flags,Y                   ; A6FE  b9 80 64
    STA jmp_trampoline                  ; A701  85 10
    LDY obj_behav,X                     ; A703  bc e0 06
    AND BitMask,Y                       ; A706  39 00 c0
    BNE loc_27_A70D                     ; A709  d0 02
    PLA                                 ; A70B  68
    PLA                                 ; A70C  68
loc_27_A70D:
    RTS                                 ; A70D  60
sub_27_A70E:
    LDA obj_aux,X                       ; A70E  bd 60 06
    BNE loc_27_A725                     ; A711  d0 12
    INC obj_aux,X                       ; A713  fe 60 06
    LDA #$03                            ; A716  a9 03
    STA pending_msg_id                  ; A718  8d c3 06
    LDA #$03                            ; A71B  a9 03
    STA pending_msg_group               ; A71D  8d e3 06
    LDA #$10                            ; A720  a9 10
    STA sub_state                       ; A722  85 41
    RTS                                 ; A724  60
loc_27_A725:
    DEC obj_y_lo                        ; A725  c6 b0
    DEC obj_y_lo_s01                    ; A727  c6 b1
    DEC obj_anim_s01                    ; A729  ce e1 04
    LDA #$00                            ; A72C  a9 00
    STA obj_meta_id_s01                 ; A72E  8d 01 03
    LDA obj_y_lo                        ; A731  a5 b0
    CMP #$86                            ; A733  c9 86
    BNE loc_27_A752                     ; A735  d0 1b
    LDA #$60                            ; A737  a9 60
    STA obj_coll_s0D                    ; A739  8d ad 03
    LDA #$52                            ; A73C  a9 52
    STA obj_active_s0D                  ; A73E  8d ad 04
    LDA #$0B                            ; A741  a9 0b
    STA obj_active                      ; A743  8d a0 04
    LDA #$86                            ; A746  a9 86
    STA obj_y_lo                        ; A748  85 b0
    LDA #$4E                            ; A74A  a9 4e
    STA chr_r5_play                     ; A74C  8d f5 07
    INC story_flags_F7F8                ; A74F  ee 7f 65
loc_27_A752:
    RTS                                 ; A752  60
sub_27_A753:
    LDA obj_timer2,X                    ; A753  bd 20 06
    BEQ loc_27_A774                     ; A756  f0 1c
    STA obj_anim,X                      ; A758  9d e0 04
    DEC obj_timer2,X                    ; A75B  de 20 06
    DEC obj_timer2,X                    ; A75E  de 20 06
    LDA #$56                            ; A761  a9 56
    STA obj_meta_id,X                   ; A763  9d 00 03
    LDA obj_anim,X                      ; A766  bd e0 04
    CMP #$0E                            ; A769  c9 0e
    BNE loc_27_A7CC                     ; A76B  d0 5f
    LDA #$5B                            ; A76D  a9 5b
    LDY #$04                            ; A76F  a0 04
    JMP SpawnShot                       ; A771  4c 2d 97
loc_27_A774:
    LDA #$55                            ; A774  a9 55
    STA obj_meta_id,X                   ; A776  9d 00 03
    LDA obj_x_lo_s01                    ; A779  a5 71
    CMP obj_x_lo,X                      ; A77B  d5 70
    BEQ loc_27_A799                     ; A77D  f0 1a
    LDA #$FF                            ; A77F  a9 ff
    BCC loc_27_A785                     ; A781  90 02
    LDA #$01                            ; A783  a9 01
loc_27_A785:
    CLC                                 ; A785  18
    ADC obj_x_lo,X                      ; A786  75 70
    STA obj_x_lo,X                      ; A788  95 70
    CMP #$27                            ; A78A  c9 27
    BNE loc_27_A790                     ; A78C  d0 02
    INC obj_x_lo,X                      ; A78E  f6 70
loc_27_A790:
    CMP #$98                            ; A790  c9 98
    BNE loc_27_A796                     ; A792  d0 02
    DEC obj_x_lo,X                      ; A794  d6 70
loc_27_A796:
    DEC obj_anim,X                      ; A796  de e0 04
loc_27_A799:
    LDA tmp_08                          ; A799  a5 08
    AND #$07                            ; A79B  29 07
    BNE loc_27_A7CC                     ; A79D  d0 2d
    LDA #$02                            ; A79F  a9 02
    STA jmp_trampoline                  ; A7A1  85 10
    LDA story_flags_F7F8                ; A7A3  ad 7f 65
    ASL A                               ; A7A6  0a
    CLC                                 ; A7A7  18
    ADC #$06                            ; A7A8  69 06
    AND #$07                            ; A7AA  29 07
    ORA #$01                            ; A7AC  09 01
    STA jmp_trampoline                  ; A7AE  85 10
    JSR Rand64                          ; A7B0  20 d7 97
    CMP jmp_trampoline                  ; A7B3  c5 10
    BMI loc_27_A7CC                     ; A7B5  30 15
    SEC                                 ; A7B7  38
    LDA obj_x_lo_s01                    ; A7B8  a5 71
    SBC obj_x_lo,X                      ; A7BA  f5 70
    BPL loc_27_A7C3                     ; A7BC  10 05
    EOR #$FF                            ; A7BE  49 ff
    SEC                                 ; A7C0  38
    ADC #$01                            ; A7C1  69 01
loc_27_A7C3:
    CMP #$08                            ; A7C3  c9 08
    BCS loc_27_A7CC                     ; A7C5  b0 05
    LDA #$1E                            ; A7C7  a9 1e
    STA obj_timer2,X                    ; A7C9  9d 20 06
loc_27_A7CC:
    RTS                                 ; A7CC  60
sub_27_A7CD:
    LDA obj_aux,X                       ; A7CD  bd 60 06
    BEQ loc_27_A7EA                     ; A7D0  f0 18
    LDA obj_flags,X                     ; A7D2  bd 80 03
    ORA #$04                            ; A7D5  09 04
    STA obj_flags,X                     ; A7D7  9d 80 03
    INC obj_aux,X                       ; A7DA  fe 60 06
    LDA obj_aux,X                       ; A7DD  bd 60 06
    CMP #$40                            ; A7E0  c9 40
    BNE loc_27_A7E9                     ; A7E2  d0 05
    LDA #$00                            ; A7E4  a9 00
    STA obj_active,X                    ; A7E6  9d a0 04
loc_27_A7E9:
    RTS                                 ; A7E9  60
loc_27_A7EA:
    LDA sub_state                       ; A7EA  a5 41
    CMP #$08                            ; A7EC  c9 08
    BNE loc_27_A7E9                     ; A7EE  d0 f9
    INC obj_aux,X                       ; A7F0  fe 60 06
    LDA #$43                            ; A7F3  a9 43
    STA $0575                           ; A7F5  8d 75 05
    LDA #$15                            ; A7F8  a9 15
    STA obj_timer2_s03                  ; A7FA  8d 23 06
    LDA #$07                            ; A7FD  a9 07
    STA sub_state                       ; A7FF  85 41
    RTS                                 ; A801  60
sub_27_A802:
    LDA obj_flags2                      ; A802  ad 40 03
    BMI loc_27_A80B                     ; A805  30 04
    LDA pad_dir                         ; A807  a5 49
    BMI loc_27_A85E                     ; A809  30 53
loc_27_A80B:
    LDA tmp_08                          ; A80B  a5 08
    AND #$03                            ; A80D  29 03
    BNE loc_27_A849                     ; A80F  d0 38
    LDA obj_flags,X                     ; A811  bd 80 03
    ORA #$10                            ; A814  09 10
    STA obj_flags,X                     ; A816  9d 80 03
    LDY #$00                            ; A819  a0 00
    JSR DirToTarget                     ; A81B  20 09 84
    LDA tmp_20                          ; A81E  a5 20
    STA obj_meta_dir,X                  ; A820  9d 60 03
    LDA #$05                            ; A823  a9 05
    LDY tmp_21                          ; A825  a4 21
    CPY #$02                            ; A827  c0 02
    BCC loc_27_A82D                     ; A829  90 02
    LDA #$07                            ; A82B  a9 07
loc_27_A82D:
    LDY obj_flags2                      ; A82D  ac 40 03
    BPL loc_27_A834                     ; A830  10 02
    LDA #$08                            ; A832  a9 08
loc_27_A834:
    STA obj_flags2,X                    ; A834  9d 40 03
    LDA obj_meta_dir,X                  ; A837  bd 60 03
    LSR A                               ; A83A  4a
    AND #$03                            ; A83B  29 03
    STA jmp_trampoline                  ; A83D  85 10
    LDA obj_meta_id,X                   ; A83F  bd 00 03
    AND #$FC                            ; A842  29 fc
    ORA jmp_trampoline                  ; A844  05 10
    STA obj_meta_id,X                   ; A846  9d 00 03
loc_27_A849:
    DEC obj_timer,X                     ; A849  de 80 04
    DEC obj_anim,X                      ; A84C  de e0 04
    JSR ObjPosToTemp                    ; A84F  20 a8 98
    LDA obj_meta_dir,X                  ; A852  bd 60 03
    JSR ObjStepVector                   ; A855  20 80 84
    JSR TempAddOffset                   ; A858  20 24 98
    JMP TempToObjPos                    ; A85B  4c 97 98
loc_27_A85E:
    RTS                                 ; A85E  60
sub_27_A85F:
    LDA obj_shot_id,X                   ; A85F  bd 40 04
    BEQ loc_27_A8B0                     ; A862  f0 4c
    LDA area_id                         ; A864  a5 6c
    CMP #$A8                            ; A866  c9 a8
    BEQ loc_27_A875                     ; A868  f0 0b
    CMP #$8F                            ; A86A  c9 8f
    BEQ loc_27_A875                     ; A86C  f0 07
    LDA #$4C                            ; A86E  a9 4c
    STA obj_overlay_id,X                ; A870  9d 80 05
    BNE loc_27_A8B0                     ; A873  d0 3b
loc_27_A875:
    LDA obj_behav,X                     ; A875  bd e0 06
    BEQ loc_27_A8A6                     ; A878  f0 2c
    DEC obj_behav,X                     ; A87A  de e0 06
    LSR A                               ; A87D  4a
    STA obj_anim,X                      ; A87E  9d e0 04
    LDA obj_behav,X                     ; A881  bd e0 06
    CMP #$30                            ; A884  c9 30
    BNE loc_27_A8B0                     ; A886  d0 28
    LDA obj_shot_id,X                   ; A888  bd 40 04
    LDY #$00                            ; A88B  a0 00
    JSR SpawnShot                       ; A88D  20 2d 97
    BCC loc_27_A8B0                     ; A890  90 1e
    JSR Rand64                          ; A892  20 d7 97
    AND #$03                            ; A895  29 03
    TAY                                 ; A897  a8
    LDA AiDirTable_A8A2,Y               ; A898  b9 a2 a8
    LDY jmp_trampoline                  ; A89B  a4 10
    STA obj_meta_dir,Y                  ; A89D  99 60 03
    BNE loc_27_A8B0                     ; A8A0  d0 0e
AiDirTable_A8A2:  ; s32: 4 bytes 08 08 09 07 = obj_meta_dir by Rand64&3 (27:A898); tracer decodes them as code because the BNE at A8A0 is always taken
    .db $08,$08,$09,$07                 ; 27:A8A2
loc_27_A8A6:
    LDA obj_flags,X                     ; A8A6  bd 80 03
    BMI loc_27_A8B0                     ; A8A9  30 05
    LDA #$3E                            ; A8AB  a9 3e
    STA obj_behav,X                     ; A8AD  9d e0 06
loc_27_A8B0:
    LDY obj_x_hi,X                      ; A8B0  b4 90
    LDA BitMask,Y                       ; A8B2  b9 00 c0
    STA jmp_trampoline                  ; A8B5  85 10
    LDY obj_y_hi,X                      ; A8B7  b4 d0
    LDA visited_map,Y                   ; A8B9  b9 f0 62
    AND jmp_trampoline                  ; A8BC  25 10
    BEQ loc_27_A8C5                     ; A8BE  f0 05
    LDA #$00                            ; A8C0  a9 00
    STA obj_active,X                    ; A8C2  9d a0 04
loc_27_A8C5:
    LDA obj_overlay_id,X                ; A8C5  bd 80 05
    STA obj_meta_id,X                   ; A8C8  9d 00 03
    LDA obj_flags,X                     ; A8CB  bd 80 03
    BMI loc_27_A8FB                     ; A8CE  30 2b
    LDY obj_phase,X                     ; A8D0  bc 00 06
    LDA obj_screen_y,X                  ; A8D3  bd e0 05
    CMP $A8FC,Y                         ; A8D6  d9 fc a8
    BCC loc_27_A8FB                     ; A8D9  90 20
    LDA obj_screen_y,X                  ; A8DB  bd e0 05
    CMP $A900,Y                         ; A8DE  d9 00 a9
    BCS loc_27_A8FB                     ; A8E1  b0 18
    LDA obj_screen_x,X                  ; A8E3  bd c0 05
    CMP $A904,Y                         ; A8E6  d9 04 a9
    BCC loc_27_A8FB                     ; A8E9  90 10
    LDA obj_screen_x,X                  ; A8EB  bd c0 05
    CMP $A908,Y                         ; A8EE  d9 08 a9
    BCS loc_27_A8FB                     ; A8F1  b0 08
    LDA obj_coll,X                      ; A8F3  bd a0 03
    ORA #$10                            ; A8F6  09 10
    STA obj_coll,X                      ; A8F8  9d a0 03
loc_27_A8FB:
    RTS                                 ; A8FB  60
    .db $30,$18,$18,$00,$B8,$C8,$C8,$00,$38,$38,$18,$00,$D8,$D8,$E8,$00; 27:A8FC
sub_27_A90C:
    LDA #$0F                            ; A90C  a9 0f
    JSR SetR7                           ; A90E  20 27 c4
    JMP loc_27_B728                     ; A911  4c 28 b7
sub_27_A914:
    RTS                                 ; A914  60
AiWalkPause:  ; AI $20: walk $0480 frames / pause $0600 frames
    LDA obj_flags,X                     ; A915  bd 80 03
    BPL loc_27_A923                     ; A918  10 09
    TXA                                 ; A91A  8a
    CLC                                 ; A91B  18
    ADC tmp_08                          ; A91C  65 08
    AND #$03                            ; A91E  29 03
    BEQ loc_27_A923                     ; A920  f0 01
    RTS                                 ; A922  60
loc_27_A923:
    LDA obj_phase,X                     ; A923  bd 00 06
    BEQ loc_27_A936                     ; A926  f0 0e
    DEC obj_phase,X                     ; A928  de 00 06
    LDA obj_anim,X                      ; A92B  bd e0 04
    AND #$07                            ; A92E  29 07
    BEQ loc_27_A935                     ; A930  f0 03
    DEC obj_anim,X                      ; A932  de e0 04
loc_27_A935:
    RTS                                 ; A935  60
loc_27_A936:
    LDA obj_timer,X                     ; A936  bd 80 04
    BNE loc_27_A93E                     ; A939  d0 03
    JSR AiWalkPick                      ; A93B  20 4a a9
loc_27_A93E:
    DEC obj_anim,X                      ; A93E  de e0 04
    DEC obj_timer,X                     ; A941  de 80 04
    JSR ObjMoveSlide                    ; A944  20 d7 98
    BCS AiWalkPick                      ; A947  b0 01
    RTS                                 ; A949  60
AiWalkPick:  ; random dir&6; walk (rand*4+$20); pause ($08&7)*8
    JSR Rand64                          ; A94A  20 d7 97
    AND #$06                            ; A94D  29 06
    STA obj_meta_dir,X                  ; A94F  9d 60 03
    JSR Rand64                          ; A952  20 d7 97
    ASL A                               ; A955  0a
    ASL A                               ; A956  0a
    CLC                                 ; A957  18
    ADC #$20                            ; A958  69 20
    STA obj_timer,X                     ; A95A  9d 80 04
    LDA tmp_08                          ; A95D  a5 08
    AND #$07                            ; A95F  29 07
    SEC                                 ; A961  38
    ASL A                               ; A962  0a
    ASL A                               ; A963  0a
    ASL A                               ; A964  0a
    STA obj_phase,X                     ; A965  9d 00 06
    RTS                                 ; A968  60
AiShooterWalk:  ; s47: AI $21: AiWalkPause + AiShootCheck; countdown every other frame; shot at $0640=$0F
    LDA obj_act_timer,X                 ; A969  bd 40 06
    LDA obj_act_timer,X                 ; A96C  bd 40 06
    BEQ loc_27_A998                     ; A96F  f0 27
    DEC obj_aux,X                       ; A971  de 60 06
    LDA obj_aux,X                       ; A974  bd 60 06
    LSR A                               ; A977  4a
    BCC loc_27_A9B3                     ; A978  90 39
    DEC obj_act_timer,X                 ; A97A  de 40 06
    LDA obj_act_timer,X                 ; A97D  bd 40 06
    LSR A                               ; A980  4a
    STA obj_anim,X                      ; A981  9d e0 04
    LDA obj_act_timer,X                 ; A984  bd 40 06
    CMP #$0F                            ; A987  c9 0f
    BNE loc_27_A9B3                     ; A989  d0 28
    LDA obj_meta_id,X                   ; A98B  bd 00 03
    ASL A                               ; A98E  0a
    AND #$07                            ; A98F  29 07
    TAY                                 ; A991  a8
    LDA obj_shot_id,X                   ; A992  bd 40 04
    JMP SpawnShot                       ; A995  4c 2d 97
loc_27_A998:
    JSR AiShootCheck                    ; A998  20 14 b7
    JSR AiWalkPause                     ; A99B  20 15 a9
    LDA obj_meta_id,X                   ; A99E  bd 00 03
    AND #$FC                            ; A9A1  29 fc
    STA jmp_trampoline                  ; A9A3  85 10
    LDA obj_meta_dir,X                  ; A9A5  bd 60 03
    LSR A                               ; A9A8  4a
    ORA jmp_trampoline                  ; A9A9  05 10
    STA obj_meta_id,X                   ; A9AB  9d 00 03
    LDA #$00                            ; A9AE  a9 00
    STA obj_phase,X                     ; A9B0  9d 00 06
loc_27_A9B3:
    RTS                                 ; A9B3  60
AiSpawnChildLunger:  ; s47: AI $2E: once SpawnShot(shot id) and link child obj_param2; then AiLunger
    LDA obj_param2,X                    ; A9B4  bd a0 06
    BNE loc_27_A9CF                     ; A9B7  d0 16
    LDY #$00                            ; A9B9  a0 00
    LDA obj_shot_id,X                   ; A9BB  bd 40 04
    JSR SpawnShot                       ; A9BE  20 2d 97
    BCC loc_27_A9CF                     ; A9C1  90 0c
    LDY jmp_trampoline                  ; A9C3  a4 10
    TXA                                 ; A9C5  8a
    STA obj_param2,Y                    ; A9C6  99 a0 06
    LDA #$00                            ; A9C9  a9 00
    STA obj_shot_id,X                   ; A9CB  9d 40 04
    RTS                                 ; A9CE  60
loc_27_A9CF:
    JMP AiLunger                        ; A9CF  4c f8 ab
AiChaseOnscreen:  ; s47: AI $25: AiChase unless offscreen
    LDA obj_flags,X                     ; A9D2  bd 80 03
    BMI loc_27_A9DA                     ; A9D5  30 03
    JMP AiChase                         ; A9D7  4c 57 aa
loc_27_A9DA:
    RTS                                 ; A9DA  60
AiShooterChaseOnscreen:  ; s47: AI $27: AiShooterChase unless offscreen
    LDA obj_flags,X                     ; A9DB  bd 80 03
    BMI loc_27_A9E3                     ; A9DE  30 03
    JMP AiShooterChase                  ; A9E0  4c e4 a9
loc_27_A9E3:
    RTS                                 ; A9E3  60
AiShooterChase:  ; s47: AI $26: attack countdown (meta base $06E0) else AiShootCheck + AiChase (meta base $06C0)
    LDA obj_act_timer,X                 ; A9E4  bd 40 06
    BEQ loc_27_AA31                     ; A9E7  f0 48
    LDA obj_flags,X                     ; A9E9  bd 80 03
    AND #$08                            ; A9EC  29 08
    BEQ loc_27_A9F4                     ; A9EE  f0 04
    LDA #$00                            ; A9F0  a9 00
    BEQ loc_27_A9FA                     ; A9F2  f0 06
loc_27_A9F4:
    LDA obj_meta_dir,X                  ; A9F4  bd 60 03
    AND #$07                            ; A9F7  29 07
    LSR A                               ; A9F9  4a
loc_27_A9FA:
    ORA obj_behav,X                     ; A9FA  1d e0 06
    STA obj_meta_id,X                   ; A9FD  9d 00 03
    DEC obj_act_timer,X                 ; AA00  de 40 06
    LDA obj_act_timer,X                 ; AA03  bd 40 06
    LSR A                               ; AA06  4a
    STA obj_anim,X                      ; AA07  9d e0 04
    LDA obj_act_timer,X                 ; AA0A  bd 40 06
    CMP #$0F                            ; AA0D  c9 0f
    BNE loc_27_AA56                     ; AA0F  d0 45
    LDA obj_flags,X                     ; AA11  bd 80 03
    AND #$08                            ; AA14  29 08
    BNE loc_27_AA25                     ; AA16  d0 0d
    LDA obj_meta_id,X                   ; AA18  bd 00 03
    AND #$03                            ; AA1B  29 03
    ASL A                               ; AA1D  0a
    TAY                                 ; AA1E  a8
    LDA obj_shot_id,X                   ; AA1F  bd 40 04
    JMP SpawnShot                       ; AA22  4c 2d 97
loc_27_AA25:
    LDA obj_meta_dir,X                  ; AA25  bd 60 03
    AND #$06                            ; AA28  29 06
    TAY                                 ; AA2A  a8
    LDA obj_shot_id,X                   ; AA2B  bd 40 04
    JMP SpawnShot                       ; AA2E  4c 2d 97
loc_27_AA31:
    LDA obj_flags,X                     ; AA31  bd 80 03
    AND #$08                            ; AA34  29 08
    BEQ loc_27_AA3C                     ; AA36  f0 04
    LDA #$00                            ; AA38  a9 00
    BEQ loc_27_AA42                     ; AA3A  f0 06
loc_27_AA3C:
    LDA obj_meta_dir,X                  ; AA3C  bd 60 03
    AND #$07                            ; AA3F  29 07
    LSR A                               ; AA41  4a
loc_27_AA42:
    ORA orbit_level,X                   ; AA42  1d c0 06
    STA obj_meta_id,X                   ; AA45  9d 00 03
    LDA obj_tile_ofs,X                  ; AA48  bd 20 03
    AND #$03                            ; AA4B  29 03
    STA obj_tile_ofs,X                  ; AA4D  9d 20 03
    JSR AiShootCheck                    ; AA50  20 14 b7
    JMP AiChase                         ; AA53  4c 57 aa
loc_27_AA56:
    RTS                                 ; AA56  60
AiChase:  ; s47: AI $24: 4-dir chase of slot 0 every 16 steps; blocked -> 96-step wall-follow detour
    LDY #$07                            ; AA57  a0 07
    LDA obj_flags,X                     ; AA59  bd 80 03
    BMI loc_27_AA60                     ; AA5C  30 02
    LDY #$01                            ; AA5E  a0 01
loc_27_AA60:
    STY jmp_trampoline                  ; AA60  84 10
    TXA                                 ; AA62  8a
    CLC                                 ; AA63  18
    ADC tmp_08                          ; AA64  65 08
    AND jmp_trampoline                  ; AA66  25 10
    BEQ loc_27_AA6B                     ; AA68  f0 01
    RTS                                 ; AA6A  60
loc_27_AA6B:
    DEC obj_anim,X                      ; AA6B  de e0 04
    DEC obj_anim,X                      ; AA6E  de e0 04
    DEC obj_timer,X                     ; AA71  de 80 04
    LDA obj_timer2,X                    ; AA74  bd 20 06
    BNE loc_27_AAC0                     ; AA77  d0 47
    LDA obj_timer,X                     ; AA79  bd 80 04
    AND #$0F                            ; AA7C  29 0f
    BNE loc_27_AA96                     ; AA7E  d0 16
    LDY #$00                            ; AA80  a0 00
    JSR DirToTarget                     ; AA82  20 09 84
    LDA tmp_20                          ; AA85  a5 20
    AND #$FE                            ; AA87  29 fe
    STA obj_meta_dir,X                  ; AA89  9d 60 03
    LDA obj_flags,X                     ; AA8C  bd 80 03
    AND #$08                            ; AA8F  29 08
    BNE loc_27_AA96                     ; AA91  d0 03
    JSR sub_27_AB2D                     ; AA93  20 2d ab
loc_27_AA96:
    JSR ObjMoveSlide                    ; AA96  20 d7 98
    BCS loc_27_AA9C                     ; AA99  b0 01
    RTS                                 ; AA9B  60
loc_27_AA9C:
    LDA #$60                            ; AA9C  a9 60
    STA obj_timer2,X                    ; AA9E  9d 20 06
    TXA                                 ; AAA1  8a
    ADC tmp_08                          ; AAA2  65 08
    LSR A                               ; AAA4  4a
    LSR A                               ; AAA5  4a
    AND #$01                            ; AAA6  29 01
    STA obj_phase,X                     ; AAA8  9d 00 06
sub_27_AAAB:
    LDA obj_phase,X                     ; AAAB  bd 00 06
    EOR #$01                            ; AAAE  49 01
sub_27_AAB0:
    LSR A                               ; AAB0  4a
    LDY obj_meta_dir,X                  ; AAB1  bc 60 03
    DEY                                 ; AAB4  88
    BCC loc_27_AAB9                     ; AAB5  90 02
    INY                                 ; AAB7  c8
    INY                                 ; AAB8  c8
loc_27_AAB9:
    TYA                                 ; AAB9  98
    AND #$07                            ; AABA  29 07
    STA obj_meta_dir,X                  ; AABC  9d 60 03
    RTS                                 ; AABF  60
loc_27_AAC0:
    DEC obj_timer2,X                    ; AAC0  de 20 06
    LDA obj_anim,X                      ; AAC3  bd e0 04
    AND #$07                            ; AAC6  29 07
    BNE loc_27_AAE0                     ; AAC8  d0 16
    LDA obj_phase,X                     ; AACA  bd 00 06
    JSR sub_27_AAB0                     ; AACD  20 b0 aa
    JSR AiTryMove                       ; AAD0  20 fa aa
    BCC loc_27_AAEF                     ; AAD3  90 1a
    JSR sub_27_AAAB                     ; AAD5  20 ab aa
    JSR AiTryMove                       ; AAD8  20 fa aa
    BCC loc_27_AAE0                     ; AADB  90 03
    JMP sub_27_AAAB                     ; AADD  4c ab aa
loc_27_AAE0:
    JSR ObjMoveSlide                    ; AAE0  20 d7 98
    LDA tmp_20                          ; AAE3  a5 20
    BNE loc_27_AAEC                     ; AAE5  d0 05
    LDA tmp_23                          ; AAE7  a5 23
    STA obj_meta_dir,X                  ; AAE9  9d 60 03
loc_27_AAEC:
    JMP sub_27_AB2D                     ; AAEC  4c 2d ab
loc_27_AAEF:
    LDA tmp_23                          ; AAEF  a5 23
    STA obj_meta_dir,X                  ; AAF1  9d 60 03
    JSR ObjMoveSlide                    ; AAF4  20 d7 98
    JMP sub_27_AB2D                     ; AAF7  4c 2d ab
AiTryMove:  ; s47: trial ObjMoveSlide at speed $0A then restore position; C=1 blocked
    LDA obj_x_lo,X                      ; AAFA  b5 70
    STA tmp_2B                          ; AAFC  85 2b
    LDA obj_x_hi,X                      ; AAFE  b5 90
    STA tmp_2C                          ; AB00  85 2c
    LDA obj_y_lo,X                      ; AB02  b5 b0
    STA tmp_2D                          ; AB04  85 2d
    LDA obj_y_hi,X                      ; AB06  b5 d0
    STA tmp_2E                          ; AB08  85 2e
    LDA obj_flags2,X                    ; AB0A  bd 40 03
    STA tmp_2F                          ; AB0D  85 2f
    LDA #$0A                            ; AB0F  a9 0a
    STA obj_flags2,X                    ; AB11  9d 40 03
    JSR ObjMoveSlide                    ; AB14  20 d7 98
    LDA tmp_2B                          ; AB17  a5 2b
    STA obj_x_lo,X                      ; AB19  95 70
    LDA tmp_2C                          ; AB1B  a5 2c
    STA obj_x_hi,X                      ; AB1D  95 90
    LDA tmp_2D                          ; AB1F  a5 2d
    STA obj_y_lo,X                      ; AB21  95 b0
    LDA tmp_2E                          ; AB23  a5 2e
    STA obj_y_hi,X                      ; AB25  95 d0
    LDA tmp_2F                          ; AB27  a5 2f
    STA obj_flags2,X                    ; AB29  9d 40 03
    RTS                                 ; AB2C  60
sub_27_AB2D:
    LDA obj_flags,X                     ; AB2D  bd 80 03
    AND #$08                            ; AB30  29 08
    BEQ loc_27_AB35                     ; AB32  f0 01
    RTS                                 ; AB34  60
loc_27_AB35:
    LDA obj_meta_dir,X                  ; AB35  bd 60 03
    AND #$06                            ; AB38  29 06
    LSR A                               ; AB3A  4a
    ORA orbit_level,X                   ; AB3B  1d c0 06
    STA obj_meta_id,X                   ; AB3E  9d 00 03
    RTS                                 ; AB41  60
AiDormant:  ; s47: AI $22: wait obj_anim onscreen then obj_active=obj_param2 meta=$06C0
    LDA obj_flags,X                     ; AB42  bd 80 03
    BMI loc_27_AB60                     ; AB45  30 19
    DEC obj_anim,X                      ; AB47  de e0 04
    BNE loc_27_AB60                     ; AB4A  d0 14
    LDA obj_param2,X                    ; AB4C  bd a0 06
    STA obj_active,X                    ; AB4F  9d a0 04
    LDA orbit_level,X                   ; AB52  bd c0 06
    STA obj_meta_id,X                   ; AB55  9d 00 03
    LDA obj_coll,X                      ; AB58  bd a0 03
    ORA #$10                            ; AB5B  09 10
    STA obj_coll,X                      ; AB5D  9d a0 03
loc_27_AB60:
    RTS                                 ; AB60  60
AiAimShooter:  ; s47: AI $28: start delay then aimed shot (DirToTarget) at $0640=$0F; else AiChase
    LDA obj_aux,X                       ; AB61  bd 60 06
    BNE loc_27_AB6F                     ; AB64  d0 09
    DEC obj_anim,X                      ; AB66  de e0 04
    BNE loc_27_AB6E                     ; AB69  d0 03
    INC obj_aux,X                       ; AB6B  fe 60 06
loc_27_AB6E:
    RTS                                 ; AB6E  60
loc_27_AB6F:
    LDA #$65                            ; AB6F  a9 65
    STA obj_meta_id,X                   ; AB71  9d 00 03
    LDA obj_act_timer,X                 ; AB74  bd 40 06
    BEQ loc_27_AB98                     ; AB77  f0 1f
    LDA #$91                            ; AB79  a9 91
    STA obj_meta_id,X                   ; AB7B  9d 00 03
    DEC obj_act_timer,X                 ; AB7E  de 40 06
    LDA obj_act_timer,X                 ; AB81  bd 40 06
    STA obj_anim,X                      ; AB84  9d e0 04
    CMP #$0F                            ; AB87  c9 0f
    BNE loc_27_AB6E                     ; AB89  d0 e3
    LDY #$01                            ; AB8B  a0 01
    JSR DirToTarget                     ; AB8D  20 09 84
    LDY tmp_20                          ; AB90  a4 20
    LDA obj_shot_id,X                   ; AB92  bd 40 04
    JMP SpawnShot                       ; AB95  4c 2d 97
loc_27_AB98:
    JSR AiShootCheck                    ; AB98  20 14 b7
    JMP AiChase                         ; AB9B  4c 57 aa
AiBurrower:  ; s47: AI $29: coll $41 walking / $11 attacking; coll=0 while slot0 $0620 (player jump) != 0
    JSR sub_27_ABAC                     ; AB9E  20 ac ab
    LDA obj_timer2                      ; ABA1  ad 20 06
    BEQ loc_27_ABAB                     ; ABA4  f0 05
    LDA #$00                            ; ABA6  a9 00
    STA obj_coll,X                      ; ABA8  9d a0 03
loc_27_ABAB:
    RTS                                 ; ABAB  60
sub_27_ABAC:
    LDA #$41                            ; ABAC  a9 41
    STA obj_coll,X                      ; ABAE  9d a0 03
    LDA #$68                            ; ABB1  a9 68
    STA obj_meta_id,X                   ; ABB3  9d 00 03
    LDA obj_act_timer,X                 ; ABB6  bd 40 06
    BEQ loc_27_ABF2                     ; ABB9  f0 37
    LDA #$11                            ; ABBB  a9 11
    STA obj_coll,X                      ; ABBD  9d a0 03
    LDA #$6B                            ; ABC0  a9 6b
    STA obj_meta_id,X                   ; ABC2  9d 00 03
    LDA obj_act_timer,X                 ; ABC5  bd 40 06
    CMP #$10                            ; ABC8  c9 10
    BNE loc_27_ABD9                     ; ABCA  d0 0d
    LDY #$01                            ; ABCC  a0 01
    JSR DirToTarget                     ; ABCE  20 09 84
    LDY tmp_20                          ; ABD1  a4 20
    LDA obj_shot_id,X                   ; ABD3  bd 40 04
    JSR SpawnShot                       ; ABD6  20 2d 97
loc_27_ABD9:
    DEC obj_anim,X                      ; ABD9  de e0 04
    DEC obj_timer,X                     ; ABDC  de 80 04
    LDA obj_timer,X                     ; ABDF  bd 80 04
    AND #$03                            ; ABE2  29 03
    BNE loc_27_ABE9                     ; ABE4  d0 03
    DEC obj_act_timer,X                 ; ABE6  de 40 06
loc_27_ABE9:
    RTS                                 ; ABE9  60
    .db $00,$08,$10,$18,$18,$10,$08,$00 ; 27:ABEA
loc_27_ABF2:
    JSR AiShootCheck                    ; ABF2  20 14 b7
    JMP AiChase                         ; ABF5  4c 57 aa
AiLunger:  ; s47: AI $2A: within 32px + Rand64=0 -> face player + melee shot at $0640=8; else AiChase
    LDA obj_flags,X                     ; ABF8  bd 80 03
    BPL loc_27_ABFE                     ; ABFB  10 01
    RTS                                 ; ABFD  60
loc_27_ABFE:
    LDA obj_act_timer,X                 ; ABFE  bd 40 06
    BEQ loc_27_AC30                     ; AC01  f0 2d
    LDA obj_meta_dir,X                  ; AC03  bd 60 03
    LSR A                               ; AC06  4a
    ORA obj_behav,X                     ; AC07  1d e0 06
    STA obj_meta_id,X                   ; AC0A  9d 00 03
    DEC obj_aux,X                       ; AC0D  de 60 06
    LDA obj_aux,X                       ; AC10  bd 60 06
    LSR A                               ; AC13  4a
    BCC loc_27_AC5F                     ; AC14  90 49
    DEC obj_act_timer,X                 ; AC16  de 40 06
    LDA obj_act_timer,X                 ; AC19  bd 40 06
    STA obj_anim,X                      ; AC1C  9d e0 04
    CMP #$08                            ; AC1F  c9 08
    BNE loc_27_AC5F                     ; AC21  d0 3c
    LDA obj_meta_id,X                   ; AC23  bd 00 03
    AND #$03                            ; AC26  29 03
    ASL A                               ; AC28  0a
    TAY                                 ; AC29  a8
    LDA obj_shot_id,X                   ; AC2A  bd 40 04
    JMP SpawnShot                       ; AC2D  4c 2d 97
loc_27_AC30:
    LDA obj_meta_dir,X                  ; AC30  bd 60 03
    LSR A                               ; AC33  4a
    ORA orbit_level,X                   ; AC34  1d c0 06
    STA obj_meta_id,X                   ; AC37  9d 00 03
    JSR AiLungeCheck                    ; AC3A  20 40 ac
    JMP AiChase                         ; AC3D  4c 57 aa
AiLungeCheck:  ; s47: distance < 2 cells and Rand64=0 -> $0640=$0F and skip caller
    LDY #$01                            ; AC40  a0 01
    JSR DirToTarget                     ; AC42  20 09 84
    LDA tmp_21                          ; AC45  a5 21
    CMP #$02                            ; AC47  c9 02
    BCS loc_27_AC5F                     ; AC49  b0 14
    JSR Rand64                          ; AC4B  20 d7 97
    BNE loc_27_AC5F                     ; AC4E  d0 0f
    LDA tmp_20                          ; AC50  a5 20
    STA obj_meta_dir,X                  ; AC52  9d 60 03
    LDA #$0F                            ; AC55  a9 0f
    STA obj_act_timer,X                 ; AC57  9d 40 06
    STA obj_anim,X                      ; AC5A  9d e0 04
    PLA                                 ; AC5D  68
    PLA                                 ; AC5E  68
loc_27_AC5F:
    RTS                                 ; AC5F  60
AI_TrapChest:  ; AI $2B (type $73): phase0 meta $AA wait; phase1 meta $90 wander with $0620 timer
    LDA obj_phase,X                     ; AC60  bd 00 06
    BEQ loc_27_ACAE                     ; AC63  f0 49
    DEC obj_timer,X                     ; AC65  de 80 04
    LDA #$90                            ; AC68  a9 90
    STA obj_meta_id,X                   ; AC6A  9d 00 03
    DEC obj_anim,X                      ; AC6D  de e0 04
    DEC obj_timer2,X                    ; AC70  de 20 06
    LDA obj_timer2,X                    ; AC73  bd 20 06
    JSR ChestHop                        ; AC76  20 9f b7
    LDA obj_timer2,X                    ; AC79  bd 20 06
    AND #$3F                            ; AC7C  29 3f
    BEQ loc_27_AC8C                     ; AC7E  f0 0c
    LDA obj_meta_dir,X                  ; AC80  bd 60 03
    JSR ObjStepVector                   ; AC83  20 80 84
    JSR TempAddStep                     ; AC86  20 61 98
    JMP TempToObjPos                    ; AC89  4c 97 98
loc_27_AC8C:
    LDA #$40                            ; AC8C  a9 40
    JSR ObjBgProbe                      ; AC8E  20 30 9a
    LDA tmp_20                          ; AC91  a5 20
    AND #$06                            ; AC93  29 06
    BEQ loc_27_ACA3                     ; AC95  f0 0c
    LDA obj_meta_dir,X                  ; AC97  bd 60 03
    CLC                                 ; AC9A  18
    ADC #$04                            ; AC9B  69 04
    AND #$07                            ; AC9D  29 07
    STA obj_meta_dir,X                  ; AC9F  9d 60 03
    RTS                                 ; ACA2  60
loc_27_ACA3:
    LDY #$01                            ; ACA3  a0 01
    JSR DirToTarget                     ; ACA5  20 09 84
    LDA tmp_20                          ; ACA8  a5 20
    STA obj_meta_dir,X                  ; ACAA  9d 60 03
    RTS                                 ; ACAD  60
loc_27_ACAE:
    LDA #$AA                            ; ACAE  a9 aa
    STA obj_meta_id,X                   ; ACB0  9d 00 03
    LDY #$01                            ; ACB3  a0 01
    JSR DirToTarget                     ; ACB5  20 09 84
    LDA tmp_21                          ; ACB8  a5 21
    BNE loc_27_ACBF                     ; ACBA  d0 03
    INC obj_phase,X                     ; ACBC  fe 00 06
loc_27_ACBF:
    RTS                                 ; ACBF  60
AiBurst:  ; s47: AI $2C: 3 x SpawnShot random dir then despawn
    LDA #$03                            ; ACC0  a9 03
    STA tmp_1F                          ; ACC2  85 1f
loc_27_ACC4:
    JSR Rand64                          ; ACC4  20 d7 97
    TAY                                 ; ACC7  a8
    LDA obj_shot_id,X                   ; ACC8  bd 40 04
    JSR SpawnShot                       ; ACCB  20 2d 97
    DEC tmp_1F                          ; ACCE  c6 1f
    BNE loc_27_ACC4                     ; ACD0  d0 f2
    LDA #$00                            ; ACD2  a9 00
    STA obj_active,X                    ; ACD4  9d a0 04
    RTS                                 ; ACD7  60
loc_27_ACD8:
    RTS                                 ; ACD8  60
AiNpcWander:  ; AI $30: talkable NPC random 4-dir wander with pauses
    LDA obj_shot_id,X                   ; ACD9  bd 40 04
    BEQ loc_27_ACE2                     ; ACDC  f0 04
    DEC obj_shot_id,X                   ; ACDE  de 40 04
    RTS                                 ; ACE1  60
loc_27_ACE2:
    LDA obj_behav,X                     ; ACE2  bd e0 06
    AND #$04                            ; ACE5  29 04
    BEQ loc_27_ACEC                     ; ACE7  f0 03
    JMP sub_27_AE16                     ; ACE9  4c 16 ae
loc_27_ACEC:
    LDA tmp_08                          ; ACEC  a5 08
    AND #$07                            ; ACEE  29 07
    TAY                                 ; ACF0  a8
    LDA BitMask,Y                       ; ACF1  b9 00 c0
    AND obj_phase,X                     ; ACF4  3d 00 06
    BEQ loc_27_ACD8                     ; ACF7  f0 df
    LDA obj_anim,X                      ; ACF9  bd e0 04
    AND #$0F                            ; ACFC  29 0f
    BEQ loc_27_AD03                     ; ACFE  f0 03
    JMP loc_27_AD84                     ; AD00  4c 84 ad
loc_27_AD03:
    LDA #$04                            ; AD03  a9 04
    STA tmp_21                          ; AD05  85 21
    LDA obj_anim,X                      ; AD07  bd e0 04
    AND #$10                            ; AD0A  29 10
    BNE loc_27_AD1E                     ; AD0C  d0 10
    JSR Rand64                          ; AD0E  20 d7 97
    STA jmp_trampoline                  ; AD11  85 10
    AND #$09                            ; AD13  29 09
    BNE loc_27_AD1E                     ; AD15  d0 07
    LDA jmp_trampoline                  ; AD17  a5 10
    AND #$06                            ; AD19  29 06
    STA obj_meta_dir,X                  ; AD1B  9d 60 03
loc_27_AD1E:
    LDA obj_meta_dir,X                  ; AD1E  bd 60 03
    AND #$06                            ; AD21  29 06
    STA obj_meta_dir,X                  ; AD23  9d 60 03
    TAY                                 ; AD26  a8
    LDA NpcLookAhead,Y                  ; AD27  b9 9c ad
    STA tmp_30                          ; AD2A  85 30
    LDA $AD9D,Y                         ; AD2C  b9 9d ad
    STA tmp_31                          ; AD2F  85 31
    JSR ObjPosToTemp                    ; AD31  20 a8 98
    JSR TempAddOffset                   ; AD34  20 24 98
    LDA #$00                            ; AD37  a9 00
    JSR ObjBgProbe                      ; AD39  20 30 9a
    JSR sub_27_ADA4                     ; AD3C  20 a4 ad
    LDA tmp_20                          ; AD3F  a5 20
    AND #$06                            ; AD41  29 06
    BEQ loc_27_AD60                     ; AD43  f0 1b
    DEC tmp_21                          ; AD45  c6 21
    BNE loc_27_AD4A                     ; AD47  d0 01
    RTS                                 ; AD49  60
loc_27_AD4A:
    LDY obj_meta_dir,X                  ; AD4A  bc 60 03
    INY                                 ; AD4D  c8
    INY                                 ; AD4E  c8
    TXA                                 ; AD4F  8a
    LSR A                               ; AD50  4a
    BCC loc_27_AD57                     ; AD51  90 04
    DEY                                 ; AD53  88
    DEY                                 ; AD54  88
    DEY                                 ; AD55  88
    DEY                                 ; AD56  88
loc_27_AD57:
    TYA                                 ; AD57  98
    AND #$06                            ; AD58  29 06
    STA obj_meta_dir,X                  ; AD5A  9d 60 03
    JMP loc_27_AD1E                     ; AD5D  4c 1e ad
loc_27_AD60:
    LDA obj_meta_dir,X                  ; AD60  bd 60 03
    LSR A                               ; AD63  4a
    STA jmp_trampoline                  ; AD64  85 10
    LDA obj_meta_id,X                   ; AD66  bd 00 03
    AND #$FC                            ; AD69  29 fc
    ORA jmp_trampoline                  ; AD6B  05 10
    STA obj_meta_id,X                   ; AD6D  9d 00 03
    LDA tmp_08                          ; AD70  a5 08
    AND #$D6                            ; AD72  29 d6
    BNE loc_27_AD84                     ; AD74  d0 0e
    TXA                                 ; AD76  8a
    ADC obj_x_lo                        ; AD77  65 70
    EOR obj_y_lo                        ; AD79  45 b0
    AND #$02                            ; AD7B  29 02
    BNE loc_27_AD84                     ; AD7D  d0 05
    LDA #$20                            ; AD7F  a9 20
    STA obj_shot_id,X                   ; AD81  9d 40 04
loc_27_AD84:
    JSR ObjPosToTemp                    ; AD84  20 a8 98
    LDA obj_meta_dir,X                  ; AD87  bd 60 03
    JSR ObjStepVector                   ; AD8A  20 80 84
    JSR TempAddStep                     ; AD8D  20 61 98
    LDA #$C0                            ; AD90  a9 c0
    JSR ObjBgProbe                      ; AD92  20 30 9a
    JSR TempToObjPos                    ; AD95  20 97 98
    DEC obj_anim,X                      ; AD98  de e0 04
    RTS                                 ; AD9B  60
NpcLookAhead:  ; 4 x (dx;dy) 16px look-ahead per dir
    .db $00,$F0,$10,$00,$00,$10,$F0,$00 ; 27:AD9C
sub_27_ADA4:
    LDA pos_x_lo                        ; ADA4  a5 34
    LDY pos_x_hi                        ; ADA6  a4 35
    JSR sub_27_AE07                     ; ADA8  20 07 ae
    STA jmp_trampoline                  ; ADAB  85 10
    LDA pos_y_lo                        ; ADAD  a5 36
    LDY pos_y_hi                        ; ADAF  a4 37
    JSR sub_27_AE07                     ; ADB1  20 07 ae
    STA tmp_11                          ; ADB4  85 11
    LDA #$0D                            ; ADB6  a9 0d
    STA tmp_12                          ; ADB8  85 12
    LDA #$1F                            ; ADBA  a9 1f
    STA tmp_13                          ; ADBC  85 13
    JSR sub_27_ADC8                     ; ADBE  20 c8 ad
    LDY #$00                            ; ADC1  a0 00
    STY tmp_12                          ; ADC3  84 12
    INY                                 ; ADC5  c8
    STY tmp_13                          ; ADC6  84 13
sub_27_ADC8:
    CPX tmp_12                          ; ADC8  e4 12
    BEQ loc_27_ADFE                     ; ADCA  f0 32
    LDY tmp_12                          ; ADCC  a4 12
    LDA obj_active,Y                    ; ADCE  b9 a0 04
    BEQ loc_27_ADFE                     ; ADD1  f0 2b
    LDA a:obj_x_hi,Y                    ; ADD3  b9 90 00
    STA tmp_1F                          ; ADD6  85 1f
    LDA a:obj_x_lo,Y                    ; ADD8  b9 70 00
    LDY tmp_1F                          ; ADDB  a4 1f
    JSR sub_27_AE07                     ; ADDD  20 07 ae
    CMP jmp_trampoline                  ; ADE0  c5 10
    BNE loc_27_ADFE                     ; ADE2  d0 1a
    LDY tmp_12                          ; ADE4  a4 12
    LDA a:obj_y_hi,Y                    ; ADE6  b9 d0 00
    STA tmp_1F                          ; ADE9  85 1f
    LDA a:obj_y_lo,Y                    ; ADEB  b9 b0 00
    LDY tmp_1F                          ; ADEE  a4 1f
    JSR sub_27_AE07                     ; ADF0  20 07 ae
    CMP tmp_11                          ; ADF3  c5 11
    BNE loc_27_ADFE                     ; ADF5  d0 07
    LDA tmp_20                          ; ADF7  a5 20
    ORA #$06                            ; ADF9  09 06
    STA tmp_20                          ; ADFB  85 20
    RTS                                 ; ADFD  60
loc_27_ADFE:
    INC tmp_12                          ; ADFE  e6 12
    LDA tmp_12                          ; AE00  a5 12
    CMP tmp_13                          ; AE02  c5 13
    BNE sub_27_ADC8                     ; AE04  d0 c2
    RTS                                 ; AE06  60
sub_27_AE07:
    STY tmp_1F                          ; AE07  84 1f
    LSR tmp_1F                          ; AE09  46 1f
    ROR A                               ; AE0B  6a
    LSR tmp_1F                          ; AE0C  46 1f
    ROR A                               ; AE0E  6a
    LSR tmp_1F                          ; AE0F  46 1f
    ROR A                               ; AE11  6a
    LSR tmp_1F                          ; AE12  46 1f
    ROR A                               ; AE14  6a
    RTS                                 ; AE15  60
sub_27_AE16:
    LDA obj_behav,X                     ; AE16  bd e0 06
    AND #$01                            ; AE19  29 01
    BEQ loc_27_AE25                     ; AE1B  f0 08
    LDA tmp_08                          ; AE1D  a5 08
    LSR A                               ; AE1F  4a
    LSR A                               ; AE20  4a
    LSR A                               ; AE21  4a
    STA obj_anim,X                      ; AE22  9d e0 04
loc_27_AE25:
    LDA obj_phase_s03                   ; AE25  ad 03 06
    BNE loc_27_AE30                     ; AE28  d0 06
    LDA orbit_level,X                   ; AE2A  bd c0 06
    STA obj_meta_id,X                   ; AE2D  9d 00 03
loc_27_AE30:
    RTS                                 ; AE30  60
AiPathWalk:  ; AI $32 (type $38): walk path AiPathTable[$0680] step $0620 one dir byte per 16 steps; FC/FD/FE/FF terminators
    LDA #$04                            ; AE31  a9 04
    STA obj_freeze                      ; AE33  8d a0 05
    LDA tmp_08                          ; AE36  a5 08
    AND #$07                            ; AE38  29 07
    TAY                                 ; AE3A  a8
    LDA BitMask,Y                       ; AE3B  b9 00 c0
    AND obj_phase,X                     ; AE3E  3d 00 06
    BEQ loc_27_AE7A                     ; AE41  f0 37
    DEC obj_anim,X                      ; AE43  de e0 04
    LDA obj_anim,X                      ; AE46  bd e0 04
    AND #$0F                            ; AE49  29 0f
    BNE loc_27_AEB7                     ; AE4B  d0 6a
    LDA obj_param1,X                    ; AE4D  bd 80 06
    ASL A                               ; AE50  0a
    TAY                                 ; AE51  a8
    LDA AiPathTable,Y                   ; AE52  b9 04 af
    STA jmp_trampoline                  ; AE55  85 10
    LDA $AF05,Y                         ; AE57  b9 05 af
    STA tmp_11                          ; AE5A  85 11
    LDY obj_timer2,X                    ; AE5C  bc 20 06
    LDA (jmp_trampoline),Y              ; AE5F  b1 10
    BMI loc_27_AE66                     ; AE61  30 03
    JMP loc_27_AEB1                     ; AE63  4c b1 ae
loc_27_AE66:
    CMP #$FC                            ; AE66  c9 fc
    BEQ loc_27_AE7B                     ; AE68  f0 11
    CMP #$FD                            ; AE6A  c9 fd
    BEQ loc_27_AE9C                     ; AE6C  f0 2e
    LDY #$30                            ; AE6E  a0 30
    CMP #$FE                            ; AE70  c9 fe
    BNE loc_27_AE76                     ; AE72  d0 02
    LDY #$00                            ; AE74  a0 00
loc_27_AE76:
    TYA                                 ; AE76  98
    STA obj_active,X                    ; AE77  9d a0 04
loc_27_AE7A:
    RTS                                 ; AE7A  60
loc_27_AE7B:
    LDA #$32                            ; AE7B  a9 32
    STA obj_active_s0D                  ; AE7D  8d ad 04
    STA obj_active_s0E                  ; AE80  8d ae 04
    LDA #$0D                            ; AE83  a9 0d
    STA obj_param1_s0D                  ; AE85  8d 8d 06
    STA obj_param1_s0E                  ; AE88  8d 8e 06
    LDA #$01                            ; AE8B  a9 01
    STA obj_anim_s0D                    ; AE8D  8d ed 04
    STA obj_anim_s0E                    ; AE90  8d ee 04
    LDA #$00                            ; AE93  a9 00
    STA obj_timer2_s0D                  ; AE95  8d 2d 06
    STA obj_timer2_s0E                  ; AE98  8d 2e 06
    RTS                                 ; AE9B  60
loc_27_AE9C:
    LDA #$0A                            ; AE9C  a9 0a
    STA obj_active                      ; AE9E  8d a0 04
    LDA #$30                            ; AEA1  a9 30
    STA obj_active,X                    ; AEA3  9d a0 04
    LDA #$06                            ; AEA6  a9 06
    STA obj_behav,X                     ; AEA8  9d e0 06
    LDA #$2E                            ; AEAB  a9 2e
    STA orbit_level,X                   ; AEAD  9d c0 06
    RTS                                 ; AEB0  60
loc_27_AEB1:
    STA obj_meta_dir,X                  ; AEB1  9d 60 03
    INC obj_timer2,X                    ; AEB4  fe 20 06
loc_27_AEB7:
    LDA obj_meta_id,X                   ; AEB7  bd 00 03
    AND #$FC                            ; AEBA  29 fc
    STA jmp_trampoline                  ; AEBC  85 10
    LDA obj_meta_dir,X                  ; AEBE  bd 60 03
    LSR A                               ; AEC1  4a
    AND #$03                            ; AEC2  29 03
    ORA jmp_trampoline                  ; AEC4  05 10
    STA obj_meta_id,X                   ; AEC6  9d 00 03
    LDA obj_meta_dir,X                  ; AEC9  bd 60 03
    JSR ObjStepVector                   ; AECC  20 80 84
    JSR ObjPosToTemp                    ; AECF  20 a8 98
    JSR TempAddStep                     ; AED2  20 61 98
    JSR TempToObjPos                    ; AED5  20 97 98
    LDA area_id                         ; AED8  a5 6c
    CMP #$D4                            ; AEDA  c9 d4
    BNE loc_27_AF03                     ; AEDC  d0 25
    LDA obj_param1,X                    ; AEDE  bd 80 06
    CMP #$0D                            ; AEE1  c9 0d
    BNE loc_27_AF03                     ; AEE3  d0 1e
    CPX #$0D                            ; AEE5  e0 0d
    BNE loc_27_AF03                     ; AEE7  d0 1a
    LDA obj_y_lo,X                      ; AEE9  b5 b0
    STA obj_y_lo_s01                    ; AEEB  85 b1
    STA obj_y_lo                        ; AEED  85 b0
    LDA obj_x_lo,X                      ; AEEF  b5 70
    CLC                                 ; AEF1  18
    ADC #$10                            ; AEF2  69 10
    STA obj_x_lo_s01                    ; AEF4  85 71
    STA obj_x_lo                        ; AEF6  85 70
    LDA #$00                            ; AEF8  a9 00
    STA obj_timer2                      ; AEFA  8d 20 06
    STA obj_act_timer                   ; AEFD  8d 40 06
    STA orbit_level                     ; AF00  8d c0 06
loc_27_AF03:
    RTS                                 ; AF03  60
AiPathTable:  ; ptr16[16] direction-byte paths for AiPathWalk (data AF24-AFA5)
    .db $24,$AF,$2C,$AF,$34,$AF,$36,$AF,$38,$AF,$42,$AF,$4E,$AF,$64,$AF; 27:AF04
    .db $6E,$AF,$79,$AF,$85,$AF,$8A,$AF,$8F,$AF,$9B,$AF,$A3,$AF,$A5,$AF; 27:AF14
    .db $06,$06,$04,$04,$04,$04,$04,$FE,$00,$02,$02,$02,$02,$02,$02,$FE; 27:AF24
    .db $02,$FF,$06,$FF,$06,$06,$06,$06,$06,$06,$06,$06,$06,$FE,$04,$04; 27:AF34
    .db $04,$04,$04,$04,$02,$04,$04,$02,$02,$FF,$02,$02,$02,$02,$02,$02; 27:AF44
    .db $02,$04,$02,$02,$04,$02,$02,$04,$02,$02,$02,$04,$04,$02,$02,$FF; 27:AF54
    .db $02,$02,$02,$02,$02,$02,$02,$02,$02,$FF,$00,$00,$02,$00,$00,$02; 27:AF64
    .db $02,$02,$02,$02,$FF,$00,$00,$00,$00,$00,$06,$06,$06,$00,$06,$06; 27:AF74
    .db $FF,$02,$02,$02,$02,$FF,$02,$02,$02,$02,$FD,$00,$00,$00,$06,$06; 27:AF84
    .db $06,$06,$06,$00,$00,$00,$FE,$04,$04,$04,$04,$04,$04,$04,$FE,$02; 27:AF94
    .db $FF,$00,$06,$06,$FC             ; 27:AFA4
sub_27_AFA9:
    DEC obj_timer,X                     ; AFA9  de 80 04
    JSR ObjPosToTemp                    ; AFAC  20 a8 98
    LDA obj_meta_dir,X                  ; AFAF  bd 60 03
    AND #$06                            ; AFB2  29 06
    TAY                                 ; AFB4  a8
    LDA $AFED,Y                         ; AFB5  b9 ed af
    STA tmp_30                          ; AFB8  85 30
    LDA $AFEE,Y                         ; AFBA  b9 ee af
    STA tmp_31                          ; AFBD  85 31
    JSR TempAddOffset                   ; AFBF  20 24 98
    LDA #$00                            ; AFC2  a9 00
    JSR ObjBgProbe                      ; AFC4  20 30 9a
    LDA tmp_20                          ; AFC7  a5 20
    AND #$01                            ; AFC9  29 01
    BEQ loc_27_AFDC                     ; AFCB  f0 0f
    JSR ObjPosToTemp                    ; AFCD  20 a8 98
    LDA obj_meta_dir,X                  ; AFD0  bd 60 03
    JSR ObjStepVector                   ; AFD3  20 80 84
    JSR TempAddStep                     ; AFD6  20 61 98
    JMP TempToObjPos                    ; AFD9  4c 97 98
loc_27_AFDC:
    LDA obj_meta_dir,X                  ; AFDC  bd 60 03
    CLC                                 ; AFDF  18
    ADC #$04                            ; AFE0  69 04
    AND #$07                            ; AFE2  29 07
    STA obj_meta_dir,X                  ; AFE4  9d 60 03
    LDA #$20                            ; AFE7  a9 20
    STA obj_freeze,X                    ; AFE9  9d a0 05
    RTS                                 ; AFEC  60
    .db $00,$E8,$0C,$00,$00,$08,$F4,$00 ; 27:AFED
sub_27_AFF5:
    JSR sub_27_AFFB                     ; AFF5  20 fb af
    JMP sub_27_AFA9                     ; AFF8  4c a9 af
sub_27_AFFB:
    CPX ride_platform_slot              ; AFFB  ec 41 05
    BNE loc_27_B006                     ; AFFE  d0 06
    LDA #$01                            ; B000  a9 01
    STA obj_phase,X                     ; B002  9d 00 06
loc_27_B005:
    RTS                                 ; B005  60
loc_27_B006:
    LDA obj_anim,X                      ; B006  bd e0 04
    CMP #$1E                            ; B009  c9 1e
    BNE loc_27_B012                     ; B00B  d0 05
    LDA #$64                            ; B00D  a9 64
    JSR PlaySound                       ; B00F  20 25 c1
loc_27_B012:
    LDA obj_phase,X                     ; B012  bd 00 06
    BEQ loc_27_B005                     ; B015  f0 ee
    LDA #$00                            ; B017  a9 00
    STA obj_coll,X                      ; B019  9d a0 03
    LDA #$B7                            ; B01C  a9 b7
    STA obj_meta_id,X                   ; B01E  9d 00 03
    DEC obj_anim,X                      ; B021  de e0 04
    BNE loc_27_B005                     ; B024  d0 df
    LDA #$00                            ; B026  a9 00
    STA obj_active,X                    ; B028  9d a0 04
    RTS                                 ; B02B  60
sub_27_B02C:
    DEC obj_anim,X                      ; B02C  de e0 04
    BNE loc_27_B037                     ; B02F  d0 06
    LDA #$00                            ; B031  a9 00
    STA obj_active,X                    ; B033  9d a0 04
    RTS                                 ; B036  60
loc_27_B037:
    LDA obj_anim,X                      ; B037  bd e0 04
    LSR A                               ; B03A  4a
    BCC loc_27_B047                     ; B03B  90 0a
    LDA obj_meta_dir,X                  ; B03D  bd 60 03
    TAY                                 ; B040  a8
    LDA obj_shot_id,X                   ; B041  bd 40 04
    JMP SpawnShot                       ; B044  4c 2d 97
loc_27_B047:
    RTS                                 ; B047  60
sub_27_B048:
    JSR ObjPosToTemp                    ; B048  20 a8 98
    LDA #$00                            ; B04B  a9 00
    STA tmp_30                          ; B04D  85 30
    LDA obj_timer2,X                    ; B04F  bd 20 06
    STA tmp_31                          ; B052  85 31
    INC obj_timer2,X                    ; B054  fe 20 06
    CMP #$08                            ; B057  c9 08
    BNE loc_27_B05E                     ; B059  d0 03
    DEC obj_timer2,X                    ; B05B  de 20 06
loc_27_B05E:
    JSR TempAddStep                     ; B05E  20 61 98
    JSR TempToObjPos                    ; B061  20 97 98
    LDA obj_y_lo,X                      ; B064  b5 b0
    CMP #$C8                            ; B066  c9 c8
    BCC loc_27_B06F                     ; B068  90 05
    LDA #$00                            ; B06A  a9 00
    STA obj_active,X                    ; B06C  9d a0 04
loc_27_B06F:
    RTS                                 ; B06F  60
sub_27_B070:
    LDA #$00                            ; B070  a9 00
    STA obj_meta_dir,X                  ; B072  9d 60 03
    LDA obj_timer2,X                    ; B075  bd 20 06
    BNE sub_27_B0BE                     ; B078  d0 44
    INC obj_timer2,X                    ; B07A  fe 20 06
    LDY #$00                            ; B07D  a0 00
    LDA #$14                            ; B07F  a9 14
    JSR SpawnShot                       ; B081  20 2d 97
    BCC sub_27_B0BE                     ; B084  90 38
    LDY jmp_trampoline                  ; B086  a4 10
    LDA a:obj_y_lo,Y                    ; B088  b9 b0 00
    CLC                                 ; B08B  18
    ADC #$18                            ; B08C  69 18
    STA a:obj_y_lo,Y                    ; B08E  99 b0 00
    JMP sub_27_B0BE                     ; B091  4c be b0
sub_27_B094:
    LDA obj_meta_dir,X                  ; B094  bd 60 03
    AND #$07                            ; B097  29 07
    ASL A                               ; B099  0a
    ASL A                               ; B09A  0a
    ASL A                               ; B09B  0a
    STA obj_anim,X                      ; B09C  9d e0 04
    JMP sub_27_B0BE                     ; B09F  4c be b0
sub_27_B0A2:
    DEC obj_anim,X                      ; B0A2  de e0 04
    JMP sub_27_B0BE                     ; B0A5  4c be b0
AiDirShot:  ; s47: AI $11: meta |= dir>>1 once then DEC obj_active -> AI $10 AiStraightMover
    LDA obj_meta_dir,X                  ; B0A8  bd 60 03
    LSR A                               ; B0AB  4a
    ORA obj_meta_id,X                   ; B0AC  1d 00 03
    STA obj_meta_id,X                   ; B0AF  9d 00 03
    DEC obj_active,X                    ; B0B2  de a0 04
AiStraightMover:  ; AI $10: lifetime $0480; ObjStepVector; optional BG collision via $0460
    LDA obj_anim,X                      ; B0B5  bd e0 04
    SEC                                 ; B0B8  38
    SBC #$04                            ; B0B9  e9 04
    STA obj_anim,X                      ; B0BB  9d e0 04
sub_27_B0BE:
    LDA obj_flags,X                     ; B0BE  bd 80 03
    BPL loc_27_B0C6                     ; B0C1  10 03
    JMP ObjDespawn                      ; B0C3  4c c7 98
loc_27_B0C6:
    DEC obj_timer,X                     ; B0C6  de 80 04
    BNE loc_27_B0CE                     ; B0C9  d0 03
    JMP ObjBecomeNext                   ; B0CB  4c cd 98
loc_27_B0CE:
    JSR ObjPosToTemp                    ; B0CE  20 a8 98
    LDA obj_meta_dir,X                  ; B0D1  bd 60 03
    JSR ObjStepVector                   ; B0D4  20 80 84
    JSR TempAddStep                     ; B0D7  20 61 98
    LDA obj_bg_mask,X                   ; B0DA  bd 60 04
    BNE loc_27_B0E2                     ; B0DD  d0 03
    JMP TempToObjPos                    ; B0DF  4c 97 98
loc_27_B0E2:
    LDA #$C0                            ; B0E2  a9 c0
    JSR ObjBgProbe                      ; B0E4  20 30 9a
    LDA tmp_20                          ; B0E7  a5 20
    AND obj_bg_mask,X                   ; B0E9  3d 60 04
    AND #$06                            ; B0EC  29 06
    BEQ loc_27_B0F3                     ; B0EE  f0 03
    JMP ObjBecomeNext                   ; B0F0  4c cd 98
loc_27_B0F3:
    JMP TempToObjPos                    ; B0F3  4c 97 98
sub_27_B0F6:
    DEC obj_anim,X                      ; B0F6  de e0 04
    DEC obj_anim,X                      ; B0F9  de e0 04
    LDA obj_timer,X                     ; B0FC  bd 80 04
    AND #$0F                            ; B0FF  29 0f
    BNE loc_27_B118                     ; B101  d0 15
    TXA                                 ; B103  8a
    LSR A                               ; B104  4a
    BCC loc_27_B10D                     ; B105  90 06
    DEC obj_meta_dir,X                  ; B107  de 60 03
    DEC obj_meta_dir,X                  ; B10A  de 60 03
loc_27_B10D:
    INC obj_meta_dir,X                  ; B10D  fe 60 03
    LDA obj_meta_dir,X                  ; B110  bd 60 03
    AND #$0F                            ; B113  29 0f
    STA obj_meta_dir,X                  ; B115  9d 60 03
loc_27_B118:
    JMP sub_27_B0BE                     ; B118  4c be b0
sub_27_B11B:
    DEC obj_anim,X                      ; B11B  de e0 04
    DEC obj_anim,X                      ; B11E  de e0 04
    BPL sub_27_B0BE                     ; B121  10 9b
    INC obj_meta_id,X                   ; B123  fe 00 03
    LDA #$10                            ; B126  a9 10
    STA obj_active,X                    ; B128  9d a0 04
    BNE sub_27_B0BE                     ; B12B  d0 91
sub_27_B12D:
    LDA obj_flags,X                     ; B12D  bd 80 03
    BPL loc_27_B135                     ; B130  10 03
    JMP ObjDespawn                      ; B132  4c c7 98
loc_27_B135:
    LDA #$05                            ; B135  a9 05
    STA tmp_1F                          ; B137  85 1f
loc_27_B139:
    DEC obj_timer,X                     ; B139  de 80 04
    DEC obj_anim,X                      ; B13C  de e0 04
    BEQ loc_27_B16E                     ; B13F  f0 2d
    JSR ObjPosToTemp                    ; B141  20 a8 98
    LDA obj_meta_dir,X                  ; B144  bd 60 03
    JSR ObjStepVector                   ; B147  20 80 84
    JSR TempAddStep                     ; B14A  20 61 98
    JSR TempToObjPos                    ; B14D  20 97 98
    LDA #$40                            ; B150  a9 40
    JSR ObjBgProbe                      ; B152  20 30 9a
    LDA tmp_20                          ; B155  a5 20
    AND obj_bg_mask,X                   ; B157  3d 60 04
    AND #$06                            ; B15A  29 06
    BEQ loc_27_B173                     ; B15C  f0 15
    LDA obj_meta_dir,X                  ; B15E  bd 60 03
    CLC                                 ; B161  18
    ADC #$04                            ; B162  69 04
    AND #$0F                            ; B164  29 0f
    STA obj_meta_dir,X                  ; B166  9d 60 03
    DEC tmp_1F                          ; B169  c6 1f
    BNE loc_27_B139                     ; B16B  d0 cc
    RTS                                 ; B16D  60
loc_27_B16E:
    LDA #$00                            ; B16E  a9 00
    STA obj_bg_mask,X                   ; B170  9d 60 04
loc_27_B173:
    RTS                                 ; B173  60
sub_27_B174:
    LDA obj_phase,X                     ; B174  bd 00 06
    BEQ loc_27_B1BC                     ; B177  f0 43
    DEC obj_anim,X                      ; B179  de e0 04
    DEC obj_anim,X                      ; B17C  de e0 04
    DEC obj_timer,X                     ; B17F  de 80 04
    BNE loc_27_B187                     ; B182  d0 03
    JMP ObjDespawn                      ; B184  4c c7 98
loc_27_B187:
    LDA obj_timer2,X                    ; B187  bd 20 06
    AND #$1F                            ; B18A  29 1f
    ASL A                               ; B18C  0a
    TAY                                 ; B18D  a8
    LDA $B1F5,Y                         ; B18E  b9 f5 b1
    STA tmp_30                          ; B191  85 30
    LDA $B1F6,Y                         ; B193  b9 f6 b1
    STA tmp_31                          ; B196  85 31
    LDA obj_meta_dir,X                  ; B198  bd 60 03
    LSR A                               ; B19B  4a
    LSR A                               ; B19C  4a
    BCC loc_27_B1A7                     ; B19D  90 08
    LDA tmp_30                          ; B19F  a5 30
    LDY tmp_31                          ; B1A1  a4 31
    STA tmp_31                          ; B1A3  85 31
    STY tmp_30                          ; B1A5  84 30
loc_27_B1A7:
    INC obj_timer2,X                    ; B1A7  fe 20 06
    JSR ObjPosToTemp                    ; B1AA  20 a8 98
    JSR TempAddStep                     ; B1AD  20 61 98
    LDA obj_meta_dir,X                  ; B1B0  bd 60 03
    JSR ObjStepVector                   ; B1B3  20 80 84
    JSR TempAddStep                     ; B1B6  20 61 98
    JMP TempToObjPos                    ; B1B9  4c 97 98
loc_27_B1BC:
    INC obj_phase,X                     ; B1BC  fe 00 06
    LDA $B1E5,X                         ; B1BF  bd e5 b1
    STA obj_timer2,X                    ; B1C2  9d 20 06
    LDA $B1E9,X                         ; B1C5  bd e9 b1
    STA tmp_30                          ; B1C8  85 30
    LDY $B1ED,X                         ; B1CA  bc ed b1
    STY tmp_31                          ; B1CD  84 31
    LDA obj_meta_dir,X                  ; B1CF  bd 60 03
    LSR A                               ; B1D2  4a
    LSR A                               ; B1D3  4a
    BCC loc_27_B1DC                     ; B1D4  90 06
    LDA tmp_30                          ; B1D6  a5 30
    STA tmp_31                          ; B1D8  85 31
    STY tmp_30                          ; B1DA  84 30
loc_27_B1DC:
    JSR ObjPosToTemp                    ; B1DC  20 a8 98
    JSR TempAddOffset                   ; B1DF  20 24 98
    JMP TempToObjPos                    ; B1E2  4c 97 98
    .db $00,$01,$00,$01,$00,$08,$10,$18,$00,$40,$00,$C0,$F4,$00,$0C,$00; 27:B1E5
    .db $08,$00,$08,$00,$08,$00,$08,$01,$08,$00,$08,$01,$08,$02,$08,$03; 27:B1F5
    .db $F8,$03,$F8,$02,$F8,$01,$F8,$00,$F8,$01,$F8,$00,$F8,$00,$F8,$00; 27:B205
    .db $F8,$00,$F8,$00,$F8,$00,$F8,$FF,$F8,$00,$F8,$FF,$F8,$FE,$F8,$FD; 27:B215
    .db $08,$FD,$08,$FE,$08,$FF,$08,$00,$08,$FF,$08,$00,$08,$00,$08,$00; 27:B225
sub_27_B235:
    LDA obj_timer,X                     ; B235  bd 80 04
    ORA #$80                            ; B238  09 80
    STA obj_timer,X                     ; B23A  9d 80 04
    INC obj_timer2,X                    ; B23D  fe 20 06
    LDA obj_timer2,X                    ; B240  bd 20 06
    AND obj_act_timer,X                 ; B243  3d 40 06
    BNE loc_27_B26B                     ; B246  d0 23
    DEC orbit_level,X                   ; B248  de c0 06
    BNE loc_27_B250                     ; B24B  d0 03
    JMP ObjDespawn                      ; B24D  4c c7 98
loc_27_B250:
    TXA                                 ; B250  8a
    AND #$07                            ; B251  29 07
    ASL A                               ; B253  0a
    CLC                                 ; B254  18
    ADC obj_phase,X                     ; B255  7d 00 06
    AND #$0F                            ; B258  29 0f
    STA obj_meta_dir,X                  ; B25A  9d 60 03
    INC obj_phase,X                     ; B25D  fe 00 06
    LDA obj_phase,X                     ; B260  bd 00 06
    AND #$0F                            ; B263  29 0f
    BNE loc_27_B26B                     ; B265  d0 04
    SEC                                 ; B267  38
    ROL obj_act_timer,X                 ; B268  3e 40 06
loc_27_B26B:
    JSR ObjPosToTemp                    ; B26B  20 a8 98
    LDA obj_meta_dir,X                  ; B26E  bd 60 03
    JSR ObjStepVector                   ; B271  20 80 84
    JSR TempAddStep                     ; B274  20 61 98
    JMP TempToObjPos                    ; B277  4c 97 98
sub_27_B27A:
    DEC obj_timer,X                     ; B27A  de 80 04
    BNE loc_27_B282                     ; B27D  d0 03
    JMP ObjDespawn                      ; B27F  4c c7 98
loc_27_B282:
    LDA obj_timer,X                     ; B282  bd 80 04
    AND #$01                            ; B285  29 01
    BNE loc_27_B292                     ; B287  d0 09
    LDY obj_meta_dir,X                  ; B289  bc 60 03
    LDA obj_shot_id,X                   ; B28C  bd 40 04
    JSR SpawnShot                       ; B28F  20 2d 97
loc_27_B292:
    DEC obj_anim,X                      ; B292  de e0 04
    JSR ObjPosToTemp                    ; B295  20 a8 98
    LDA obj_meta_dir,X                  ; B298  bd 60 03
    JSR ObjStepVector                   ; B29B  20 80 84
    JSR TempAddStep                     ; B29E  20 61 98
    JMP TempToObjPos                    ; B2A1  4c 97 98
sub_27_B2A4:
    DEC obj_timer2,X                    ; B2A4  de 20 06
    LDA obj_timer2,X                    ; B2A7  bd 20 06
    LSR A                               ; B2AA  4a
    BCC loc_27_B2AE                     ; B2AB  90 01
    RTS                                 ; B2AD  60
loc_27_B2AE:
    LDA obj_phase,X                     ; B2AE  bd 00 06
    CMP #$07                            ; B2B1  c9 07
    BNE loc_27_B2BA                     ; B2B3  d0 05
    LDA #$00                            ; B2B5  a9 00
    STA obj_active,X                    ; B2B7  9d a0 04
loc_27_B2BA:
    LDY obj_phase,X                     ; B2BA  bc 00 06
    INC obj_phase,X                     ; B2BD  fe 00 06
    LDA #$17                            ; B2C0  a9 17
    JMP SpawnShot                       ; B2C2  4c 2d 97
AiCountdown:  ; AI $14: obj_anim -= $0600 per frame; borrow -> 27:98C7
    LDA obj_anim,X                      ; B2C5  bd e0 04
    SEC                                 ; B2C8  38
    SBC obj_phase,X                     ; B2C9  fd 00 06
    STA obj_anim,X                      ; B2CC  9d e0 04
    BCS loc_27_B2D4                     ; B2CF  b0 03
    JMP ObjDespawn                      ; B2D1  4c c7 98
loc_27_B2D4:
    RTS                                 ; B2D4  60
sub_27_B2D5:
    INC obj_timer2,X                    ; B2D5  fe 20 06
    LDA obj_meta_dir,X                  ; B2D8  bd 60 03
    LSR A                               ; B2DB  4a
    ORA obj_meta_id,X                   ; B2DC  1d 00 03
    STA obj_meta_id,X                   ; B2DF  9d 00 03
sub_27_B2E2:
    LDA obj_meta_dir,X                  ; B2E2  bd 60 03
    ASL A                               ; B2E5  0a
    CLC                                 ; B2E6  18
    ADC $B2F1,X                         ; B2E7  7d f1 b2
    AND #$0F                            ; B2EA  29 0f
    STA obj_meta_dir,X                  ; B2EC  9d 60 03
    LDA #$10                            ; B2EF  a9 10
    STA obj_active,X                    ; B2F1  9d a0 04
sub_27_B2F4:
    RTS                                 ; B2F4  60
    .db $00,$FF,$01,$FE,$02,$FD,$03     ; 27:B2F5
sub_27_B2FC:
    LDX #$00                            ; B2FC  a2 00
    JSR ObjPosToTemp                    ; B2FE  20 a8 98
    LDX #$0C                            ; B301  a2 0c
    JSR TempToObjPos                    ; B303  20 97 98
    LDA obj_anim,X                      ; B306  bd e0 04
    SEC                                 ; B309  38
    SBC obj_timer2,X                    ; B30A  fd 20 06
    STA obj_anim,X                      ; B30D  9d e0 04
    BPL loc_27_B317                     ; B310  10 05
    LDA #$00                            ; B312  a9 00
    STA obj_active,X                    ; B314  9d a0 04
loc_27_B317:
    RTS                                 ; B317  60
sub_27_B318:
    DEC obj_anim,X                      ; B318  de e0 04
    LDA obj_anim,X                      ; B31B  bd e0 04
    AND #$07                            ; B31E  29 07
    BEQ loc_27_B323                     ; B320  f0 01
loc_27_B322:
    RTS                                 ; B322  60
loc_27_B323:
    LDA obj_flags,X                     ; B323  bd 80 03
    BMI loc_27_B322                     ; B326  30 fa
    LDA obj_x_lo,X                      ; B328  b5 70
    ROL A                               ; B32A  2a
    ROL A                               ; B32B  2a
    ROL A                               ; B32C  2a
    ROL A                               ; B32D  2a
    AND #$04                            ; B32E  29 04
    ORA #$02                            ; B330  09 02
    TAY                                 ; B332  a8
    LDA obj_shot_id,X                   ; B333  bd 40 04
    JMP SpawnShot                       ; B336  4c 2d 97
AiShellOnHit:  ; s47: AI $41: hit -> 63 frames closed (no coll group) then AI $C1; else AiShooterChase
    LDA obj_param2,X                    ; B339  bd a0 06
    BNE loc_27_B364                     ; B33C  d0 26
    LDA obj_flags2,X                    ; B33E  bd 40 03
    BMI loc_27_B346                     ; B341  30 03
    JMP AiShooterChase                  ; B343  4c e4 a9
loc_27_B346:
    LDA obj_meta_id,X                   ; B346  bd 00 03
    STA obj_param1,X                    ; B349  9d 80 06
    LDA #$41                            ; B34C  a9 41
    STA obj_active,X                    ; B34E  9d a0 04
    LDA obj_coll,X                      ; B351  bd a0 03
    AND #$0F                            ; B354  29 0f
    STA obj_coll,X                      ; B356  9d a0 03
    LDA #$3F                            ; B359  a9 3f
    STA obj_param2,X                    ; B35B  9d a0 06
    LDA #$97                            ; B35E  a9 97
    STA obj_meta_id,X                   ; B360  9d 00 03
    RTS                                 ; B363  60
loc_27_B364:
    DEC obj_param2,X                    ; B364  de a0 06
    BEQ loc_27_B383                     ; B367  f0 1a
    CMP #$1F                            ; B369  c9 1f
    BCC loc_27_B37D                     ; B36B  90 10
    LDA obj_param2,X                    ; B36D  bd a0 06
    STA obj_anim,X                      ; B370  9d e0 04
    CMP #$20                            ; B373  c9 20
    BNE loc_27_B37C                     ; B375  d0 05
    LDA #$08                            ; B377  a9 08
    STA obj_freeze,X                    ; B379  9d a0 05
loc_27_B37C:
    RTS                                 ; B37C  60
loc_27_B37D:
    EOR #$FF                            ; B37D  49 ff
    STA obj_anim,X                      ; B37F  9d e0 04
    RTS                                 ; B382  60
loc_27_B383:
    LDA obj_coll,X                      ; B383  bd a0 03
    ORA #$10                            ; B386  09 10
    STA obj_coll,X                      ; B388  9d a0 03
    LDA #$00                            ; B38B  a9 00
    STA obj_param2,X                    ; B38D  9d a0 06
    LDA #$C1                            ; B390  a9 c1
    STA obj_active,X                    ; B392  9d a0 04
    LDA obj_param1,X                    ; B395  bd 80 06
    STA obj_meta_id,X                   ; B398  9d 00 03
    JMP AiShooterChase                  ; B39B  4c e4 a9
AiWander16:  ; AI $40 (types $55 $7C): new 16-way direction every $D0 frames from 27:B41E + position
    JSR AiOffscreenHalf                 ; B39E  20 69 b7
    LDA obj_phase,X                     ; B3A1  bd 00 06
    BNE loc_27_B3D0                     ; B3A4  d0 2a
    LDA #$D0                            ; B3A6  a9 d0
    STA obj_phase,X                     ; B3A8  9d 00 06
    LDY #$01                            ; B3AB  a0 01
    JSR DirToTarget                     ; B3AD  20 09 84
    LDA tmp_20                          ; B3B0  a5 20
    ASL A                               ; B3B2  0a
    STA jmp_trampoline                  ; B3B3  85 10
    LDA obj_x_lo                        ; B3B5  a5 70
    CLC                                 ; B3B7  18
    ADC obj_y_lo                        ; B3B8  65 b0
    CLC                                 ; B3BA  18
    ADC obj_timer                       ; B3BB  6d 80 04
    AND #$03                            ; B3BE  29 03
    TAY                                 ; B3C0  a8
    LDA $B41E,Y                         ; B3C1  b9 1e b4
    CLC                                 ; B3C4  18
    ADC jmp_trampoline                  ; B3C5  65 10
    AND #$0F                            ; B3C7  29 0f
    STA obj_meta_dir,X                  ; B3C9  9d 60 03
    DEC obj_phase,X                     ; B3CC  de 00 06
    RTS                                 ; B3CF  60
loc_27_B3D0:
    BMI loc_27_B422                     ; B3D0  30 50
    DEC obj_anim,X                      ; B3D2  de e0 04
    DEC obj_anim,X                      ; B3D5  de e0 04
    LDA obj_anim,X                      ; B3D8  bd e0 04
    AND #$06                            ; B3DB  29 06
    BNE loc_27_B3F9                     ; B3DD  d0 1a
    LDA obj_anim,X                      ; B3DF  bd e0 04
    LSR A                               ; B3E2  4a
    LSR A                               ; B3E3  4a
    LSR A                               ; B3E4  4a
    STA jmp_trampoline                  ; B3E5  85 10
    TXA                                 ; B3E7  8a
    CLC                                 ; B3E8  18
    ADC jmp_trampoline                  ; B3E9  65 10
    AND #$0F                            ; B3EB  29 0f
    TAY                                 ; B3ED  a8
    LDA $B40E,Y                         ; B3EE  b9 0e b4
    STA obj_meta_dir,X                  ; B3F1  9d 60 03
    LDA #$0C                            ; B3F4  a9 0c
    STA obj_flags2,X                    ; B3F6  9d 40 03
loc_27_B3F9:
    DEC obj_timer,X                     ; B3F9  de 80 04
    JSR ObjPosToTemp                    ; B3FC  20 a8 98
    LDA obj_meta_dir,X                  ; B3FF  bd 60 03
    JSR ObjStepVector                   ; B402  20 80 84
    JSR TempAddStep                     ; B405  20 61 98
    DEC obj_phase,X                     ; B408  de 00 06
    JMP TempToObjPos                    ; B40B  4c 97 98
    .db $04,$03,$0C,$07,$0A,$01,$05,$0D,$0E,$00,$02,$0B,$0F,$06,$09,$08; 27:B40E
    .db $FF,$00,$01,$00                 ; 27:B41E
loc_27_B422:
    DEC obj_anim,X                      ; B422  de e0 04
    DEC obj_anim,X                      ; B425  de e0 04
    DEC obj_anim,X                      ; B428  de e0 04
    DEC obj_phase,X                     ; B42B  de 00 06
    LDA #$0E                            ; B42E  a9 0e
    STA obj_flags2,X                    ; B430  9d 40 03
    JMP loc_27_B3F9                     ; B433  4c f9 b3
AiDasher:  ; s47: AI $44: random 4-dir walk; 2/8 picks dash at speed 6 (meta $92)
    LDA obj_flags2,X                    ; B436  bd 40 03
    AND #$F0                            ; B439  29 f0
    BNE loc_27_B479                     ; B43B  d0 3c
    JSR AiOffscreenHalf                 ; B43D  20 69 b7
    LDA obj_phase,X                     ; B440  bd 00 06
    BEQ loc_27_B44E                     ; B443  f0 09
    DEC obj_phase,X                     ; B445  de 00 06
    LDA #$00                            ; B448  a9 00
    STA obj_anim,X                      ; B44A  9d e0 04
    RTS                                 ; B44D  60
loc_27_B44E:
    LDA obj_timer,X                     ; B44E  bd 80 04
    BEQ loc_27_B479                     ; B451  f0 26
    LDA obj_flags2,X                    ; B453  bd 40 03
    CMP #$06                            ; B456  c9 06
    BNE loc_27_B462                     ; B458  d0 08
    LDA #$92                            ; B45A  a9 92
    STA obj_meta_id,X                   ; B45C  9d 00 03
    DEC obj_anim,X                      ; B45F  de e0 04
loc_27_B462:
    DEC obj_anim,X                      ; B462  de e0 04
    DEC obj_timer,X                     ; B465  de 80 04
    JSR ObjPosToTemp                    ; B468  20 a8 98
    LDA obj_meta_dir,X                  ; B46B  bd 60 03
    JSR ObjStepVector                   ; B46E  20 80 84
    JSR TempAddStep                     ; B471  20 61 98
    JSR ObjMoveSlide                    ; B474  20 d7 98
    BCC loc_27_B4A9                     ; B477  90 30
loc_27_B479:
    JSR ObjPosToTemp                    ; B479  20 a8 98
    LDA #$40                            ; B47C  a9 40
    JSR ObjBgProbe                      ; B47E  20 30 9a
    JSR Rand64                          ; B481  20 d7 97
    AND #$06                            ; B484  29 06
    STA obj_meta_dir,X                  ; B486  9d 60 03
    LDA #$65                            ; B489  a9 65
    STA obj_meta_id,X                   ; B48B  9d 00 03
    JSR Rand64                          ; B48E  20 d7 97
    TAY                                 ; B491  a8
    LDA $B4AA,Y                         ; B492  b9 aa b4
    STA obj_timer,X                     ; B495  9d 80 04
    LDA $B4B2,Y                         ; B498  b9 b2 b4
    STA obj_flags2,X                    ; B49B  9d 40 03
    LDA tmp_08                          ; B49E  a5 08
    AND #$07                            ; B4A0  29 07
    SEC                                 ; B4A2  38
    ASL A                               ; B4A3  0a
    ASL A                               ; B4A4  0a
    ASL A                               ; B4A5  0a
    STA obj_phase,X                     ; B4A6  9d 00 06
loc_27_B4A9:
    RTS                                 ; B4A9  60
    .db $20,$28,$30,$38,$40,$48,$50,$58,$01,$01,$01,$06,$01,$06,$01,$01; 27:B4AA
AiHomingFlyer:  ; s47: AI $45: SteerToward player every 16 steps; speed level 0-3; no BG test; random aimed shot
    LDA obj_spawn_param,X               ; B4BA  bd 60 05
    CMP #$01                            ; B4BD  c9 01
    BNE loc_27_B4D3                     ; B4BF  d0 12
    LDA obj_spawn_param,X               ; B4C1  bd 60 05
    CMP #$01                            ; B4C4  c9 01
    BNE loc_27_B4D3                     ; B4C6  d0 0b
    LDA obj_active_s0D                  ; B4C8  ad ad 04
    BMI loc_27_B4D3                     ; B4CB  30 06
    LDA #$00                            ; B4CD  a9 00
    STA obj_active,X                    ; B4CF  9d a0 04
    RTS                                 ; B4D2  60
loc_27_B4D3:
    JSR AiOffscreenHalf                 ; B4D3  20 69 b7
    LDA obj_flags,X                     ; B4D6  bd 80 03
    AND #$EF                            ; B4D9  29 ef
    STA obj_flags,X                     ; B4DB  9d 80 03
    LDA obj_timer,X                     ; B4DE  bd 80 04
    AND #$0F                            ; B4E1  29 0f
    BNE loc_27_B50E                     ; B4E3  d0 29
loc_27_B4E5:
    LDY #$01                            ; B4E5  a0 01
    JSR DirToTarget                     ; B4E7  20 09 84
    LDA tmp_20                          ; B4EA  a5 20
    ASL A                               ; B4EC  0a
    JSR SteerToward                     ; B4ED  20 3e b7
    INC obj_phase,X                     ; B4F0  fe 00 06
    BIT jmp_trampoline                  ; B4F3  24 10
    BEQ loc_27_B4FD                     ; B4F5  f0 06
    DEC obj_phase,X                     ; B4F7  de 00 06
    DEC obj_phase,X                     ; B4FA  de 00 06
loc_27_B4FD:
    LDY obj_phase,X                     ; B4FD  bc 00 06
    INY                                 ; B500  c8
    LDA $B550,Y                         ; B501  b9 50 b5
    STA obj_phase,X                     ; B504  9d 00 06
    TAY                                 ; B507  a8
    LDA $B54C,Y                         ; B508  b9 4c b5
    STA obj_flags2,X                    ; B50B  9d 40 03
loc_27_B50E:
    LDY obj_phase,X                     ; B50E  bc 00 06
    LDA obj_anim,X                      ; B511  bd e0 04
    SEC                                 ; B514  38
    SBC $B556,Y                         ; B515  f9 56 b5
    STA obj_anim,X                      ; B518  9d e0 04
    DEC obj_timer,X                     ; B51B  de 80 04
    JSR ObjPosToTemp                    ; B51E  20 a8 98
    LDA obj_meta_dir,X                  ; B521  bd 60 03
    JSR ObjStepVector                   ; B524  20 80 84
    JSR TempAddStep                     ; B527  20 61 98
    JSR TempToObjPos                    ; B52A  20 97 98
    LDA obj_shot_id,X                   ; B52D  bd 40 04
    BEQ loc_27_B54B                     ; B530  f0 19
    LDA tmp_08                          ; B532  a5 08
    AND #$07                            ; B534  29 07
    BNE loc_27_B54B                     ; B536  d0 13
    JSR Rand64                          ; B538  20 d7 97
    BNE loc_27_B54B                     ; B53B  d0 0e
    LDY #$01                            ; B53D  a0 01
    JSR DirToTarget                     ; B53F  20 09 84
    LDA tmp_20                          ; B542  a5 20
    TAY                                 ; B544  a8
    LDA obj_shot_id,X                   ; B545  bd 40 04
    JMP SpawnShot                       ; B548  4c 2d 97
loc_27_B54B:
    RTS                                 ; B54B  60
    .db $0E,$0F,$0D,$0C,$00,$00,$01,$02,$03,$03,$FC,$FD,$FE,$FF; 27:B54C
sub_27_B55A:
    LDA obj_act_timer,X                 ; B55A  bd 40 06
    BEQ loc_27_B586                     ; B55D  f0 27
    DEC obj_act_timer,X                 ; B55F  de 40 06
    LDA obj_anim,X                      ; B562  bd e0 04
    SEC                                 ; B565  38
    SBC #$02                            ; B566  e9 02
    STA obj_anim,X                      ; B568  9d e0 04
    LDA obj_act_timer,X                 ; B56B  bd 40 06
    CMP #$18                            ; B56E  c9 18
    BEQ loc_27_B573                     ; B570  f0 01
    RTS                                 ; B572  60
loc_27_B573:
    LDY #$01                            ; B573  a0 01
    JSR DirToTarget                     ; B575  20 09 84
    LDA tmp_20                          ; B578  a5 20
    ASL A                               ; B57A  0a
    STA obj_meta_dir,X                  ; B57B  9d 60 03
    LDY tmp_20                          ; B57E  a4 20
    LDA obj_shot_id,X                   ; B580  bd 40 04
    JMP SpawnShot                       ; B583  4c 2d 97
loc_27_B586:
    JSR AiShootCheck                    ; B586  20 14 b7
    JSR AiHomingFlyer                   ; B589  20 ba b4
    LDA obj_timer,X                     ; B58C  bd 80 04
    BNE loc_27_B594                     ; B58F  d0 03
    JMP loc_27_B4E5                     ; B591  4c e5 b4
loc_27_B594:
    RTS                                 ; B594  60
AiFanTurret:  ; s47: AI $4C/$4D: static; 3-shot fan (16-dir aimed -1/0/+1) at $0640=$18/$10/$08
    LDA obj_act_timer,X                 ; B595  bd 40 06
    BEQ AiFanTurretIdle                 ; B598  f0 39
    DEC obj_act_timer,X                 ; B59A  de 40 06
    LDA obj_act_timer,X                 ; B59D  bd 40 06
    ASL A                               ; B5A0  0a
    STA obj_anim,X                      ; B5A1  9d e0 04
    AND #$F0                            ; B5A4  29 f0
    BEQ loc_27_B5AF                     ; B5A6  f0 07
    LDA obj_anim,X                      ; B5A8  bd e0 04
    AND #$0F                            ; B5AB  29 0f
    BEQ loc_27_B5B0                     ; B5AD  f0 01
loc_27_B5AF:
    RTS                                 ; B5AF  60
loc_27_B5B0:
    LDY #$01                            ; B5B0  a0 01
    JSR DirToTarget                     ; B5B2  20 09 84
    LDA obj_act_timer,X                 ; B5B5  bd 40 06
    AND #$18                            ; B5B8  29 18
    LSR A                               ; B5BA  4a
    LSR A                               ; B5BB  4a
    LSR A                               ; B5BC  4a
    TAY                                 ; B5BD  a8
    LDA FanShotOfs,Y                    ; B5BE  b9 cf b5
    ASL tmp_20                          ; B5C1  06 20
    CLC                                 ; B5C3  18
    ADC tmp_20                          ; B5C4  65 20
    AND #$0F                            ; B5C6  29 0f
    TAY                                 ; B5C8  a8
    LDA obj_shot_id,X                   ; B5C9  bd 40 04
    JMP SpawnShot                       ; B5CC  4c 2d 97
FanShotOfs:  ; s55: s8[4] 00 FF 00 01 by ($0640&$18)>>3: fan shots at $0640=$08/$10/$18 = -1/0/+1 (16-dir units)
    .db $00,$FF,$00,$01                 ; 27:B5CF
AiFanTurretIdle:  ; s55: obj_anim--; JMP AiShootCheck: the tail JMP makes its PLA PLA pop ObjUpdateAll's return (slots after it skip their AI that frame)
    DEC obj_anim,X                      ; B5D3  de e0 04
    JMP AiShootCheck                    ; B5D6  4c 14 b7
AiTurret:  ; s47: AI $4E: static; aimed shot every 16 obj_anim steps
    DEC obj_anim,X                      ; B5D9  de e0 04
    LDA obj_anim,X                      ; B5DC  bd e0 04
    AND #$0F                            ; B5DF  29 0f
    BNE loc_27_B5F2                     ; B5E1  d0 0f
    LDY #$01                            ; B5E3  a0 01
    JSR DirToTarget                     ; B5E5  20 09 84
    LDA tmp_20                          ; B5E8  a5 20
    ASL A                               ; B5EA  0a
    TAY                                 ; B5EB  a8
    LDA obj_shot_id,X                   ; B5EC  bd 40 04
    JMP SpawnShot                       ; B5EF  4c 2d 97
loc_27_B5F2:
    RTS                                 ; B5F2  60
AiWaitClear:  ; s47: AI $5E: wakes (AI $2A) when no spawn-param $50 object left on player's screen row
    TXA                                 ; B5F3  8a
    ASL A                               ; B5F4  0a
    ASL A                               ; B5F5  0a
    ASL A                               ; B5F6  0a
    ASL A                               ; B5F7  0a
    CLC                                 ; B5F8  18
    ADC tmp_08                          ; B5F9  65 08
    AND #$3F                            ; B5FB  29 3f
    BNE loc_27_B625                     ; B5FD  d0 26
    LDA #$50                            ; B5FF  a9 50
    LDY #$0D                            ; B601  a0 0d
loc_27_B603:
    LDA obj_active,Y                    ; B603  b9 a0 04
    BEQ loc_27_B616                     ; B606  f0 0e
    LDA a:obj_y_hi,Y                    ; B608  b9 d0 00
    CMP obj_y_hi                        ; B60B  c5 d0
    BNE loc_27_B616                     ; B60D  d0 07
    LDA obj_spawn_param,Y               ; B60F  b9 60 05
    CMP #$50                            ; B612  c9 50
    BEQ loc_27_B625                     ; B614  f0 0f
loc_27_B616:
    INY                                 ; B616  c8
    CPY #$1F                            ; B617  c0 1f
    BNE loc_27_B603                     ; B619  d0 e8
    LDA #$2A                            ; B61B  a9 2a
    STA obj_active,X                    ; B61D  9d a0 04
    LDA #$10                            ; B620  a9 10
    STA obj_coll,X                      ; B622  9d a0 03
loc_27_B625:
    RTS                                 ; B625  60
AiPatrolGunner:  ; s47: AI $5C: horizontal patrol (elem $0F) / attack burst (elem 0) of aimed shots
    LDA #$0F                            ; B626  a9 0f
    STA obj_elem_drop,X                 ; B628  9d 00 05
    LDA obj_timer2,X                    ; B62B  bd 20 06
    BNE loc_27_B66E                     ; B62E  d0 3e
    LDA #$00                            ; B630  a9 00
    STA obj_anim,X                      ; B632  9d e0 04
    LDA obj_x_lo,X                      ; B635  b5 70
    CMP #$36                            ; B637  c9 36
    BCC loc_27_B63F                     ; B639  90 04
    CMP #$E8                            ; B63B  c9 e8
    BCC loc_27_B64A                     ; B63D  90 0b
loc_27_B63F:
    LDA obj_meta_dir,X                  ; B63F  bd 60 03
    CLC                                 ; B642  18
    ADC #$04                            ; B643  69 04
    AND #$07                            ; B645  29 07
    STA obj_meta_dir,X                  ; B647  9d 60 03
loc_27_B64A:
    DEC obj_timer,X                     ; B64A  de 80 04
    LDA obj_timer,X                     ; B64D  bd 80 04
    AND #$1F                            ; B650  29 1f
    BNE loc_27_B65F                     ; B652  d0 0b
    JSR Rand64                          ; B654  20 d7 97
    LSR A                               ; B657  4a
    BCC loc_27_B65F                     ; B658  90 05
    LDA #$20                            ; B65A  a9 20
    STA obj_timer2,X                    ; B65C  9d 20 06
loc_27_B65F:
    LDA obj_meta_dir,X                  ; B65F  bd 60 03
    JSR ObjStepVector                   ; B662  20 80 84
    JSR ObjPosToTemp                    ; B665  20 a8 98
    JSR TempAddStep                     ; B668  20 61 98
    JMP TempToObjPos                    ; B66B  4c 97 98
loc_27_B66E:
    LDA #$00                            ; B66E  a9 00
    STA obj_elem_drop,X                 ; B670  9d 00 05
    LDA #$08                            ; B673  a9 08
    STA obj_anim,X                      ; B675  9d e0 04
    DEC obj_timer2,X                    ; B678  de 20 06
    LDA obj_timer2,X                    ; B67B  bd 20 06
    AND #$07                            ; B67E  29 07
    BNE loc_27_B68F                     ; B680  d0 0d
    LDY #$01                            ; B682  a0 01
    JSR DirToTarget                     ; B684  20 09 84
    LDY tmp_20                          ; B687  a4 20
    LDA obj_shot_id,X                   ; B689  bd 40 04
    JMP SpawnShot                       ; B68C  4c 2d 97
loc_27_B68F:
    RTS                                 ; B68F  60
AiDrifter:  ; s47: AI $5D: re-aim every 64 steps; straight move no BG test
    DEC obj_anim,X                      ; B690  de e0 04
    DEC obj_timer,X                     ; B693  de 80 04
    LDA obj_timer,X                     ; B696  bd 80 04
    AND #$3F                            ; B699  29 3f
    BNE loc_27_B6A7                     ; B69B  d0 0a
    LDY #$01                            ; B69D  a0 01
    JSR DirToTarget                     ; B69F  20 09 84
    LDA tmp_20                          ; B6A2  a5 20
    STA obj_meta_dir,X                  ; B6A4  9d 60 03
loc_27_B6A7:
    JMP loc_27_B65F                     ; B6A7  4c 5f b6
AiOrbiter:  ; s47: AI $58: circles slot $0D (circle table 27:B6F4 r=24); random aimed shot
    TXA                                 ; B6AA  8a
    ASL A                               ; B6AB  0a
    ASL A                               ; B6AC  0a
    ASL A                               ; B6AD  0a
    CLC                                 ; B6AE  18
    ADC tmp_08                          ; B6AF  65 08
    AND #$1F                            ; B6B1  29 1f
    TAY                                 ; B6B3  a8
    LDA $B6F4,Y                         ; B6B4  b9 f4 b6
    STA tmp_30                          ; B6B7  85 30
    TYA                                 ; B6B9  98
    CLC                                 ; B6BA  18
    ADC #$F8                            ; B6BB  69 f8
    AND #$1F                            ; B6BD  29 1f
    TAY                                 ; B6BF  a8
    LDA $B6F4,Y                         ; B6C0  b9 f4 b6
    SEC                                 ; B6C3  38
    SBC #$08                            ; B6C4  e9 08
    STA tmp_31                          ; B6C6  85 31
    STX jmp_trampoline                  ; B6C8  86 10
    LDX #$0D                            ; B6CA  a2 0d
    JSR ObjPosToTemp                    ; B6CC  20 a8 98
    LDX jmp_trampoline                  ; B6CF  a6 10
    JSR TempAddOffset                   ; B6D1  20 24 98
    JSR TempToObjPos                    ; B6D4  20 97 98
    DEC obj_anim,X                      ; B6D7  de e0 04
    LDA obj_anim,X                      ; B6DA  bd e0 04
    AND #$3F                            ; B6DD  29 3f
    BNE loc_27_B6F3                     ; B6DF  d0 12
    JSR Rand64                          ; B6E1  20 d7 97
    BNE loc_27_B6F3                     ; B6E4  d0 0d
    LDY #$01                            ; B6E6  a0 01
    JSR DirToTarget                     ; B6E8  20 09 84
    LDY tmp_20                          ; B6EB  a4 20
    LDA obj_shot_id,X                   ; B6ED  bd 40 04
    JMP SpawnShot                       ; B6F0  4c 2d 97
loc_27_B6F3:
    RTS                                 ; B6F3  60
    .db $18,$16,$14,$11,$0E,$0A,$05,$00,$FB,$F6,$F1,$EF,$EC,$EA,$E8,$E7; 27:B6F4
    .db $E8,$EA,$EC,$EF,$F1,$F6,$FB,$00,$05,$0A,$0E,$11,$14,$16,$18,$19; 27:B704
AiShootCheck:  ; s47: onscreen; every 32 frames per slot; Rand64=0; player < 5 cells -> $0640=$1F and skip caller
    LDA obj_flags,X                     ; B714  bd 80 03
    BMI loc_27_B73D                     ; B717  30 24
    TXA                                 ; B719  8a
    ASL A                               ; B71A  0a
    ASL A                               ; B71B  0a
    ASL A                               ; B71C  0a
    ASL A                               ; B71D  0a
    CLC                                 ; B71E  18
    ADC tmp_08                          ; B71F  65 08
    AND #$1F                            ; B721  29 1f
    BNE loc_27_B73D                     ; B723  d0 18
    JSR Rand64                          ; B725  20 d7 97
loc_27_B728:
    BNE loc_27_B73D                     ; B728  d0 13
    LDY #$01                            ; B72A  a0 01
    JSR DirToTarget                     ; B72C  20 09 84
    LDA tmp_21                          ; B72F  a5 21
    CMP #$05                            ; B731  c9 05
    BCC loc_27_B736                     ; B733  90 01
    RTS                                 ; B735  60
loc_27_B736:
    LDA #$1F                            ; B736  a9 1f
    STA obj_act_timer,X                 ; B738  9d 40 06
    PLA                                 ; B73B  68
    PLA                                 ; B73C  68
loc_27_B73D:
    RTS                                 ; B73D  60
SteerToward:  ; s47: A=target heading 0-15; heading += 27:B759[(heading-target)&F] (0/-1/+1)
    EOR #$FF                            ; B73E  49 ff
    CLC                                 ; B740  18
    ADC #$01                            ; B741  69 01
    CLC                                 ; B743  18
    ADC obj_meta_dir,X                  ; B744  7d 60 03
    AND #$0F                            ; B747  29 0f
    TAY                                 ; B749  a8
    LDA $B759,Y                         ; B74A  b9 59 b7
    STA jmp_trampoline                  ; B74D  85 10
    CLC                                 ; B74F  18
    ADC obj_meta_dir,X                  ; B750  7d 60 03
    AND #$0F                            ; B753  29 0f
    STA obj_meta_dir,X                  ; B755  9d 60 03
    RTS                                 ; B758  60
    .db $00,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$01,$01,$01,$01,$01,$01,$01,$01; 27:B759
AiOffscreenHalf:  ; s47: offscreen and (X+$08) odd -> skip caller (half rate)
    LDA obj_flags,X                     ; B769  bd 80 03
    BPL loc_27_B777                     ; B76C  10 09
    TXA                                 ; B76E  8a
    CLC                                 ; B76F  18
    ADC tmp_08                          ; B770  65 08
    LSR A                               ; B772  4a
    BCC loc_27_B777                     ; B773  90 02
    PLA                                 ; B775  68
    PLA                                 ; B776  68
loc_27_B777:
    RTS                                 ; B777  60
sub_27_B778:
    DEC obj_anim,X                      ; B778  de e0 04
    DEC obj_anim,X                      ; B77B  de e0 04
    DEC obj_timer,X                     ; B77E  de 80 04
    BNE loc_27_B786                     ; B781  d0 03
    JMP ObjBecomeNext                   ; B783  4c cd 98
loc_27_B786:
    LDA obj_timer,X                     ; B786  bd 80 04
    CMP #$30                            ; B789  c9 30
    BCS loc_27_B798                     ; B78B  b0 0b
    DEC obj_anim,X                      ; B78D  de e0 04
    LDA obj_flags,X                     ; B790  bd 80 03
    ORA #$04                            ; B793  09 04
    STA obj_flags,X                     ; B795  9d 80 03
loc_27_B798:
    LDA obj_timer,X                     ; B798  bd 80 04
    CMP #$C0                            ; B79B  c9 c0
    BCC loc_27_B7C5                     ; B79D  90 26
ChestHop:  ; s47: A=counter; dy from nibble table 27:B7C6 -> 27:B7E6; moves y only
    STA jmp_trampoline                  ; B79F  85 10
    LSR A                               ; B7A1  4a
    AND #$1F                            ; B7A2  29 1f
    TAY                                 ; B7A4  a8
    LDA $B7C6,Y                         ; B7A5  b9 c6 b7
    LSR jmp_trampoline                  ; B7A8  46 10
    BCS loc_27_B7B0                     ; B7AA  b0 04
    LSR A                               ; B7AC  4a
    LSR A                               ; B7AD  4a
    LSR A                               ; B7AE  4a
    LSR A                               ; B7AF  4a
loc_27_B7B0:
    AND #$0F                            ; B7B0  29 0f
    TAY                                 ; B7B2  a8
    JSR ObjPosToTemp                    ; B7B3  20 a8 98
    LDA $B7E6,Y                         ; B7B6  b9 e6 b7
    STA tmp_31                          ; B7B9  85 31
    LDA #$00                            ; B7BB  a9 00
    STA tmp_30                          ; B7BD  85 30
    JSR TempAddStep                     ; B7BF  20 61 98
    JMP TempToObjPos                    ; B7C2  4c 97 98
loc_27_B7C5:
    RTS                                 ; B7C5  60
    .db $00,$00,$00,$01,$0F,$21,$00,$FE,$32,$10,$0F,$EC,$43,$32,$21,$10; 27:B7C6
    .db $0F,$FE,$ED,$DC,$76,$54,$33,$32,$11,$10,$0F,$FF,$ED,$DD,$CB,$A9; 27:B7D6
    .db $00,$01,$02,$03,$04,$05,$06,$07,$F8,$F9,$FA,$FB,$FC,$FD,$FE,$FF; 27:B7E6
sub_27_B7F6:
    JSR sub_27_B850                     ; B7F6  20 50 b8
    DEC obj_act_timer,X                 ; B7F9  de 40 06
    LDA obj_act_timer,X                 ; B7FC  bd 40 06
    CMP #$0A                            ; B7FF  c9 0a
    BCS loc_27_B83A                     ; B801  b0 37
    LDA #$28                            ; B803  a9 28
loc_27_B805:
    STA tmp_1F                          ; B805  85 1f
    JSR MarkScreenVisited               ; B807  20 5c c7
    JSR WaitPpuQueue                    ; B80A  20 76 c6
    TXA                                 ; B80D  8a
    PHA                                 ; B80E  48
    JSR ObjPosToTemp                    ; B80F  20 a8 98
    LDA obj_act_timer,X                 ; B812  bd 40 06
    PHA                                 ; B815  48
    ASL A                               ; B816  0a
    ASL A                               ; B817  0a
    ASL A                               ; B818  0a
    SEC                                 ; B819  38
    SBC tmp_1F                          ; B81A  e5 1f
    STA tmp_31                          ; B81C  85 31
    JSR TempAddOffset                   ; B81E  20 24 98
    LDA #$FF                            ; B821  a9 ff
    STA tmp_30                          ; B823  85 30
    STA tmp_31                          ; B825  85 31
    LDA zp_3C                           ; B827  a5 3c
    STA pos_x_lo                        ; B829  85 34
    LDA zp_3D                           ; B82B  a5 3d
    STA pos_x_hi                        ; B82D  85 35
    JSR FarCall_EB91                    ; B82F  20 9d ff
    PLA                                 ; B832  68
    BNE loc_27_B838                     ; B833  d0 03
    JSR sub_30_C739                     ; B835  20 39 c7
loc_27_B838:
    PLA                                 ; B838  68
    TAX                                 ; B839  aa
loc_27_B83A:
    RTS                                 ; B83A  60
ObjAiExplode:  ; AI $74 boss explosion (types $DC -> $DD -> $CF via ObjBecomeNext)
    LDA area_id                         ; B83B  a5 6c
    CMP #$5F                            ; B83D  c9 5f
    BNE loc_27_B845                     ; B83F  d0 04
    LDA #$00                            ; B841  a9 00
    STA irq_mode                        ; B843  85 51
loc_27_B845:
    LDA area_id                         ; B845  a5 6c
    CMP #$1A                            ; B847  c9 1a
    BEQ sub_27_B850                     ; B849  f0 05
    LDA #$10                            ; B84B  a9 10
    STA ram_07D6                        ; B84D  8d d6 07
sub_27_B850:
    LDA #$10                            ; B850  a9 10
    STA boss_cam_lock                   ; B852  8d d7 07
    DEC obj_phase,X                     ; B855  de 00 06
    BMI loc_27_B85B                     ; B858  30 01
    RTS                                 ; B85A  60
loc_27_B85B:
    LDA obj_active,X                    ; B85B  bd a0 04
    AND #$04                            ; B85E  29 04
    BEQ loc_27_B879                     ; B860  f0 17
    LDA #$54                            ; B862  a9 54
    JSR PlaySound                       ; B864  20 25 c1
    LDA #$45                            ; B867  a9 45
    STA chr_r5_play                     ; B869  8d f5 07
    LDA #$80                            ; B86C  a9 80
    STA obj_tile_ofs,X                  ; B86E  9d 20 03
    LDA #$04                            ; B871  a9 04
    STA pal_id_spr3                     ; B873  8d e7 07
    JSR PaletteBuild                    ; B876  20 0e 8c
loc_27_B879:
    LDA area_id                         ; B879  a5 6c
    CMP #$E4                            ; B87B  c9 e4
    BEQ loc_27_B896                     ; B87D  f0 17
    CMP #$73                            ; B87F  c9 73
    BEQ loc_27_B896                     ; B881  f0 13
    CMP #$28                            ; B883  c9 28
    BEQ loc_27_B88B                     ; B885  f0 04
    CMP #$7C                            ; B887  c9 7c
    BNE loc_27_B88F                     ; B889  d0 04
loc_27_B88B:
    CPX #$0E                            ; B88B  e0 0e
    BEQ loc_27_B896                     ; B88D  f0 07
loc_27_B88F:
    LDA #$3E                            ; B88F  a9 3e
    JSR SpawnShot                       ; B891  20 2d 97
    BCS loc_27_B8A8                     ; B894  b0 12
loc_27_B896:
    LDA obj_timer2,X                    ; B896  bd 20 06
    INC obj_timer2,X                    ; B899  fe 20 06
    ASL A                               ; B89C  0a
    ASL A                               ; B89D  0a
    TAY                                 ; B89E  a8
    LDA $B8E9,Y                         ; B89F  b9 e9 b8
    BEQ loc_27_B8BC                     ; B8A2  f0 18
    STA obj_phase,X                     ; B8A4  9d 00 06
    RTS                                 ; B8A7  60
loc_27_B8A8:
    STX tmp_11                          ; B8A8  86 11
    LDA obj_timer2,X                    ; B8AA  bd 20 06
    INC obj_timer2,X                    ; B8AD  fe 20 06
    ASL A                               ; B8B0  0a
    ASL A                               ; B8B1  0a
    TAY                                 ; B8B2  a8
    LDA $B8E6,Y                         ; B8B3  b9 e6 b8
    STA tmp_30                          ; B8B6  85 30
    BNE loc_27_B8BF                     ; B8B8  d0 05
    LDX tmp_11                          ; B8BA  a6 11
loc_27_B8BC:
    JMP ObjBecomeNext                   ; B8BC  4c cd 98
loc_27_B8BF:
    LDA $B8E7,Y                         ; B8BF  b9 e7 b8
    STA tmp_31                          ; B8C2  85 31
    LDA $B8E9,Y                         ; B8C4  b9 e9 b8
    STA obj_phase,X                     ; B8C7  9d 00 06
    JSR ObjPosToTemp                    ; B8CA  20 a8 98
    LDA obj_tile_ofs,X                  ; B8CD  bd 20 03
    PHA                                 ; B8D0  48
    LDA $B8E8,Y                         ; B8D1  b9 e8 b8
    LDX jmp_trampoline                  ; B8D4  a6 10
    STA obj_meta_id,X                   ; B8D6  9d 00 03
    PLA                                 ; B8D9  68
    STA obj_tile_ofs,X                  ; B8DA  9d 20 03
    JSR TempAddOffset                   ; B8DD  20 24 98
    JSR TempToObjPos                    ; B8E0  20 97 98
    LDX tmp_11                          ; B8E3  a6 11
    RTS                                 ; B8E5  60
    .db $03,$10,$EB,$05,$18,$05,$EB,$05,$07,$02,$EC,$02,$FA,$FC,$EB,$04; 27:B8E6
    .db $09,$F3,$EB,$02,$F9,$F3,$EC,$02,$10,$08,$EC,$02,$03,$FA,$EB,$02; 27:B8F6
    .db $0C,$14,$EC,$02,$11,$FB,$EB,$05,$FE,$0D,$EB,$04,$09,$0E,$EC,$03; 27:B906
    .db $0C,$F8,$EB,$02,$1E,$01,$EC,$02,$F2,$00,$EC,$02,$12,$F7,$EB,$02; 27:B916
    .db $18,$0F,$EC,$02,$0C,$FF,$EC,$02,$07,$EB,$EC,$02,$0A,$02,$EC,$02; 27:B926
    .db $05,$FD,$EB,$02,$00,$00,$00,$00 ; 27:B936
sub_27_B93E:
    LDA #$10                            ; B93E  a9 10
    STA boss_cam_lock                   ; B940  8d d7 07
    JSR sub_27_B955                     ; B943  20 55 b9
    DEC obj_act_timer,X                 ; B946  de 40 06
    LDA obj_act_timer,X                 ; B949  bd 40 06
    CMP #$04                            ; B94C  c9 04
    BCS loc_27_B99D                     ; B94E  b0 4d
    LDA #$08                            ; B950  a9 08
    JMP loc_27_B805                     ; B952  4c 05 b8
sub_27_B955:
    DEC obj_timer2,X                    ; B955  de 20 06
    LDA #$3D                            ; B958  a9 3d
    JSR SpawnShot                       ; B95A  20 2d 97
    BCC loc_27_B980                     ; B95D  90 21
    LDA obj_timer2,X                    ; B95F  bd 20 06
    AND #$0F                            ; B962  29 0f
    ASL A                               ; B964  0a
    ASL A                               ; B965  0a
    TAY                                 ; B966  a8
    LDA $B8E6,Y                         ; B967  b9 e6 b8
    STA tmp_30                          ; B96A  85 30
    LDA $B8E7,Y                         ; B96C  b9 e7 b8
    STA tmp_31                          ; B96F  85 31
    JSR ObjPosToTemp                    ; B971  20 a8 98
    STX tmp_11                          ; B974  86 11
    LDX jmp_trampoline                  ; B976  a6 10
    JSR TempAddOffset                   ; B978  20 24 98
    JSR TempToObjPos                    ; B97B  20 97 98
    LDX tmp_11                          ; B97E  a6 11
loc_27_B980:
    LDA obj_phase,X                     ; B980  bd 00 06
    BNE loc_27_B993                     ; B983  d0 0e
    INC obj_anim,X                      ; B985  fe e0 04
    LDA obj_anim,X                      ; B988  bd e0 04
    CMP #$17                            ; B98B  c9 17
    BNE loc_27_B992                     ; B98D  d0 03
    INC obj_phase,X                     ; B98F  fe 00 06
loc_27_B992:
    RTS                                 ; B992  60
loc_27_B993:
    DEC obj_anim,X                      ; B993  de e0 04
    BNE loc_27_B99D                     ; B996  d0 05
    LDA #$00                            ; B998  a9 00
    STA obj_active,X                    ; B99A  9d a0 04
loc_27_B99D:
    RTS                                 ; B99D  60
loc_27_B99E:
    DEC obj_anim,X                      ; B99E  de e0 04
    DEC obj_anim,X                      ; B9A1  de e0 04
    BPL loc_27_B9A9                     ; B9A4  10 03
    JMP ObjDespawn                      ; B9A6  4c c7 98
loc_27_B9A9:
    RTS                                 ; B9A9  60
sub_27_B9AA:
    LDA obj_phase,X                     ; B9AA  bd 00 06
    BNE loc_27_B99E                     ; B9AD  d0 ef
    JSR ObjPosToTemp                    ; B9AF  20 a8 98
    LDA obj_anim,X                      ; B9B2  bd e0 04
    LSR A                               ; B9B5  4a
    AND #$01                            ; B9B6  29 01
    TAY                                 ; B9B8  a8
    LDA $BA2C,Y                         ; B9B9  b9 2c ba
    STA tmp_30                          ; B9BC  85 30
    LDA #$00                            ; B9BE  a9 00
    STA tmp_31                          ; B9C0  85 31
    JSR TempAddStep                     ; B9C2  20 61 98
    JSR TempToObjPos                    ; B9C5  20 97 98
    DEC obj_anim,X                      ; B9C8  de e0 04
    DEC obj_anim,X                      ; B9CB  de e0 04
    BMI loc_27_B9D1                     ; B9CE  30 01
    RTS                                 ; B9D0  60
loc_27_B9D1:
    LDA obj_flags,X                     ; B9D1  bd 80 03
    AND #$DD                            ; B9D4  29 dd
    ORA #$01                            ; B9D6  09 01
    STA obj_flags,X                     ; B9D8  9d 80 03
    LDA #$3F                            ; B9DB  a9 3f
    STA obj_anim,X                      ; B9DD  9d e0 04
    LDA #$AC                            ; B9E0  a9 ac
    STA obj_meta_id,X                   ; B9E2  9d 00 03
    STA obj_phase,X                     ; B9E5  9d 00 06
    LDA #$00                            ; B9E8  a9 00
    STA obj_tile_ofs,X                  ; B9EA  9d 20 03
    LDA obj_elem_drop,X                 ; B9ED  bd 00 05
    AND #$F0                            ; B9F0  29 f0
    BNE loc_27_B9F5                     ; B9F2  d0 01
    RTS                                 ; B9F4  60
loc_27_B9F5:
    LDA #$3F                            ; B9F5  a9 3f
    LDY #$00                            ; B9F7  a0 00
    JSR SpawnShot                       ; B9F9  20 2d 97
    BCS loc_27_BA14                     ; B9FC  b0 16
    LDA #$00                            ; B9FE  a9 00
    STA obj_active,X                    ; BA00  9d a0 04
    LDA obj_elem_drop,X                 ; BA03  bd 00 05
    PHA                                 ; BA06  48
    LDA #$3F                            ; BA07  a9 3f
    LDY #$00                            ; BA09  a0 00
    JSR SpawnShot                       ; BA0B  20 2d 97
    PLA                                 ; BA0E  68
    STA obj_elem_drop,X                 ; BA0F  9d 00 05
    BCC loc_27_BA2B                     ; BA12  90 17
loc_27_BA14:
    LDY jmp_trampoline                  ; BA14  a4 10
    LDA obj_elem_drop,X                 ; BA16  bd 00 05
    STA obj_elem_drop,Y                 ; BA19  99 00 05
    LSR A                               ; BA1C  4a
    LSR A                               ; BA1D  4a
    LSR A                               ; BA1E  4a
    LSR A                               ; BA1F  4a
    AND #$0F                            ; BA20  29 0f
    TAY                                 ; BA22  a8
    LDA $8BFE,Y                         ; BA23  b9 fe 8b
    LDY jmp_trampoline                  ; BA26  a4 10
    STA obj_meta_id,Y                   ; BA28  99 00 03
loc_27_BA2B:
    RTS                                 ; BA2B  60
    .db $FC,$04                         ; 27:BA2C
Ai70PhaseDispatch:  ; s67: AI $70 (final scene): JMP (JmpTable27_BA40[obj_phase])
    LDA obj_phase,X                     ; BA2E  bd 00 06
    ASL A                               ; BA31  0a
    TAY                                 ; BA32  a8
    LDA JmpTable27_BA40,Y               ; BA33  b9 40 ba
    STA jmp_trampoline                  ; BA36  85 10
    LDA $BA41,Y                         ; BA38  b9 41 ba
    STA tmp_11                          ; BA3B  85 11
    JMP (jmp_trampoline)                ; BA3D  6c 10 00
JmpTable27_BA40:  ; s67: AI $70 phase handlers [obj_phase]: exactly 13 entries (ends at code BA5A); 7 and 11 null; 10 = BD19 (-> SubstFinalSceneReset) (JUMP_TABLES)
    .dw sub_27_BA5A                     ; 27:BA40
    .dw sub_27_BAF5                     ; 27:BA42
    .dw sub_27_BBE7                     ; 27:BA44
    .dw sub_27_BC0C                     ; 27:BA46
    .dw sub_27_BC37                     ; 27:BA48
    .dw sub_27_BC71                     ; 27:BA4A
    .dw sub_27_BC94                     ; 27:BA4C
    .dw $0000                           ; 27:BA4E
    .dw sub_27_BD29                     ; 27:BA50
    .dw sub_27_BCF6                     ; 27:BA52
    .dw sub_27_BD19                     ; 27:BA54
    .dw $0000                           ; 27:BA56
    .dw sub_27_BDA9                     ; 27:BA58
sub_27_BA5A:
    LDA obj_active_s0D                  ; BA5A  ad ad 04
    BNE loc_27_BA70                     ; BA5D  d0 11
    LDA #$6E                            ; BA5F  a9 6e
    JSR PlaySound                       ; BA61  20 25 c1
    STX tmp_2F                          ; BA64  86 2f
    JSR sub_27_BA71                     ; BA66  20 71 ba
    LDX tmp_2F                          ; BA69  a6 2f
    LDA #$00                            ; BA6B  a9 00
    STA obj_active,X                    ; BA6D  9d a0 04
loc_27_BA70:
    RTS                                 ; BA70  60
sub_27_BA71:
    JSR WaitPpuQueue                    ; BA71  20 76 c6
    JSR sub_27_BAB8                     ; BA74  20 b8 ba
    LDA #$07                            ; BA77  a9 07
    STA tmp_1F                          ; BA79  85 1f
loc_27_BA7B:
    JSR WaitPpuQueue                    ; BA7B  20 76 c6
    LDA #$00                            ; BA7E  a9 00
    STA frame_ready                     ; BA80  85 09
    LDY tmp_1F                          ; BA82  a4 1f
    LDA $BA9A,Y                         ; BA84  b9 9a ba
    TAX                                 ; BA87  aa
    JSR sub_27_BAA2                     ; BA88  20 a2 ba
    LDA #$2E                            ; BA8B  a9 2e
    JSR PpuQueueBlock                   ; BA8D  20 82 c4
    DEC tmp_1F                          ; BA90  c6 1f
    BPL loc_27_BA7B                     ; BA92  10 e7
    LDA #$FF                            ; BA94  a9 ff
    STA visited_map                     ; BA96  8d f0 62
    RTS                                 ; BA99  60
    .db $00,$04,$02,$06,$01,$05,$03,$07 ; 27:BA9A
sub_27_BAA2:
    LDA wram_buf,X                      ; BAA2  bd 00 60
    CMP #$7C                            ; BAA5  c9 7c
    BNE loc_27_BAAE                     ; BAA7  d0 05
    LDA #$7E                            ; BAA9  a9 7e
    STA wram_buf,X                      ; BAAB  9d 00 60
loc_27_BAAE:
    TXA                                 ; BAAE  8a
    CLC                                 ; BAAF  18
    ADC #$08                            ; BAB0  69 08
    TAX                                 ; BAB2  aa
    CPX #$DC                            ; BAB3  e0 dc
    BCC sub_27_BAA2                     ; BAB5  90 eb
    RTS                                 ; BAB7  60
sub_27_BAB8:
    LDX #$DC                            ; BAB8  a2 dc
loc_27_BABA:
    LDA #$7C                            ; BABA  a9 7c
    STA $5FFF,X                         ; BABC  9d ff 5f
    DEX                                 ; BABF  ca
    BNE loc_27_BABA                     ; BAC0  d0 f8
    LDX #$16                            ; BAC2  a2 16
loc_27_BAC4:
    LDA #$6F                            ; BAC4  a9 6f
    STA wram_6001,X                     ; BAC6  9d 01 60
    LDA #$72                            ; BAC9  a9 72
    STA $6017,X                         ; BACB  9d 17 60
    LDA #$75                            ; BACE  a9 75
    STA $60C7,X                         ; BAD0  9d c7 60
    DEX                                 ; BAD3  ca
    BPL loc_27_BAC4                     ; BAD4  10 ee
    LDY #$0A                            ; BAD6  a0 0a
    LDX #$00                            ; BAD8  a2 00
loc_27_BADA:
    LDA #$1A                            ; BADA  a9 1a
    STA wram_buf,X                      ; BADC  9d 00 60
    LDA #$1B                            ; BADF  a9 1b
    STA $6015,X                         ; BAE1  9d 15 60
    TXA                                 ; BAE4  8a
    CLC                                 ; BAE5  18
    ADC #$16                            ; BAE6  69 16
    TAX                                 ; BAE8  aa
    DEY                                 ; BAE9  88
    BNE loc_27_BADA                     ; BAEA  d0 ee
    LDA #$7F                            ; BAEC  a9 7f
    STA $60C6                           ; BAEE  8d c6 60
    STA $60DB                           ; BAF1  8d db 60
    RTS                                 ; BAF4  60
sub_27_BAF5:
    LDA #$7F                            ; BAF5  a9 7f
    STA boss_cam_lock                   ; BAF7  8d d7 07
    LDA obj_behav,X                     ; BAFA  bd e0 06
    BMI loc_27_BB03                     ; BAFD  30 04
    INC obj_behav,X                     ; BAFF  fe e0 06
    RTS                                 ; BB02  60
loc_27_BB03:
    INC obj_anim,X                      ; BB03  fe e0 04
    LDA obj_anim,X                      ; BB06  bd e0 04
    AND #$0F                            ; BB09  29 0f
    BNE loc_27_BB78                     ; BB0B  d0 6b
    LDA obj_anim,X                      ; BB0D  bd e0 04
    LSR A                               ; BB10  4a
    LSR A                               ; BB11  4a
    TAY                                 ; BB12  a8
    LDA $BBCE,Y                         ; BB13  b9 ce bb
    CMP #$FF                            ; BB16  c9 ff
    BNE loc_27_BB79                     ; BB18  d0 5f
    DEC obj_anim,X                      ; BB1A  de e0 04
    LDA orbit_level,X                   ; BB1D  bd c0 06
    BMI loc_27_BB26                     ; BB20  30 04
    INC orbit_level,X                   ; BB22  fe c0 06
    RTS                                 ; BB25  60
loc_27_BB26:
    LDA #$1B                            ; BB26  a9 1b
    STA text_group                      ; BB28  8d df 07
    LDA #$05                            ; BB2B  a9 05
    STA text_msg_id                     ; BB2D  8d de 07
    TXA                                 ; BB30  8a
    PHA                                 ; BB31  48
loc_27_BB32:
    LDA #$29                            ; BB32  a9 29
    JSR PlaySound                       ; BB34  20 25 c1
    JSR FarCall_D354                    ; BB37  20 cf ff
    INC text_msg_id                     ; BB3A  ee de 07
    LDA text_msg_id                     ; BB3D  ad de 07
    CMP #$08                            ; BB40  c9 08
    BNE loc_27_BB32                     ; BB42  d0 ee
    LDA #$01                            ; BB44  a9 01
    STA $0676                           ; BB46  8d 76 06
    LDA #$53                            ; BB49  a9 53
    STA $04B6                           ; BB4B  8d b6 04
    LDA #$00                            ; BB4E  a9 00
    STA $05B6                           ; BB50  8d b6 05
    LDA #$25                            ; BB53  a9 25
    JSR PlaySound                       ; BB55  20 25 c1
    LDA player_hp_max                   ; BB58  ad c0 03
    STA player_hp                       ; BB5B  8d c1 03
    JSR HudHpBar                        ; BB5E  20 c0 8c
    LDA mp_max                          ; BB61  ad 09 07
    STA mp_cur                          ; BB64  8d 08 07
    LDA #$04                            ; BB67  a9 04
    JSR HudRedraw                       ; BB69  20 46 8e
    PLA                                 ; BB6C  68
    TAX                                 ; BB6D  aa
    LDA #$02                            ; BB6E  a9 02
    STA obj_active                      ; BB70  8d a0 04
    LDA #$00                            ; BB73  a9 00
    STA obj_active,X                    ; BB75  9d a0 04
loc_27_BB78:
    RTS                                 ; BB78  60
loc_27_BB79:
    TXA                                 ; BB79  8a
    PHA                                 ; BB7A  48
    LDA obj_anim,X                      ; BB7B  bd e0 04
    LSR A                               ; BB7E  4a
    LSR A                               ; BB7F  4a
    LSR A                               ; BB80  4a
    LSR A                               ; BB81  4a
    STA jmp_trampoline                  ; BB82  85 10
    TXA                                 ; BB84  8a
    CLC                                 ; BB85  18
    ADC jmp_trampoline                  ; BB86  65 10
    TAX                                 ; BB88  aa
    STX jmp_trampoline                  ; BB89  86 10
    LDA #$31                            ; BB8B  a9 31
    STA tmp_11                          ; BB8D  85 11
    JSR FarCall_C25D                    ; BB8F  20 80 ff
    LDA $BBCE,Y                         ; BB92  b9 ce bb
    STA obj_x_lo,X                      ; BB95  95 70
    LDA #$00                            ; BB97  a9 00
    STA obj_x_hi,X                      ; BB99  95 90
    LDA $BBCF,Y                         ; BB9B  b9 cf bb
    STA obj_y_lo,X                      ; BB9E  95 b0
    LDA #$00                            ; BBA0  a9 00
    STA obj_y_hi,X                      ; BBA2  95 d0
    LDA $BBD0,Y                         ; BBA4  b9 d0 bb
    STA obj_tile_ofs,X                  ; BBA7  9d 20 03
    LDA $BBD1,Y                         ; BBAA  b9 d1 bb
    STA obj_meta_id,X                   ; BBAD  9d 00 03
    LDA #$00                            ; BBB0  a9 00
    STA obj_phase                       ; BBB2  8d 00 06
    STA obj_timer2                      ; BBB5  8d 20 06
    STA orbit_level                     ; BBB8  8d c0 06
    STA obj_meta_dir                    ; BBBB  8d 60 03
    LDA #$FF                            ; BBBE  a9 ff
    STA obj_freeze                      ; BBC0  8d a0 05
    LDA #$80                            ; BBC3  a9 80
    STA obj_x_lo                        ; BBC5  85 70
    LDA #$A0                            ; BBC7  a9 a0
    STA obj_y_lo                        ; BBC9  85 b0
    PLA                                 ; BBCB  68
    TAX                                 ; BBCC  aa
    RTS                                 ; BBCD  60
    .db $00,$00,$00,$00,$80,$68,$00,$DF,$58,$58,$40,$32,$A8,$58,$40,$2E; 27:BBCE
    .db $58,$88,$40,$36,$A8,$88,$40,$3A,$FF; 27:BBDE
sub_27_BBE7:
    JSR WaitPpuQueue                    ; BBE7  20 76 c6
    LDY #$3F                            ; BBEA  a0 3f
    LDA #$FF                            ; BBEC  a9 ff
loc_27_BBEE:
    STA wram_6060,Y                     ; BBEE  99 60 60
    DEY                                 ; BBF1  88
    BPL loc_27_BBEE                     ; BBF2  10 fa
    LDY #$2F                            ; BBF4  a0 2f
loc_27_BBF6:
    LDA attr_shadow,Y                   ; BBF6  b9 00 61
    STA wram_6060,Y                     ; BBF9  99 60 60
    DEY                                 ; BBFC  88
    BPL loc_27_BBF6                     ; BBFD  10 f7
    LDA #$04                            ; BBFF  a9 04
    JSR PpuQueueBlock                   ; BC01  20 82 c4
    LDA #$1F                            ; BC04  a9 1f
    STA sub_state                       ; BC06  85 41
    INC obj_phase,X                     ; BC08  fe 00 06
    RTS                                 ; BC0B  60
sub_27_BC0C:
    INC obj_phase,X                     ; BC0C  fe 00 06
    STX tmp_2F                          ; BC0F  86 2f
    LDX #$02                            ; BC11  a2 02
loc_27_BC13:
    LDA $BC34,X                         ; BC13  bd 34 bc
    STA pal_nofade_mask                 ; BC16  8d e8 07
    TXA                                 ; BC19  8a
    CLC                                 ; BC1A  18
    ADC #$A2                            ; BC1B  69 a2
    STA pal_ids,X                       ; BC1D  9d e0 07
    LDA #$09                            ; BC20  a9 09
    JSR Bank26_FadeB                    ; BC22  20 49 8c
    DEX                                 ; BC25  ca
    BPL loc_27_BC13                     ; BC26  10 eb
    LDA #$00                            ; BC28  a9 00
    STA pal_nofade_mask                 ; BC2A  8d e8 07
    LDX tmp_2F                          ; BC2D  a6 2f
    LDA #$14                            ; BC2F  a9 14
    JMP PlaySound                       ; BC31  4c 25 c1
    .db $3E,$3D,$3B                     ; 27:BC34
sub_27_BC37:
    LDA #$00                            ; BC37  a9 00
    STA obj_coll,X                      ; BC39  9d a0 03
    JSR sub_27_BCD7                     ; BC3C  20 d7 bc
    LDA #$FC                            ; BC3F  a9 fc
    STA obj_overlay_id,X                ; BC41  9d 80 05
    LDA #$FB                            ; BC44  a9 fb
    STA obj_meta_id,X                   ; BC46  9d 00 03
    LDA #$00                            ; BC49  a9 00
    STA obj_anim,X                      ; BC4B  9d e0 04
    INC obj_timer2,X                    ; BC4E  fe 20 06
    LDA obj_timer2,X                    ; BC51  bd 20 06
    AND #$0F                            ; BC54  29 0f
    TAY                                 ; BC56  a8
    LDA $BC61,Y                         ; BC57  b9 61 bc
    STA obj_behav,X                     ; BC5A  9d e0 06
    INC obj_phase,X                     ; BC5D  fe 00 06
loc_27_BC60:
    RTS                                 ; BC60  60
    .db $01,$01,$3C,$78,$78,$28,$0A,$0A,$1E,$14,$F0,$01,$01,$14,$64,$C8; 27:BC61
sub_27_BC71:
    JSR sub_27_BCD7                     ; BC71  20 d7 bc
    DEC obj_behav,X                     ; BC74  de e0 06
    BNE loc_27_BC60                     ; BC77  d0 e7
    INC obj_behav,X                     ; BC79  fe e0 06
    LDA #$34                            ; BC7C  a9 34
    STA obj_coll,X                      ; BC7E  9d a0 03
    INC obj_anim,X                      ; BC81  fe e0 04
    LDA obj_anim,X                      ; BC84  bd e0 04
    CMP #$1F                            ; BC87  c9 1f
    BNE loc_27_BC60                     ; BC89  d0 d5
    INC obj_phase,X                     ; BC8B  fe 00 06
    LDA #$20                            ; BC8E  a9 20
    STA obj_behav,X                     ; BC90  9d e0 06
    RTS                                 ; BC93  60
sub_27_BC94:
    JSR sub_27_BCD7                     ; BC94  20 d7 bc
    LDA $064E                           ; BC97  ad 4e 06
    AND #$03                            ; BC9A  29 03
    BEQ loc_27_BCC1                     ; BC9C  f0 23
    LDA tmp_08                          ; BC9E  a5 08
    AND #$07                            ; BCA0  29 07
    BNE loc_27_BCC1                     ; BCA2  d0 1d
    LDY #$04                            ; BCA4  a0 04
    LDA #$29                            ; BCA6  a9 29
    JSR SpawnShot                       ; BCA8  20 2d 97
    BCC loc_27_BCC1                     ; BCAB  90 14
    LDY jmp_trampoline                  ; BCAD  a4 10
    LDA a:obj_x_lo,Y                    ; BCAF  b9 70 00
    CLC                                 ; BCB2  18
    ADC #$04                            ; BCB3  69 04
    STA a:obj_x_lo,Y                    ; BCB5  99 70 00
    LDA a:obj_y_lo,Y                    ; BCB8  b9 b0 00
    CLC                                 ; BCBB  18
    ADC #$12                            ; BCBC  69 12
    STA a:obj_y_lo,Y                    ; BCBE  99 b0 00
loc_27_BCC1:
    DEC obj_behav,X                     ; BCC1  de e0 06
    BNE loc_27_BC60                     ; BCC4  d0 9a
    INC obj_behav,X                     ; BCC6  fe e0 06
    DEC obj_anim,X                      ; BCC9  de e0 04
    LDA obj_anim,X                      ; BCCC  bd e0 04
    BNE loc_27_BC60                     ; BCCF  d0 8f
    LDA #$04                            ; BCD1  a9 04
    STA obj_phase,X                     ; BCD3  9d 00 06
    RTS                                 ; BCD6  60
sub_27_BCD7:
    LDA #$07                            ; BCD7  a9 07
    JSR sub_26_9B07                     ; BCD9  20 07 9b
    LDA #$00                            ; BCDC  a9 00
    STA irq_mode                        ; BCDE  85 51
    LDA tmp_08                          ; BCE0  a5 08
    AND #$04                            ; BCE2  29 04
    BNE loc_27_BCF5                     ; BCE4  d0 0f
    LDA #$02                            ; BCE6  a9 02
    STA irq_mode                        ; BCE8  85 51
    LDY #$05                            ; BCEA  a0 05
loc_27_BCEC:
    LDA chr_r0_play,Y                   ; BCEC  b9 f0 07
    STA a:chr_r0_alt,Y                  ; BCEF  99 58 00
    DEY                                 ; BCF2  88
    BPL loc_27_BCEC                     ; BCF3  10 f7
loc_27_BCF5:
    RTS                                 ; BCF5  60
sub_27_BCF6:
    INC obj_phase,X                     ; BCF6  fe 00 06
    JSR WaitPpuQueue                    ; BCF9  20 76 c6
    LDA #$0B                            ; BCFC  a9 0b
    JSR SetR6                           ; BCFE  20 18 c4
    LDY #$00                            ; BD01  a0 00
loc_27_BD03:
    LDA $9E00,Y                         ; BD03  b9 00 9e
    STA wram_buf,Y                      ; BD06  99 00 60
    INY                                 ; BD09  c8
    BNE loc_27_BD03                     ; BD0A  d0 f7
    LDA #$44                            ; BD0C  a9 44
    JSR PpuQueueBlock                   ; BD0E  20 82 c4
    JSR WaitPpuQueue                    ; BD11  20 76 c6
    LDA #$1A                            ; BD14  a9 1a
    JMP SetR6                           ; BD16  4c 18 c4
sub_27_BD19:
    INC obj_timer2,X                    ; BD19  fe 20 06
    BEQ loc_27_BD1F                     ; BD1C  f0 01
    RTS                                 ; BD1E  60
loc_27_BD1F:
    LDA #$1E                            ; BD1F  a9 1e
    STA sub_state                       ; BD21  85 41
    LDA #$00                            ; BD23  a9 00
    STA obj_active,X                    ; BD25  9d a0 04
    RTS                                 ; BD28  60
sub_27_BD29:
    LDA $034D                           ; BD29  ad 4d 03
    BMI loc_27_BD68                     ; BD2C  30 3a
    LDA tmp_08                          ; BD2E  a5 08
    BNE loc_27_BD35                     ; BD30  d0 03
    INC obj_act_timer,X                 ; BD32  fe 40 06
loc_27_BD35:
    CLC                                 ; BD35  18
    TXA                                 ; BD36  8a
    ASL A                               ; BD37  0a
    ASL A                               ; BD38  0a
    ADC obj_act_timer,X                 ; BD39  7d 40 06
    AND #$07                            ; BD3C  29 07
    BEQ loc_27_BD41                     ; BD3E  f0 01
loc_27_BD40:
    RTS                                 ; BD40  60
loc_27_BD41:
    LDA tmp_08                          ; BD41  a5 08
    AND #$01                            ; BD43  29 01
    BNE loc_27_BD40                     ; BD45  d0 f9
    LDA tmp_08                          ; BD47  a5 08
    AND #$1E                            ; BD49  29 1e
    LSR A                               ; BD4B  4a
    TAY                                 ; BD4C  a8
    LDA $BD58,Y                         ; BD4D  b9 58 bd
    BMI loc_27_BD40                     ; BD50  30 ee
    TAY                                 ; BD52  a8
    LDA #$2A                            ; BD53  a9 2a
    JMP SpawnShot                       ; BD55  4c 2d 97
    .db $FF,$05,$06,$07,$08,$09,$0A,$0B,$FF,$0B,$0A,$09,$08,$07,$06,$05; 27:BD58
loc_27_BD68:
    LDA tmp_08                          ; BD68  a5 08
    AND #$07                            ; BD6A  29 07
    BNE loc_27_BD40                     ; BD6C  d0 d2
    LDA obj_x_lo                        ; BD6E  a5 70
    LSR A                               ; BD70  4a
    LSR A                               ; BD71  4a
    LSR A                               ; BD72  4a
    LSR A                               ; BD73  4a
    STA jmp_trampoline                  ; BD74  85 10
    TXA                                 ; BD76  8a
    AND #$01                            ; BD77  29 01
    ASL A                               ; BD79  0a
    ASL A                               ; BD7A  0a
    ASL A                               ; BD7B  0a
    ASL A                               ; BD7C  0a
    ORA jmp_trampoline                  ; BD7D  05 10
    TAY                                 ; BD7F  a8
    LDA $BD89,Y                         ; BD80  b9 89 bd
    TAY                                 ; BD83  a8
    LDA #$28                            ; BD84  a9 28
    JMP SpawnShot                       ; BD86  4c 2d 97
    .db $0A,$09,$08,$07,$07,$07,$06,$06,$06,$05,$05,$05,$05,$05,$05,$05; 27:BD89
    .db $0A,$0A,$0A,$0A,$0A,$0A,$09,$09,$09,$08,$08,$07,$07,$06,$05,$05; 27:BD99
sub_27_BDA9:
    LDY obj_param2,X                    ; BDA9  bc a0 06
    LDA obj_active,Y                    ; BDAC  b9 a0 04
    CMP #$2E                            ; BDAF  c9 2e
    BEQ loc_27_BDB9                     ; BDB1  f0 06
    LDA #$00                            ; BDB3  a9 00
    STA obj_active,X                    ; BDB5  9d a0 04
    RTS                                 ; BDB8  60
loc_27_BDB9:
    LDA obj_freeze,Y                    ; BDB9  b9 a0 05
    BNE loc_27_BDDF                     ; BDBC  d0 21
    LDA #$00                            ; BDBE  a9 00
    STA obj_coll,X                      ; BDC0  9d a0 03
    LDA #$F4                            ; BDC3  a9 f4
    STA obj_meta_id,X                   ; BDC5  9d 00 03
    LDA obj_anim,X                      ; BDC8  bd e0 04
    SEC                                 ; BDCB  38
    SBC #$04                            ; BDCC  e9 04
    STA obj_anim,X                      ; BDCE  9d e0 04
    AND #$7F                            ; BDD1  29 7f
    BNE loc_27_BDDF                     ; BDD3  d0 0a
    LDA obj_flags,X                     ; BDD5  bd 80 03
    BMI loc_27_BDDF                     ; BDD8  30 05
    LDA #$6F                            ; BDDA  a9 6f
    JSR PlaySound                       ; BDDC  20 25 c1
loc_27_BDDF:
    STX tmp_1F                          ; BDDF  86 1f
    LDA obj_param2,X                    ; BDE1  bd a0 06
    TAX                                 ; BDE4  aa
    JSR ObjPosToTemp                    ; BDE5  20 a8 98
    LDA obj_act_timer,X                 ; BDE8  bd 40 06
    STA jmp_trampoline                  ; BDEB  85 10
    LDA obj_meta_id,X                   ; BDED  bd 00 03
    AND #$03                            ; BDF0  29 03
    STA tmp_11                          ; BDF2  85 11
    ASL A                               ; BDF4  0a
    TAY                                 ; BDF5  a8
    LDX tmp_1F                          ; BDF6  a6 1f
    LDA jmp_trampoline                  ; BDF8  a5 10
    BEQ loc_27_BE20                     ; BDFA  f0 24
    TYA                                 ; BDFC  98
    ORA #$08                            ; BDFD  09 08
    TAY                                 ; BDFF  a8
    LDA tmp_11                          ; BE00  a5 11
    CLC                                 ; BE02  18
    ADC #$F5                            ; BE03  69 f5
    STA obj_meta_id,X                   ; BE05  9d 00 03
    LDA tmp_11                          ; BE08  a5 11
    ASL A                               ; BE0A  0a
    ASL A                               ; BE0B  0a
    ASL A                               ; BE0C  0a
    ASL A                               ; BE0D  0a
    STA obj_anim,X                      ; BE0E  9d e0 04
    LDA tmp_11                          ; BE11  a5 11
    AND #$01                            ; BE13  29 01
    ORA #$46                            ; BE15  09 46
    STA obj_coll,X                      ; BE17  9d a0 03
    LDA tmp_11                          ; BE1A  a5 11
    ASL A                               ; BE1C  0a
    STA obj_meta_dir,X                  ; BE1D  9d 60 03
loc_27_BE20:
    LDA $BE30,Y                         ; BE20  b9 30 be
    STA tmp_30                          ; BE23  85 30
    LDA $BE31,Y                         ; BE25  b9 31 be
    STA tmp_31                          ; BE28  85 31
    JSR TempAddOffset                   ; BE2A  20 24 98
    JMP TempToObjPos                    ; BE2D  4c 97 98
    .db $08,$EA,$FD,$EA,$F8,$EA,$03,$EA,$06,$CE,$1E,$F1,$FD,$0C,$DD,$F1; 27:BE30
StaleText27_BE40:  ; dead filler to BFFF: copy of 21:BE40 text + 23 save table
    .db $6B,$20,$F3,$2E,$20,$42,$65,$63,$61,$75,$73,$65,$20,$CF,$F4,$02; 27:BE40
    .db $6B,$69,$6E,$64,$CA,$20,$FB,$F8,$C7,$72,$65,$74,$75,$72,$6E,$02; 27:BE50
    .db $D8,$DA,$73,$65,$61,$2E,$20,$DE,$E0,$03,$01,$20,$85,$E5,$B1,$EB; 27:BE60
    .db $F3,$77,$69,$73,$68,$02,$D8,$74,$72,$61,$76,$65,$6C,$20,$DA,$73; 27:BE70
    .db $65,$61,$2E,$00,$01,$05,$00,$68,$6F,$70,$20,$CD,$FA,$62,$61,$63; 27:BE80
    .db $6B,$2E,$20,$AE,$02,$DF,$F3,$77,$68,$65,$72,$65,$76,$FF,$F3,$6C; 27:BE90
    .db $69,$6B,$65,$2E,$00,$01,$DE,$63,$61,$72,$65,$2E,$00,$01,$FB,$68; 27:BEA0
    .db $65,$61,$72,$64,$20,$E1,$61,$20,$07,$35,$E8,$02,$A9,$2C,$20,$89; 27:BEB0
    .db $FB,$63,$6F,$75,$6C,$64,$92,$A0,$02,$69,$74,$2E,$20,$AE,$B9,$FE; 27:BEC0
    .db $B1,$AB,$61,$02,$F9,$61,$63,$72,$6F,$73,$73,$20,$DA,$72,$69,$76; 27:BED0
    .db $FF,$5F,$00,$01,$06,$01,$3A,$22,$48,$65,$79,$2C,$20,$F3,$27,$72; 27:BEE0
    .db $65,$20,$DA,$02,$05,$02,$77,$68,$6F,$20,$96,$FA,$73,$74,$61,$74; 27:BEF0
    .db $75,$65,$5F,$02,$AE,$9E,$F3,$E0,$2C,$20,$62,$75,$74,$02,$C7,$77; 27:BF00
    .db $65,$27,$72,$65,$20,$65,$76,$65,$6E,$2E,$22,$00,$01,$2D,$2D,$2D; 27:BF10
    .db $2D,$2D,$2D,$00,$01,$B0,$73,$61,$76,$86,$21,$00,$01,$5F,$5F,$5F; 27:BF20
    .db $00,$00,$80,$00,$00,$81,$00,$00,$00,$00,$00,$00,$00,$00,$81,$00; 27:BF30
    .db $81,$81,$81,$00,$00,$00,$00,$81,$00,$00,$00,$00,$00,$00,$00,$00; 27:BF40
    .db $81,$00,$00,$00,$00,$00,$00,$00,$81,$00,$00,$00,$00,$00,$80,$00; 27:BF50
    .db $00,$81,$81,$00,$00,$81,$00,$00,$00,$00,$00,$00,$00,$00,$80,$00; 27:BF60
    .db $81,$81,$81,$81,$00,$00,$00,$00,$81,$00,$00,$00,$00,$00,$00,$00; 27:BF70
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$81,$00,$81,$00; 27:BF80
    .db $81,$00,$00,$81,$81,$81,$81,$00,$81,$00,$00,$00,$80,$00,$00,$80; 27:BF90
    .db $00,$00,$00,$00,$00,$00,$80,$00,$00,$80,$81,$00,$80,$00,$00,$00; 27:BFA0
    .db $00,$00,$00,$00,$00,$00,$80,$00,$81,$80,$00,$81,$00,$00,$00,$81; 27:BFB0
    .db $81,$81,$00,$00,$00,$81,$81,$00,$00,$00,$00,$00,$00,$81,$81,$00; 27:BFC0
    .db $00,$00,$00,$00,$81,$81,$81,$00,$81,$00,$00,$00,$00,$00,$81,$81; 27:BFD0
    .db $81,$81,$81,$81,$81,$81,$00,$00,$81,$81,$00,$00,$81,$00,$00,$81; 27:BFE0
    .db $00,$81,$00,$81,$81,$00,$00,$00,$00,$00,$81,$00,$FF,$FF,$FF,$FF; 27:BFF0
