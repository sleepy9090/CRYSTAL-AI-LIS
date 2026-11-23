; re-c bank 31  file 0x3E010  CPU $E000-$FFFF  (tools/disasm_all.py)

ppuctrl_shadow = $00
ppumask_shadow = $01
scroll_shadow0 = $02
cam_x_hi = $03
cam_y_lo = $04
cam_y_hi = $05
nmi_busy_flag = $06
nmi_missed_flag = $07
tmp_08 = $08
frame_ready = $09
tmp_0A = $0A
tmp_0B = $0B
boot_ab_flag = $0F
jmp_trampoline = $10
tmp_11 = $11
tmp_12 = $12
tmp_13 = $13
tmp_14 = $14
tmp_15 = $15
tmp_16 = $16
tmp_18 = $18
tmp_19 = $19
tmp_1E = $1E
tmp_1F = $1F
tmp_20 = $20
tmp_21 = $21
tmp_22 = $22
tmp_23 = $23
tmp_24 = $24
tmp_25 = $25
tmp_26 = $26
tmp_28 = $28
tmp_29 = $29
tmp_2A = $2A
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
cam_ofs_x = $3A
cam_ofs_y = $3B
zp_3C = $3C
zp_3D = $3D
zp_3E = $3E
zp_3F = $3F
main_state = $40
sub_state = $41
pad_prev = $43
pad_hold_a = $45
pad_hold_b = $47
pad_dir = $49
pad_new = $4B
zp_4C = $4C
pad_act = $4D
pad_raw = $4F
mmc3_banksel_shadow = $50
irq_mode = $51
irq_vector = $54
irq_handler_idx = $56
zp_57 = $57
chr_r0_alt = $58
chr_r1_alt = $59
chr_r2_alt = $5A
chr_r3_alt = $5B
chr_r4_alt = $5C
chr_r5_alt = $5D
zp_5E = $5E
zp_5F = $5F
nmi_disable = $60
zp_66 = $66
zp_67 = $67
zp_68 = $68
zp_69 = $69
area_id = $6C
area_entry = $6D
cur_bank16_x2 = $6E
r7_shadow = $6F
obj_x_lo = $70
obj_x_hi = $90
obj_y_lo = $B0
obj_y_lo_s01 = $B1
obj_y_hi = $D0
snd_fade_timer = $FE
snd_queue_count = $0100
snd_queue_lock = $0101
snd_cur_music = $0102
snd_queue = $0103
snd_cmd = $0110
snd_music_id = $0111
snd_ch_reg0 = $0168
warmboot_sig = $0182
oam_attr = $0202
obj_meta_id = $0300
obj_tile_ofs = $0320
obj_flags2 = $0340
obj_flags = $0380
obj_flags_s01 = $0381
obj_coll = $03A0
player_hp_max = $03C0
player_hp = $03C1
obj_active = $04A0
obj_active_s1E = $04BE
obj_anim = $04E0
obj_spawn_b2 = $0540
ride_platform_slot = $0541
obj_spawn_param = $0560
obj_spawn_param_s01 = $0561
obj_freeze = $05A0
obj_screen_x = $05C0
obj_phase = $0600
obj_timer2 = $0620
obj_act_timer = $0640
player_bg_attr = $0641
obj_param1 = $0680
obj_param2 = $06A0
orbit_level = $06C0
obj_behav = $06E0
mp_cur = $0708
mp_max = $0709
obj1_flash = $0710
magic_sel = $0712
equip_armor = $0713
equip_accessory = $0716
invuln_timer = $071A
ram_0744 = $0744
ram_07D5 = $07D5
ram_07D6 = $07D6
boss_cam_lock = $07D7
scroll_x_irq = $07D8
ram_07DA = $07DA
ram_07DB = $07DB
pal_ids = $07E0
pal_id_bg1 = $07E1
pal_id_bg2 = $07E2
pal_id_bg3 = $07E3
pal_id_spr0 = $07E4
pal_id_spr2 = $07E6
pal_id_spr3 = $07E7
pal_nofade_mask = $07E8
chr_r0_play = $07F0
chr_r1_play = $07F1
chr_r2_play = $07F2
chr_r3_play = $07F3
chr_r4_play = $07F4
chr_r5_play = $07F5
irq_delay = $07FE
obj_draw_rot = $07FF
PPUCTRL = $2000
PPUMASK = $2001
PPUSTATUS = $2002
OAMADDR = $2003
PPUSCROLL = $2005
PPUADDR = $2006
PPUDATA = $2007
DMC_FREQ = $4010
OAMDMA = $4014
JOY1 = $4016
JOY2_FRAMECNT = $4017
wram_buf = $6000
wram_6001 = $6001
wram_6002 = $6002
wram_6003 = $6003
wram_6004 = $6004
wram_6005 = $6005
wram_6006 = $6006
wram_6007 = $6007
attr_shadow = $6100
pal_buf = $6140
wram_6150 = $6150
ppu_queue = $6200
ppu_queue_1 = $6201
ppu_queue_2 = $6202
ppu_queue_3 = $6203
visited_map = $62F0
area_w = $62FC
area_h = $62FD
area_f62FE = $62FE
area_f62FF = $62FF
screen_grid = $6300
story_flags = $6480
story_flags_F040 = $6488
story_flags_F050 = $648A
screen_flags = $64C0
npc_state = $6580
save_mirror = $6D00
save_mirror_flags = $6E00
save_mirror_npc = $6F00
save_reset_flag = $7001
boot_r7_save = $7005
wram_700E = $700E
wram_700F = $700F
save_csum0_a = $70F0
save_csum0_b = $70F1
save_csum1_a = $70F2
save_csum1_b = $70F3
save_slot0 = $7100
save_slot1 = $7700
save_cur = $7D00
save_tag = $7DFD
save_flags = $7E00
save_npc_state = $7F00
ObjSpriteDraw = $800E
SndInit = $8020
IntroColdBoot = $8028
SndCommand = $8048
NpcHidden = $80A0
SndTick = $8186
ChestPresence = $85BF
PaletteBuild = $8C0E
Bank26_FadeA = $8C3D
Bank26_FadeB = $8C49
HudHpBar = $8CC0
HudRedraw = $8E46
AreaPtrTable = $9201
AreaPtrTableHi = $9301
TempAddOffset = $9824
TempAddStep = $9861
TempToObjPos = $9897
ObjPosToTemp = $98A8
ObjBgProbe = $9A30
SpendMp = $9AE8
PlayerPlaceBody = $9CEF
EndingInit = $A000
SaveJumpTable = $BC00
NewSaveTemplate = $BE00
BitMask = $C000
sub_30_C008 = $C008
PlaySound = $C125
WaitFrame = $C169
sub_30_C176 = $C176
ObjInit = $C25D
SwitchBank16 = $C40E
SetR6 = $C418
SetR7 = $C427
sub_30_C436 = $C436
sub_30_C43E = $C43E
WaitPpuQueue = $C676
PpuQueueFlush = $C67D
sub_30_C72B = $C72B
sub_30_C739 = $C739
AreaSubTable = $C7A5
sub_30_C831 = $C831
MainLoop = $C900
ShowMessage = $D354

    .org $E000
    .db $00,$60                         ; 31:E000
    LDA #$A0                            ; E002  a9 a0
    STA wram_6001                       ; E004  8d 01 60
    LDA #$CD                            ; E007  a9 cd
    STA wram_6002                       ; E009  8d 02 60
    LDA #$4C                            ; E00C  a9 4c
    STA wram_6003                       ; E00E  8d 03 60
    LDA #$0B                            ; E011  a9 0b
    STA wram_6004                       ; E013  8d 04 60
    LDA #$00                            ; E016  a9 00
    STA wram_6005                       ; E018  8d 05 60
    STA wram_6006                       ; E01B  8d 06 60
    DEC wram_6007                       ; E01E  ce 07 60
    LDA tmp_1F                          ; E021  a5 1f
    BNE loc_31_E026                     ; E023  d0 01
    RTS                                 ; E025  60
loc_31_E026:
    LDA #$CC                            ; E026  a9 cc
    STA wram_6002                       ; E028  8d 02 60
    LDA obj_draw_rot                    ; E02B  ad ff 07
    LSR A                               ; E02E  4a
    LSR A                               ; E02F  4a
    STA wram_6007                       ; E030  8d 07 60
    BNE loc_31_E037                     ; E033  d0 02
    STA tmp_1F                          ; E035  85 1f
loc_31_E037:
    RTS                                 ; E037  60
sub_31_E038:
    LDA #$84                            ; E038  a9 84
    STA wram_buf                        ; E03A  8d 00 60
    LDA #$A0                            ; E03D  a9 a0
    STA wram_6001                       ; E03F  8d 01 60
    LDA #$02                            ; E042  a9 02
    STA wram_6002                       ; E044  8d 02 60
    LDA #$4C                            ; E047  a9 4c
    STA wram_6003                       ; E049  8d 03 60
    LDA #$0B                            ; E04C  a9 0b
    STA wram_6004                       ; E04E  8d 04 60
    LDA #$00                            ; E051  a9 00
    STA wram_6005                       ; E053  8d 05 60
    STA wram_6006                       ; E056  8d 06 60
    STA wram_6007                       ; E059  8d 07 60
    LDA tmp_1F                          ; E05C  a5 1f
    BNE loc_31_E061                     ; E05E  d0 01
    RTS                                 ; E060  60
loc_31_E061:
    LDA obj_draw_rot                    ; E061  ad ff 07
    STA wram_6007                       ; E064  8d 07 60
    LDA tmp_21                          ; E067  a5 21
    ASL A                               ; E069  0a
    ASL A                               ; E06A  0a
    CLC                                 ; E06B  18
    ADC #$2E                            ; E06C  69 2e
    STA wram_6002                       ; E06E  8d 02 60
    LDA #$D0                            ; E071  a9 d0
    STA wram_6003                       ; E073  8d 03 60
    RTS                                 ; E076  60
sub_31_E077:
    LDX #$00                            ; E077  a2 00
loc_31_E079:
    LDA oam_attr,X                      ; E079  bd 02 02
    AND #$FC                            ; E07C  29 fc
    STA oam_attr,X                      ; E07E  9d 02 02
    INX                                 ; E081  e8
    INX                                 ; E082  e8
    INX                                 ; E083  e8
    INX                                 ; E084  e8
    BNE loc_31_E079                     ; E085  d0 f2
    RTS                                 ; E087  60
    .db $3C,$24,$2E,$4C,$09,$00,$00,$00,$6C,$24,$32,$4C,$09,$00,$00,$00; 31:E088
    .db $9C,$24,$36,$4C,$09,$00,$00,$00,$CC,$24,$3A,$4C,$09,$00,$00,$00; 31:E098
    .db $0F,$36,$14,$0F,$0F,$30,$27,$03,$0F,$36,$13,$0F,$0F,$36,$1B,$0F; 31:E0A8
SubstPlayTail:  ; s48: SubstPlay JMP tail: skipped if boss_cam_lock; JSR E75E; every 16 frames slot $0D+($08>>4) if empty -> re-read area record (respawn tick; static RE only)
    LDA boss_cam_lock                   ; E0B8  ad d7 07
    BNE loc_31_E12A                     ; E0BB  d0 6d
    JSR BgChrAnimTick                   ; E0BD  20 5e e7
    LDA tmp_08                          ; E0C0  a5 08
    AND #$0F                            ; E0C2  29 0f
    BNE loc_31_E12A                     ; E0C4  d0 64
    LDA tmp_08                          ; E0C6  a5 08
    LSR A                               ; E0C8  4a
    LSR A                               ; E0C9  4a
    LSR A                               ; E0CA  4a
    LSR A                               ; E0CB  4a
    CLC                                 ; E0CC  18
    ADC #$0D                            ; E0CD  69 0d
    STA tmp_13                          ; E0CF  85 13
    TAX                                 ; E0D1  aa
    LDA obj_active,X                    ; E0D2  bd a0 04
    BEQ loc_31_E0D8                     ; E0D5  f0 01
    RTS                                 ; E0D7  60
loc_31_E0D8:
    JSR SwitchBank16_6                  ; E0D8  20 d4 e3
    LDA area_id                         ; E0DB  a5 6c
    ASL A                               ; E0DD  0a
    TAY                                 ; E0DE  a8
    LDA AreaPtrTable,Y                  ; E0DF  b9 01 92
    STA jmp_trampoline                  ; E0E2  85 10
    LDA $9202,Y                         ; E0E4  b9 02 92
    STA tmp_11                          ; E0E7  85 11
    BCC loc_31_E0F5                     ; E0E9  90 0a
    LDA AreaPtrTableHi,Y                ; E0EB  b9 01 93
    STA jmp_trampoline                  ; E0EE  85 10
    LDA $9302,Y                         ; E0F0  b9 02 93
    STA tmp_11                          ; E0F3  85 11
loc_31_E0F5:
    LDA tmp_11                          ; E0F5  a5 11
    BPL loc_31_E12A                     ; E0F7  10 31
    LDY #$05                            ; E0F9  a0 05
    LDX #$0D                            ; E0FB  a2 0d
loc_31_E0FD:
    LDA (jmp_trampoline),Y              ; E0FD  b1 10
    EOR #$F0                            ; E0FF  49 f0
    AND #$F0                            ; E101  29 f0
    BEQ loc_31_E12A                     ; E103  f0 25
    CPX tmp_13                          ; E105  e4 13
    BNE loc_31_E11E                     ; E107  d0 15
    INY                                 ; E109  c8
    LDA (jmp_trampoline),Y              ; E10A  b1 10
    DEY                                 ; E10C  88
    ASL A                               ; E10D  0a
    BCC loc_31_E12A                     ; E10E  90 1a
    JSR AreaSpawnEntry                  ; E110  20 b6 e1
    JSR sub_31_E12B                     ; E113  20 2b e1
    BCS loc_31_E12A                     ; E116  b0 12
    LDA #$00                            ; E118  a9 00
    STA obj_active,X                    ; E11A  9d a0 04
    RTS                                 ; E11D  60
loc_31_E11E:
    INY                                 ; E11E  c8
    INY                                 ; E11F  c8
    INY                                 ; E120  c8
    INY                                 ; E121  c8
    INX                                 ; E122  e8
    CPX #$1D                            ; E123  e0 1d
    BEQ loc_31_E12A                     ; E125  f0 03
    JMP loc_31_E0FD                     ; E127  4c fd e0
loc_31_E12A:
    RTS                                 ; E12A  60
sub_31_E12B:
    LDA obj_x_lo,X                      ; E12B  b5 70
    SEC                                 ; E12D  38
    SBC scroll_shadow0                  ; E12E  e5 02
    LDA obj_x_hi,X                      ; E130  b5 90
    SBC cam_x_hi                        ; E132  e5 03
    BNE loc_31_E142                     ; E134  d0 0c
    LDA obj_y_lo,X                      ; E136  b5 b0
    CMP cam_y_lo                        ; E138  c5 04
    LDA obj_y_hi,X                      ; E13A  b5 d0
    SBC cam_y_hi                        ; E13C  e5 05
    BNE loc_31_E142                     ; E13E  d0 02
    CLC                                 ; E140  18
    RTS                                 ; E141  60
loc_31_E142:
    SEC                                 ; E142  38
    RTS                                 ; E143  60
AreaLoadA:  ; $18=1 then AreaLoad
    LDA #$01                            ; E144  a9 01
    BNE AreaLoad                        ; E146  d0 02
AreaLoadB:  ; $18=0 then AreaLoad
    LDA #$00                            ; E148  a9 00
AreaLoad:  ; SwitchBank16(6); ptr=AreaPtrTable[$6C]; falls into LoadAreaChrRecord
    STA tmp_18                          ; E14A  85 18
    JSR SwitchBank16_6                  ; E14C  20 d4 e3
    LDA area_id                         ; E14F  a5 6c
    ASL A                               ; E151  0a
    TAY                                 ; E152  a8
    LDA AreaPtrTable,Y                  ; E153  b9 01 92
    STA jmp_trampoline                  ; E156  85 10
    LDA $9202,Y                         ; E158  b9 02 92
    STA tmp_11                          ; E15B  85 11
    BCC loc_31_E169                     ; E15D  90 0a
    LDA AreaPtrTableHi,Y                ; E15F  b9 01 93
    STA jmp_trampoline                  ; E162  85 10
    LDA $9302,Y                         ; E164  b9 02 93
    STA tmp_11                          ; E167  85 11
loc_31_E169:
    LDA tmp_11                          ; E169  a5 11
    BPL loc_31_E1AE                     ; E16B  10 41
LoadAreaChrRecord:  ; (ptr $10) record: 00 then $07E6 $07E7 R4 R5 + 4-byte spawn entries from slot $0D; FF end FE skip
    LDY #$00                            ; E16D  a0 00
    LDA (jmp_trampoline),Y              ; E16F  b1 10
    BEQ loc_31_E174                     ; E171  f0 01
    RTS                                 ; E173  60
loc_31_E174:
    INY                                 ; E174  c8
    LDA (jmp_trampoline),Y              ; E175  b1 10
    STA pal_id_spr2                     ; E177  8d e6 07
    INY                                 ; E17A  c8
    LDA (jmp_trampoline),Y              ; E17B  b1 10
    STA pal_id_spr3                     ; E17D  8d e7 07
    INY                                 ; E180  c8
    LDA (jmp_trampoline),Y              ; E181  b1 10
    STA chr_r4_play                     ; E183  8d f4 07
    INY                                 ; E186  c8
    LDA (jmp_trampoline),Y              ; E187  b1 10
    STA chr_r5_play                     ; E189  8d f5 07
    INY                                 ; E18C  c8
    LDX #$0D                            ; E18D  a2 0d
loc_31_E18F:
    LDA (jmp_trampoline),Y              ; E18F  b1 10
    CMP #$FF                            ; E191  c9 ff
    BEQ loc_31_E1AE                     ; E193  f0 19
    CMP #$FE                            ; E195  c9 fe
    BEQ loc_31_E1AF                     ; E197  f0 16
    INY                                 ; E199  c8
    LDA (jmp_trampoline),Y              ; E19A  b1 10
    DEY                                 ; E19C  88
    ASL A                               ; E19D  0a
    BCC loc_31_E1A7                     ; E19E  90 07
    INY                                 ; E1A0  c8
    INY                                 ; E1A1  c8
    INY                                 ; E1A2  c8
    INY                                 ; E1A3  c8
    INX                                 ; E1A4  e8
    BNE loc_31_E18F                     ; E1A5  d0 e8
loc_31_E1A7:
    JSR AreaSpawnEntry                  ; E1A7  20 b6 e1
    INX                                 ; E1AA  e8
    JMP loc_31_E18F                     ; E1AB  4c 8f e1
loc_31_E1AE:
    RTS                                 ; E1AE  60
loc_31_E1AF:
    INY                                 ; E1AF  c8
    INY                                 ; E1B0  c8
    INY                                 ; E1B1  c8
    INY                                 ; E1B2  c8
    INX                                 ; E1B3  e8
    BNE loc_31_E18F                     ; E1B4  d0 d9
AreaSpawnEntry:  ; decode 4-byte spawn entry for slot X then JMP (SpawnHandlerTable[b2&7])
    LDA (jmp_trampoline),Y              ; E1B6  b1 10
    STA tmp_2C                          ; E1B8  85 2c
    INY                                 ; E1BA  c8
    LDA (jmp_trampoline),Y              ; E1BB  b1 10
    STA tmp_2D                          ; E1BD  85 2d
    INY                                 ; E1BF  c8
    LDA (jmp_trampoline),Y              ; E1C0  b1 10
    STA tmp_2E                          ; E1C2  85 2e
    INY                                 ; E1C4  c8
    LDA (jmp_trampoline),Y              ; E1C5  b1 10
    STA tmp_2F                          ; E1C7  85 2f
    INY                                 ; E1C9  c8
    LDA tmp_2C                          ; E1CA  a5 2c
    PHA                                 ; E1CC  48
    LSR A                               ; E1CD  4a
    LSR A                               ; E1CE  4a
    LSR A                               ; E1CF  4a
    LSR A                               ; E1D0  4a
    AND #$0F                            ; E1D1  29 0f
    STA obj_y_hi,X                      ; E1D3  95 d0
    PLA                                 ; E1D5  68
    ASL A                               ; E1D6  0a
    ASL A                               ; E1D7  0a
    ASL A                               ; E1D8  0a
    ASL A                               ; E1D9  0a
    ORA #$0C                            ; E1DA  09 0c
    STA obj_y_lo,X                      ; E1DC  95 b0
    LDA tmp_2E                          ; E1DE  a5 2e
    AND #$40                            ; E1E0  29 40
    LSR A                               ; E1E2  4a
    LSR A                               ; E1E3  4a
    LSR A                               ; E1E4  4a
    STA tmp_20                          ; E1E5  85 20
    LDA tmp_2D                          ; E1E7  a5 2d
    PHA                                 ; E1E9  48
    LSR A                               ; E1EA  4a
    LSR A                               ; E1EB  4a
    LSR A                               ; E1EC  4a
    LSR A                               ; E1ED  4a
    AND #$07                            ; E1EE  29 07
    STA obj_x_hi,X                      ; E1F0  95 90
    PLA                                 ; E1F2  68
    SEC                                 ; E1F3  38
    ROL A                               ; E1F4  2a
    ASL A                               ; E1F5  0a
    ASL A                               ; E1F6  0a
    ASL A                               ; E1F7  0a
    CLC                                 ; E1F8  18
    ADC tmp_20                          ; E1F9  65 20
    STA obj_x_lo,X                      ; E1FB  95 70
    STY tmp_14                          ; E1FD  84 14
    LDA tmp_2E                          ; E1FF  a5 2e
    AND #$07                            ; E201  29 07
    ASL A                               ; E203  0a
    TAY                                 ; E204  a8
    LDA SpawnHandlerTable,Y             ; E205  b9 18 e2
    STA tmp_12                          ; E208  85 12
    LDA $E219,Y                         ; E20A  b9 19 e2
    STA tmp_13                          ; E20D  85 13
    LDA jmp_trampoline                  ; E20F  a5 10
    PHA                                 ; E211  48
    LDA tmp_11                          ; E212  a5 11
    PHA                                 ; E214  48
    JMP (tmp_12)                        ; E215  6c 12 00
SpawnHandlerTable:  ; 5 x ptr16 spawn handlers (entries 5-7 are code bytes)
    .dw SpawnH0                         ; 31:E218
    .dw SpawnH1_Indirect                ; 31:E21A
    .dw SpawnH2                         ; 31:E21C
    .dw SpawnH3                         ; 31:E21E
    .dw SpawnH4_Item                    ; 31:E220
SpawnH4_Item:  ; type=p; p&F0=$40 flag item (WRAM $6480 bit set -> p=$4E)
    LDA tmp_2F                          ; E222  a5 2f
    AND #$F0                            ; E224  29 f0
    CMP #$40                            ; E226  c9 40
    BEQ loc_31_E239                     ; E228  f0 0f
loc_31_E22A:
    LDA tmp_2F                          ; E22A  a5 2f
    STA tmp_11                          ; E22C  85 11
    STX jmp_trampoline                  ; E22E  86 10
    JSR ObjInit                         ; E230  20 5d c2
    JSR SpawnSetTileHi                  ; E233  20 c7 e3
    JMP SpawnCommonTail                 ; E236  4c 8e e2
loc_31_E239:
    LDA area_id                         ; E239  a5 6c
    AND #$07                            ; E23B  29 07
    CLC                                 ; E23D  18
    ADC #$09                            ; E23E  69 09
    STA pal_id_spr2                     ; E240  8d e6 07
    STA pal_id_spr3                     ; E243  8d e7 07
    LDA #$84                            ; E246  a9 84
    LDY area_id                         ; E248  a4 6c
    CPY #$C2                            ; E24A  c0 c2
    BEQ loc_31_E264                     ; E24C  f0 16
    CPY #$C3                            ; E24E  c0 c3
    BEQ loc_31_E264                     ; E250  f0 12
    CPY #$C4                            ; E252  c0 c4
    BEQ loc_31_E264                     ; E254  f0 0e
    LDA #$27                            ; E256  a9 27
    CPY #$F5                            ; E258  c0 f5
    BEQ loc_31_E264                     ; E25A  f0 08
    CPY #$F6                            ; E25C  c0 f6
    BEQ loc_31_E264                     ; E25E  f0 04
    CPY #$F7                            ; E260  c0 f7
    BNE loc_31_E22A                     ; E262  d0 c6
loc_31_E264:
    PHA                                 ; E264  48
    LSR A                               ; E265  4a
    LSR A                               ; E266  4a
    LSR A                               ; E267  4a
    TAY                                 ; E268  a8
    LDA story_flags,Y                   ; E269  b9 80 64
    STA jmp_trampoline                  ; E26C  85 10
    PLA                                 ; E26E  68
    AND #$07                            ; E26F  29 07
    TAY                                 ; E271  a8
    LDA BitMask,Y                       ; E272  b9 00 c0
    AND jmp_trampoline                  ; E275  25 10
    BEQ loc_31_E22A                     ; E277  f0 b1
    LDA #$4E                            ; E279  a9 4e
    STA tmp_2F                          ; E27B  85 2f
    BNE loc_31_E22A                     ; E27D  d0 ab
SpawnH0:  ; type=p+$50
    LDA tmp_2F                          ; E27F  a5 2f
    CLC                                 ; E281  18
    ADC #$50                            ; E282  69 50
    STA tmp_11                          ; E284  85 11
    STX jmp_trampoline                  ; E286  86 10
    JSR ObjInit                         ; E288  20 5d c2
    JSR SpawnSetTileHi                  ; E28B  20 c7 e3
SpawnCommonTail:  ; $0540=b2&3 $0560=p; restore Y/$10/$11
    LDA tmp_2E                          ; E28E  a5 2e
    AND #$03                            ; E290  29 03
    STA obj_spawn_b2,X                  ; E292  9d 40 05
    LDA tmp_2F                          ; E295  a5 2f
    STA obj_spawn_param,X               ; E297  9d 60 05
    LDY tmp_14                          ; E29A  a4 14
    PLA                                 ; E29C  68
    STA tmp_11                          ; E29D  85 11
    PLA                                 ; E29F  68
    STA jmp_trampoline                  ; E2A0  85 10
    RTS                                 ; E2A2  60
SpawnH3:  ; type=p+$D0
    LDA tmp_2F                          ; E2A3  a5 2f
    CLC                                 ; E2A5  18
    ADC #$D0                            ; E2A6  69 d0
    STA tmp_11                          ; E2A8  85 11
    STX jmp_trampoline                  ; E2AA  86 10
    JSR ObjInit                         ; E2AC  20 5d c2
    JSR SpawnSetTileHi                  ; E2AF  20 c7 e3
    JMP SpawnCommonTail                 ; E2B2  4c 8e e2
SpawnH1_Indirect:  ; 4-byte rec from banks 4/5 at $80F0+; type $30 or rec[1]
    LDA #$02                            ; E2B5  a9 02
    JSR SwitchBank16                    ; E2B7  20 0e c4
    LDA tmp_2F                          ; E2BA  a5 2f
    AND #$03                            ; E2BC  29 03
    ASL A                               ; E2BE  0a
    ASL A                               ; E2BF  0a
    ORA #$F0                            ; E2C0  09 f0
    STA tmp_24                          ; E2C2  85 24
    LDA tmp_2F                          ; E2C4  a5 2f
    LSR A                               ; E2C6  4a
    LSR A                               ; E2C7  4a
    ORA #$80                            ; E2C8  09 80
    STA tmp_25                          ; E2CA  85 25
    LDY #$03                            ; E2CC  a0 03
loc_31_E2CE:
    LDA (tmp_24),Y                      ; E2CE  b1 24
    STA a:tmp_20,Y                      ; E2D0  99 20 00
    DEY                                 ; E2D3  88
    BPL loc_31_E2CE                     ; E2D4  10 f8
    LDA tmp_20                          ; E2D6  a5 20
    CMP #$FF                            ; E2D8  c9 ff
    BNE loc_31_E2DF                     ; E2DA  d0 03
    JMP SpawnH1_TypeRec                 ; E2DC  4c 63 e3
loc_31_E2DF:
    JSR SwitchBank16_6                  ; E2DF  20 d4 e3
    LDA #$30                            ; E2E2  a9 30
    STA tmp_11                          ; E2E4  85 11
    STX jmp_trampoline                  ; E2E6  86 10
    JSR ObjInit                         ; E2E8  20 5d c2
    JSR SpawnSetTileHi                  ; E2EB  20 c7 e3
    LDA tmp_20                          ; E2EE  a5 20
    STA obj_param1,X                    ; E2F0  9d 80 06
    LDA tmp_21                          ; E2F3  a5 21
    STA obj_param2,X                    ; E2F5  9d a0 06
    LDA tmp_22                          ; E2F8  a5 22
    BPL loc_31_E307                     ; E2FA  10 0b
    AND #$7F                            ; E2FC  29 7f
    STA obj_active,X                    ; E2FE  9d a0 04
    STA obj_behav,X                     ; E301  9d e0 06
    JMP loc_31_E311                     ; E304  4c 11 e3
loc_31_E307:
    STA obj_behav,X                     ; E307  9d e0 06
    LDA tmp_22                          ; E30A  a5 22
    AND #$70                            ; E30C  29 70
    STA obj_tile_ofs,X                  ; E30E  9d 20 03
loc_31_E311:
    LDA obj_behav,X                     ; E311  bd e0 06
    AND #$04                            ; E314  29 04
    BNE loc_31_E323                     ; E316  d0 0b
    LDA tmp_2F                          ; E318  a5 2f
    AND #$03                            ; E31A  29 03
    TAY                                 ; E31C  a8
    LDA $E35F,Y                         ; E31D  b9 5f e3
    STA obj_phase,X                     ; E320  9d 00 06
loc_31_E323:
    LDA tmp_22                          ; E323  a5 22
    AND #$08                            ; E325  29 08
    BEQ loc_31_E32F                     ; E327  f0 06
    LDA obj_y_lo,X                      ; E329  b5 b0
    ORA #$0E                            ; E32B  09 0e
    STA obj_y_lo,X                      ; E32D  95 b0
loc_31_E32F:
    LDA tmp_23                          ; E32F  a5 23
    STA obj_meta_id,X                   ; E331  9d 00 03
    STA orbit_level,X                   ; E334  9d c0 06
    TXA                                 ; E337  8a
    AND #$03                            ; E338  29 03
    CLC                                 ; E33A  18
    ADC #$01                            ; E33B  69 01
    STA obj_freeze,X                    ; E33D  9d a0 05
SpawnH1_Tail:  ; $23=p; SwitchBank16(7)+JSR 14:80A0; $20!=0 -> obj_active=0; SpawnCommonTail
    LDA tmp_2F                          ; E340  a5 2f
    STA tmp_23                          ; E342  85 23
    STX tmp_15                          ; E344  86 15
    LDA #$07                            ; E346  a9 07
    JSR SwitchBank16                    ; E348  20 0e c4
    JSR NpcHidden                       ; E34B  20 a0 80
    JSR SwitchBank16_6                  ; E34E  20 d4 e3
    LDX tmp_15                          ; E351  a6 15
    LDA tmp_20                          ; E353  a5 20
    BEQ loc_31_E35C                     ; E355  f0 05
    LDA #$00                            ; E357  a9 00
    STA obj_active,X                    ; E359  9d a0 04
loc_31_E35C:
    JMP SpawnCommonTail                 ; E35C  4c 8e e2
    .db $DA,$22,$4A,$AA                 ; 31:E35F
SpawnH1_TypeRec:  ; rec[0]=$FF: ObjInit type rec[1] then JMP SpawnH1_Tail (no y|=$0E/meta/tile add; s42)
    JSR SwitchBank16_6                  ; E363  20 d4 e3
    LDA tmp_21                          ; E366  a5 21
    STA tmp_11                          ; E368  85 11
    STX jmp_trampoline                  ; E36A  86 10
    JSR ObjInit                         ; E36C  20 5d c2
    JSR SpawnSetTileHi                  ; E36F  20 c7 e3
    JMP SpawnH1_Tail                    ; E372  4c 40 e3
SpawnH2:  ; type $0D (+bank14 $85BF) or $0E if p bit7
    LDA tmp_2F                          ; E375  a5 2f
    BMI loc_31_E3B8                     ; E377  30 3f
    STX jmp_trampoline                  ; E379  86 10
    LDA #$0D                            ; E37B  a9 0d
    STA tmp_11                          ; E37D  85 11
    JSR ObjInit                         ; E37F  20 5d c2
    STX tmp_15                          ; E382  86 15
    LDA #$07                            ; E384  a9 07
    JSR SwitchBank16                    ; E386  20 0e c4
    LDA tmp_2F                          ; E389  a5 2f
    STA tmp_23                          ; E38B  85 23
    JSR ChestPresence                   ; E38D  20 bf 85
    JSR SwitchBank16_6                  ; E390  20 d4 e3
    LDX tmp_15                          ; E393  a6 15
    LDA tmp_20                          ; E395  a5 20
    BNE loc_31_E39C                     ; E397  d0 03
    STA obj_active,X                    ; E399  9d a0 04
loc_31_E39C:
    JSR SpawnSetTileHi                  ; E39C  20 c7 e3
    LDA tmp_2F                          ; E39F  a5 2f
    CMP #$25                            ; E3A1  c9 25
    BEQ loc_31_E3B0                     ; E3A3  f0 0b
    CMP #$3C                            ; E3A5  c9 3c
    BEQ loc_31_E3B0                     ; E3A7  f0 07
    CMP #$3B                            ; E3A9  c9 3b
    BEQ loc_31_E3B0                     ; E3AB  f0 03
    JMP SpawnCommonTail                 ; E3AD  4c 8e e2
loc_31_E3B0:
    LDA #$4C                            ; E3B0  a9 4c
    STA obj_meta_id,X                   ; E3B2  9d 00 03
    JMP SpawnCommonTail                 ; E3B5  4c 8e e2
loc_31_E3B8:
    STX jmp_trampoline                  ; E3B8  86 10
    LDA #$0E                            ; E3BA  a9 0e
    STA tmp_11                          ; E3BC  85 11
    JSR ObjInit                         ; E3BE  20 5d c2
    JSR SpawnSetTileHi                  ; E3C1  20 c7 e3
    JMP SpawnCommonTail                 ; E3C4  4c 8e e2
SpawnSetTileHi:  ; $0380 |= (b2 bit7)>>2
    LDA tmp_2E                          ; E3C7  a5 2e
    AND #$80                            ; E3C9  29 80
    LSR A                               ; E3CB  4a
    LSR A                               ; E3CC  4a
    ORA obj_flags,X                     ; E3CD  1d 80 03
    STA obj_flags,X                     ; E3D0  9d 80 03
    RTS                                 ; E3D3  60
SwitchBank16_6:  ; tail-call SwitchBank16(6) -> banks 12/13 (restores default after n=7 entity calls)
    LDA #$06                            ; E3D4  a9 06
    JMP SwitchBank16                    ; E3D6  4c 0e c4
AreaEntryTable:  ; s46: 8 entries by ($6D&$E0)>>5: E422 normal / E51C keep pos / E533 save+load / E544 pit fall / 0000 x3 / E41C no record (was JmpTable31_E3D9)
    .dw sub_31_E422                     ; 31:E3D9
    .dw sub_31_E51C                     ; 31:E3DB
    .dw sub_31_E533                     ; 31:E3DD
    .dw AreaPitFall                     ; 31:E3DF
    .dw $0000                           ; 31:E3E1
    .dw $0000                           ; 31:E3E3
    .dw $0000                           ; 31:E3E5
    .dw sub_31_E41C                     ; 31:E3E7
AreaEnter:  ; s60: sprite pass + wait PPU queue + irq_mode/boss_cam_lock/$07D6=0 then the area load (substate 1)
    JSR FarCall_B14_800E                ; E3E9  20 8c ff
    JSR WaitPpuQueue                    ; E3EC  20 76 c6
    LDA #$00                            ; E3EF  a9 00
    STA irq_mode                        ; E3F1  85 51
    STA boss_cam_lock                   ; E3F3  8d d7 07
    STA ram_07D6                        ; E3F6  8d d6 07
    LDA #$1A                            ; E3F9  a9 1a
    JSR SetR6                           ; E3FB  20 18 c4
    BIT pal_nofade_mask                 ; E3FE  2c e8 07
    BMI loc_31_E406                     ; E401  30 03
    JSR PaletteBuild                    ; E403  20 0e 8c
loc_31_E406:
    LDA area_entry                      ; E406  a5 6d
    AND #$E0                            ; E408  29 e0
AreaEntryDispatch:  ; s46: JMP (AreaEntryTable[($6D&$E0)>>4])
    LSR A                               ; E40A  4a
    LSR A                               ; E40B  4a
    LSR A                               ; E40C  4a
    LSR A                               ; E40D  4a
    TAY                                 ; E40E  a8
    LDA AreaEntryTable,Y                ; E40F  b9 d9 e3
    STA jmp_trampoline                  ; E412  85 10
    LDA $E3DA,Y                         ; E414  b9 da e3
    STA tmp_11                          ; E417  85 11
    JMP (jmp_trampoline)                ; E419  6c 10 00
sub_31_E41C:
    JSR sub_31_E61B                     ; E41C  20 1b e6
    JMP loc_31_E439                     ; E41F  4c 39 e4
sub_31_E422:
    JSR sub_31_E611                     ; E422  20 11 e6
    LDA obj1_flash                      ; E425  ad 10 07
    AND #$0F                            ; E428  29 0f
    CMP #$04                            ; E42A  c9 04
    BNE loc_31_E439                     ; E42C  d0 0b
    LDA obj1_flash                      ; E42E  ad 10 07
    AND #$F0                            ; E431  29 f0
    STA obj1_flash                      ; E433  8d 10 07
    JSR sub_30_C008                     ; E436  20 08 c0
loc_31_E439:
    JSR AreaLoadMap                     ; E439  20 28 e6
    LDA #$0D                            ; E43C  a9 0d
    JSR SwitchBank16                    ; E43E  20 0e c4
    LDX #$00                            ; E441  a2 00
    JSR TempToObjPos                    ; E443  20 97 98
    INX                                 ; E446  e8
    JSR TempToObjPos                    ; E447  20 97 98
loc_31_E44A:
    JSR sub_31_E799                     ; E44A  20 99 e7
    JSR AreaFollowerHook                ; E44D  20 c4 e7
    JSR sub_31_E823                     ; E450  20 23 e8
    LDA #$00                            ; E453  a9 00
    STA scroll_shadow0                  ; E455  85 02
    STA cam_y_lo                        ; E457  85 04
    LDA #$80                            ; E459  a9 80
    STA tmp_30                          ; E45B  85 30
    LDA #$80                            ; E45D  a9 80
    STA tmp_31                          ; E45F  85 31
    LDA #$0D                            ; E461  a9 0d
    JSR SwitchBank16                    ; E463  20 0e c4
    JSR TempAddOffset                   ; E466  20 24 98
    JSR CameraClampX                    ; E469  20 72 ea
    LDX #$03                            ; E46C  a2 03
loc_31_E46E:
    LDA pos_x_lo,X                      ; E46E  b5 34
    STA zp_3C,X                         ; E470  95 3c
    DEX                                 ; E472  ca
    BPL loc_31_E46E                     ; E473  10 f9
    LDA sub_state                       ; E475  a5 41
    CMP #$14                            ; E477  c9 14
    BEQ loc_31_E489                     ; E479  f0 0e
    CMP #$06                            ; E47B  c9 06
    BEQ loc_31_E489                     ; E47D  f0 0a
    LDA #$0D                            ; E47F  a9 0d
    JSR SwitchBank16                    ; E481  20 0e c4
    LDX #$01                            ; E484  a2 01
    JSR PlayerPlaceBody                 ; E486  20 ef 9c
loc_31_E489:
    LDX #$1F                            ; E489  a2 1f
    LDA #$FF                            ; E48B  a9 ff
loc_31_E48D:
    STA ppu_queue,X                     ; E48D  9d 00 62
    DEX                                 ; E490  ca
    BNE loc_31_E48D                     ; E491  d0 fa
    JSR CameraTick                      ; E493  20 f6 e8
    LDA #$00                            ; E496  a9 00
    STA cam_ofs_x                       ; E498  85 3a
    STA cam_ofs_y                       ; E49A  85 3b
    LDX #$03                            ; E49C  a2 03
loc_31_E49E:
    LDA zp_3C,X                         ; E49E  b5 3c
    STA pos_x_lo,X                      ; E4A0  95 34
    DEX                                 ; E4A2  ca
    BPL loc_31_E49E                     ; E4A3  10 f9
    LDA #$00                            ; E4A5  a9 00
    STA frame_ready                     ; E4A7  85 09
    LDA #$80                            ; E4A9  a9 80
    STA irq_mode                        ; E4AB  85 51
    LDA ppumask_shadow                  ; E4AD  a5 01
    AND #$06                            ; E4AF  29 06
    STA ppumask_shadow                  ; E4B1  85 01
    JSR WaitFrame                       ; E4B3  20 69 c1
    JSR WaitPpuQueue                    ; E4B6  20 76 c6
    LDA #$01                            ; E4B9  a9 01
    JSR sub_30_C831                     ; E4BB  20 31 c8
    JSR WaitPpuQueue                    ; E4BE  20 76 c6
    LDX #$08                            ; E4C1  a2 08
loc_31_E4C3:
    TXA                                 ; E4C3  8a
    PHA                                 ; E4C4  48
    JSR CameraTick                      ; E4C5  20 f6 e8
    PLA                                 ; E4C8  68
    TAX                                 ; E4C9  aa
    DEX                                 ; E4CA  ca
    BPL loc_31_E4C3                     ; E4CB  10 f6
    LDA #$0E                            ; E4CD  a9 0e
    JSR SwitchBank16                    ; E4CF  20 0e c4
    JSR ObjSpriteDraw                   ; E4D2  20 0e 80
    JSR WaitFrame                       ; E4D5  20 69 c1
    LDA #$00                            ; E4D8  a9 00
    STA irq_mode                        ; E4DA  85 51
    LDA ppumask_shadow                  ; E4DC  a5 01
    ORA #$18                            ; E4DE  09 18
    STA ppumask_shadow                  ; E4E0  85 01
    JSR WaitFrame                       ; E4E2  20 69 c1
    BIT pal_nofade_mask                 ; E4E5  2c e8 07
    BMI loc_31_E4EF                     ; E4E8  30 05
    LDA #$2F                            ; E4EA  a9 2f
    JSR PlaySound                       ; E4EC  20 25 c1
loc_31_E4EF:
    LDA #$1A                            ; E4EF  a9 1a
    JSR SetR6                           ; E4F1  20 18 c4
    BIT pal_nofade_mask                 ; E4F4  2c e8 07
    BMI loc_31_E4FE                     ; E4F7  30 05
    LDA #$02                            ; E4F9  a9 02
    JSR Bank26_FadeB                    ; E4FB  20 49 8c
loc_31_E4FE:
    LDA #$17                            ; E4FE  a9 17
    JSR SetR7                           ; E500  20 27 c4
    JSR SaveJumpTable                   ; E503  20 00 bc
    LDA #$04                            ; E506  a9 04
    STA obj_freeze                      ; E508  8d a0 05
    LDA obj_flags_s01                   ; E50B  ad 81 03
    ORA #$04                            ; E50E  09 04
    STA obj_flags_s01                   ; E510  8d 81 03
    LDA invuln_timer                    ; E513  ad 1a 07
    ORA #$04                            ; E516  09 04
    STA invuln_timer                    ; E518  8d 1a 07
    RTS                                 ; E51B  60
sub_31_E51C:
    LDX #$0D                            ; E51C  a2 0d
    LDA #$00                            ; E51E  a9 00
loc_31_E520:
    STA obj_active,X                    ; E520  9d a0 04
    INX                                 ; E523  e8
    CPX #$20                            ; E524  e0 20
    BNE loc_31_E520                     ; E526  d0 f8
    JSR AreaLoadMap                     ; E528  20 28 e6
    LDA #$1A                            ; E52B  a9 1a
    JSR SetR6                           ; E52D  20 18 c4
    JMP PaletteBuild                    ; E530  4c 0e 8c
sub_31_E533:
    LDA #$17                            ; E533  a9 17
    JSR SetR7                           ; E535  20 27 c4
    JSR SaveJumpTable                   ; E538  20 00 bc
    JSR sub_31_E845                     ; E53B  20 45 e8
    JSR sub_31_E61B                     ; E53E  20 1b e6
    JMP loc_31_E439                     ; E541  4c 39 e4
AreaPitFall:  ; s46: area_entry $6x: slide slots 0/1 down 8 px/frame while probe b0; then AreaEdgeLink (k5)
    JSR WaitFrame                       ; E544  20 69 c1
    LDA #$0D                            ; E547  a9 0d
    JSR SwitchBank16                    ; E549  20 0e c4
    LDX #$01                            ; E54C  a2 01
    JSR ObjPosToTemp                    ; E54E  20 a8 98
    LDA #$08                            ; E551  a9 08
    STA tmp_31                          ; E553  85 31
    LDA #$00                            ; E555  a9 00
    STA tmp_30                          ; E557  85 30
    JSR TempAddStep                     ; E559  20 61 98
    LDA #$40                            ; E55C  a9 40
    JSR ObjBgProbe                      ; E55E  20 30 9a
    LDA tmp_20                          ; E561  a5 20
    LSR A                               ; E563  4a
    BCC AreaEdgeLink                    ; E564  90 0d
    JSR TempToObjPos                    ; E566  20 97 98
    DEX                                 ; E569  ca
    JSR TempToObjPos                    ; E56A  20 97 98
    JSR FarCall_B14_800E                ; E56D  20 8c ff
    JMP AreaPitFall                     ; E570  4c 44 e5
AreaEdgeLink:  ; area sub-table 5 (10 areas): pit/edge link to another area
    JSR sub_31_E611                     ; E573  20 11 e6
    LDA #$05                            ; E576  a9 05
    JSR AreaSubTable                    ; E578  20 a5 c7
loc_31_E57B:
    LDX #$00                            ; E57B  a2 00
    LDA #$04                            ; E57D  a9 04
    STA tmp_12                          ; E57F  85 12
loc_31_E581:
    LDA (jmp_trampoline),Y              ; E581  b1 10
    STA tmp_20,X                        ; E583  95 20
    INX                                 ; E585  e8
    INY                                 ; E586  c8
    BEQ loc_31_E603                     ; E587  f0 7a
    DEC tmp_12                          ; E589  c6 12
    BNE loc_31_E581                     ; E58B  d0 f4
    LDA tmp_21                          ; E58D  a5 21
    LSR A                               ; E58F  4a
    LSR A                               ; E590  4a
    LSR A                               ; E591  4a
    LSR A                               ; E592  4a
    CMP obj_x_hi                        ; E593  c5 90
    BNE loc_31_E57B                     ; E595  d0 e4
    LDA tmp_22                          ; E597  a5 22
    AND #$F0                            ; E599  29 f0
    STA tmp_13                          ; E59B  85 13
    LDA tmp_23                          ; E59D  a5 23
    LSR A                               ; E59F  4a
    LSR A                               ; E5A0  4a
    LSR A                               ; E5A1  4a
    LSR A                               ; E5A2  4a
    ORA tmp_13                          ; E5A3  05 13
    CMP obj_y_hi                        ; E5A5  c5 d0
    BNE loc_31_E57B                     ; E5A7  d0 d2
    LDA obj_y_lo                        ; E5A9  a5 b0
    CLC                                 ; E5AB  18
    ADC #$10                            ; E5AC  69 10
    STA obj_y_lo                        ; E5AE  85 b0
    STA obj_y_lo_s01                    ; E5B0  85 b1
    LDA tmp_22                          ; E5B2  a5 22
    ASL A                               ; E5B4  0a
    AND #$F0                            ; E5B5  29 f0
    STA jmp_trampoline                  ; E5B7  85 10
    LDA tmp_23                          ; E5B9  a5 23
    AND #$0F                            ; E5BB  29 0f
    ORA jmp_trampoline                  ; E5BD  05 10
    STA obj_y_hi                        ; E5BF  85 d0
    STA $D1                             ; E5C1  85 d1
    LDA tmp_21                          ; E5C3  a5 21
    AND #$0F                            ; E5C5  29 0f
    STA obj_x_hi                        ; E5C7  85 90
    STA $91                             ; E5C9  85 91
    LDA tmp_20                          ; E5CB  a5 20
    PHA                                 ; E5CD  48
    LDA obj1_flash                      ; E5CE  ad 10 07
    AND #$0F                            ; E5D1  29 0f
    CMP #$04                            ; E5D3  c9 04
    BNE loc_31_E5E2                     ; E5D5  d0 0b
    LDA obj1_flash                      ; E5D7  ad 10 07
    AND #$F0                            ; E5DA  29 f0
    STA obj1_flash                      ; E5DC  8d 10 07
    JSR sub_30_C008                     ; E5DF  20 08 c0
loc_31_E5E2:
    PLA                                 ; E5E2  68
    STA area_id                         ; E5E3  85 6c
    JSR AreaLoadMap                     ; E5E5  20 28 e6
    LDA #$0D                            ; E5E8  a9 0d
    JSR SwitchBank16                    ; E5EA  20 0e c4
    LDX #$00                            ; E5ED  a2 00
    JSR ObjPosToTemp                    ; E5EF  20 a8 98
    INX                                 ; E5F2  e8
    JSR TempToObjPos                    ; E5F3  20 97 98
    LDA #$FF                            ; E5F6  a9 ff
    STA obj_act_timer                   ; E5F8  8d 40 06
    LDA #$30                            ; E5FB  a9 30
    STA obj_timer2                      ; E5FD  8d 20 06
    JMP loc_31_E44A                     ; E600  4c 4a e4
loc_31_E603:
    LDA #$08                            ; E603  a9 08
    STA sub_state                       ; E605  85 41
    LDA #$1A                            ; E607  a9 1a
    JSR SetR6                           ; E609  20 18 c4
    LDA #$04                            ; E60C  a9 04
    JMP Bank26_FadeB                    ; E60E  4c 49 8c
sub_31_E611:
    LDA #$1A                            ; E611  a9 1a
    JSR SetR6                           ; E613  20 18 c4
    LDA #$02                            ; E616  a9 02
    JSR Bank26_FadeA                    ; E618  20 3d 8c
sub_31_E61B:
    LDX #$04                            ; E61B  a2 04
    LDA #$00                            ; E61D  a9 00
loc_31_E61F:
    STA obj_active,X                    ; E61F  9d a0 04
    INX                                 ; E622  e8
    CPX #$1F                            ; E623  e0 1f
    BNE loc_31_E61F                     ; E625  d0 f8
    RTS                                 ; E627  60
AreaLoadMap:  ; area sub-table 0: PlaySound(music) + $62FC-$62FF + screen grid -> $6300
    LDA #$05                            ; E628  a9 05
    JSR SwitchBank16                    ; E62A  20 0e c4
    LDA #$00                            ; E62D  a9 00
    JSR AreaSubTable                    ; E62F  20 a5 c7
    LDA (jmp_trampoline),Y              ; E632  b1 10
    JSR PlaySound                       ; E634  20 25 c1
    LDY #$01                            ; E637  a0 01
    LDA (jmp_trampoline),Y              ; E639  b1 10
    STA area_w                          ; E63B  8d fc 62
    INY                                 ; E63E  c8
    LDA (jmp_trampoline),Y              ; E63F  b1 10
    STA area_h                          ; E641  8d fd 62
    STA tmp_13                          ; E644  85 13
    INY                                 ; E646  c8
    LDA (jmp_trampoline),Y              ; E647  b1 10
    STA area_f62FE                      ; E649  8d fe 62
    INY                                 ; E64C  c8
    LDA (jmp_trampoline),Y              ; E64D  b1 10
    STA area_f62FF                      ; E64F  8d ff 62
    INY                                 ; E652  c8
    LDX #$7F                            ; E653  a2 7f
    LDA #$80                            ; E655  a9 80
loc_31_E657:
    STA screen_grid,X                   ; E657  9d 00 63
    DEX                                 ; E65A  ca
    BPL loc_31_E657                     ; E65B  10 fa
    INX                                 ; E65D  e8
loc_31_E65E:
    LDA area_w                          ; E65E  ad fc 62
    STA tmp_12                          ; E661  85 12
loc_31_E663:
    LDA (jmp_trampoline),Y              ; E663  b1 10
    STA screen_grid,X                   ; E665  9d 00 63
    INX                                 ; E668  e8
    INY                                 ; E669  c8
    DEC tmp_12                          ; E66A  c6 12
    BPL loc_31_E663                     ; E66C  10 f5
    TXA                                 ; E66E  8a
    CLC                                 ; E66F  18
    ADC #$07                            ; E670  69 07
    AND #$F8                            ; E672  29 f8
    TAX                                 ; E674  aa
    DEC tmp_13                          ; E675  c6 13
    BPL loc_31_E65E                     ; E677  10 e5
    LDY #$0B                            ; E679  a0 0b
    LDA #$00                            ; E67B  a9 00
loc_31_E67D:
    STA visited_map,Y                   ; E67D  99 f0 62
    DEY                                 ; E680  88
    BPL loc_31_E67D                     ; E681  10 fa
    JSR AreaRestoreVisited              ; E683  20 02 e7
    LDA #$02                            ; E686  a9 02
    JSR AreaSubTable                    ; E688  20 a5 c7
    LDA area_entry                      ; E68B  a5 6d
    CMP #$FF                            ; E68D  c9 ff
    BNE loc_31_E69E                     ; E68F  d0 0d
    LDA #$1A                            ; E691  a9 1a
    JSR SetR6                           ; E693  20 18 c4
    LDX #$00                            ; E696  a2 00
    JSR ObjPosToTemp                    ; E698  20 a8 98
    JMP AreaLoadGfx                     ; E69B  4c b9 e6
loc_31_E69E:
    LDA area_entry                      ; E69E  a5 6d
    AND #$E0                            ; E6A0  29 e0
    CMP #$20                            ; E6A2  c9 20
    BEQ AreaLoadGfx                     ; E6A4  f0 13
    LDA area_entry                      ; E6A6  a5 6d
    AND #$1F                            ; E6A8  29 1f
    ASL A                               ; E6AA  0a
    ASL A                               ; E6AB  0a
    TAY                                 ; E6AC  a8
    LDX #$00                            ; E6AD  a2 00
loc_31_E6AF:
    LDA (jmp_trampoline),Y              ; E6AF  b1 10
    STA pos_x_lo,X                      ; E6B1  95 34
    INY                                 ; E6B3  c8
    INX                                 ; E6B4  e8
    CPX #$04                            ; E6B5  e0 04
    BNE loc_31_E6AF                     ; E6B7  d0 f6
AreaLoadGfx:  ; area sub-table 1: BG palette ids $07E0-$07E2 + $67/$69 + BG CHR R0/R1 then AreaLoadB
    LDA #$01                            ; E6B9  a9 01
    JSR AreaSubTable                    ; E6BB  20 a5 c7
    LDA (jmp_trampoline),Y              ; E6BE  b1 10
    STA pal_ids                         ; E6C0  8d e0 07
    INY                                 ; E6C3  c8
    LDA (jmp_trampoline),Y              ; E6C4  b1 10
    STA pal_id_bg1                      ; E6C6  8d e1 07
    INY                                 ; E6C9  c8
    LDA (jmp_trampoline),Y              ; E6CA  b1 10
    STA pal_id_bg2                      ; E6CC  8d e2 07
    INY                                 ; E6CF  c8
    LDA #$7F                            ; E6D0  a9 7f
    STA pal_id_bg3                      ; E6D2  8d e3 07
    LDA #$00                            ; E6D5  a9 00
    STA zp_66                           ; E6D7  85 66
    STA zp_68                           ; E6D9  85 68
    LDA (jmp_trampoline),Y              ; E6DB  b1 10
    STA zp_67                           ; E6DD  85 67
    INY                                 ; E6DF  c8
    LDA (jmp_trampoline),Y              ; E6E0  b1 10
    STA zp_69                           ; E6E2  85 69
    INY                                 ; E6E4  c8
    LDA zp_67                           ; E6E5  a5 67
    AND #$3C                            ; E6E7  29 3c
    ASL A                               ; E6E9  0a
    ASL A                               ; E6EA  0a
    ASL A                               ; E6EB  0a
    STA $6A                             ; E6EC  85 6a
    LDA #$BE                            ; E6EE  a9 be
    ADC #$00                            ; E6F0  69 00
    STA $6B                             ; E6F2  85 6b
    LDA (jmp_trampoline),Y              ; E6F4  b1 10
    STA chr_r0_play                     ; E6F6  8d f0 07
    INY                                 ; E6F9  c8
    LDA (jmp_trampoline),Y              ; E6FA  b1 10
    STA chr_r1_play                     ; E6FC  8d f1 07
    JMP AreaLoadB                       ; E6FF  4c 48 e1
AreaRestoreVisited:  ; area sub-table 4: screen visited bits $62F0 from WRAM flags $64C0
    LDA cur_bank16_x2                   ; E702  a5 6e
    PHA                                 ; E704  48
    LDA r7_shadow                       ; E705  a5 6f
    PHA                                 ; E707  48
    LDA #$04                            ; E708  a9 04
    JSR AreaSubTable                    ; E70A  20 a5 c7
loc_31_E70D:
    LDA (jmp_trampoline),Y              ; E70D  b1 10
    CMP #$FF                            ; E70F  c9 ff
    BEQ loc_31_E756                     ; E711  f0 43
    LSR A                               ; E713  4a
    LSR A                               ; E714  4a
    LSR A                               ; E715  4a
    TAX                                 ; E716  aa
    LDA screen_flags,X                  ; E717  bd c0 64
    STA tmp_12                          ; E71A  85 12
    LDA #$00                            ; E71C  a9 00
    STA tmp_13                          ; E71E  85 13
    LDA (jmp_trampoline),Y              ; E720  b1 10
    AND #$07                            ; E722  29 07
    TAX                                 ; E724  aa
    LDA BitMask,X                       ; E725  bd 00 c0
    AND tmp_12                          ; E728  25 12
    BEQ loc_31_E72E                     ; E72A  f0 02
    DEC tmp_13                          ; E72C  c6 13
loc_31_E72E:
    INY                                 ; E72E  c8
    LDA (jmp_trampoline),Y              ; E72F  b1 10
    AND #$07                            ; E731  29 07
    TAX                                 ; E733  aa
    LDA BitMask,X                       ; E734  bd 00 c0
    STA tmp_12                          ; E737  85 12
    LDA (jmp_trampoline),Y              ; E739  b1 10
    LSR A                               ; E73B  4a
    LSR A                               ; E73C  4a
    LSR A                               ; E73D  4a
    LSR A                               ; E73E  4a
    TAX                                 ; E73F  aa
    LDA tmp_12                          ; E740  a5 12
    EOR #$FF                            ; E742  49 ff
    AND visited_map,X                   ; E744  3d f0 62
    STA tmp_14                          ; E747  85 14
    LDA tmp_12                          ; E749  a5 12
    AND tmp_13                          ; E74B  25 13
    ORA tmp_14                          ; E74D  05 14
    STA visited_map,X                   ; E74F  9d f0 62
    INY                                 ; E752  c8
    JMP loc_31_E70D                     ; E753  4c 0d e7
loc_31_E756:
    PLA                                 ; E756  68
    JSR SetR7                           ; E757  20 27 c4
    PLA                                 ; E75A  68
    JMP SetR6                           ; E75B  4c 18 c4
BgChrAnimTick:  ; s59: if $62FE!=0: chr_r1_play $07F1 = BgChrAnimTable[$62FE*8 + (($08>>3)&7)] (BG 2KB R1 swaps every 8 frames); called from SubstPlayTail
    LDA area_f62FE                      ; E75E  ad fe 62
    ASL A                               ; E761  0a
    ASL A                               ; E762  0a
    ASL A                               ; E763  0a
    STA jmp_trampoline                  ; E764  85 10
    BEQ loc_31_E778                     ; E766  f0 10
    LDA tmp_08                          ; E768  a5 08
    LSR A                               ; E76A  4a
    LSR A                               ; E76B  4a
    LSR A                               ; E76C  4a
    AND #$07                            ; E76D  29 07
    ORA jmp_trampoline                  ; E76F  05 10
    TAY                                 ; E771  a8
    LDA BgChrAnimTable,Y                ; E772  b9 79 e7
    STA chr_r1_play                     ; E775  8d f1 07
loc_31_E778:
    RTS                                 ; E778  60
BgChrAnimTable:  ; s59: 4x8 R1 values; set 1 = 0A 08 06 04 x2
    .db $00,$00,$00,$00,$00,$00,$00,$00,$0A,$08,$06,$04,$0A,$08,$06,$04; 31:E779
    .db $26,$26,$24,$24,$22,$22,$20,$20,$20,$22,$24,$26,$20,$22,$24,$26; 31:E789
sub_31_E799:
    BIT obj1_flash                      ; E799  2c 10 07
    BVC loc_31_E7C3                     ; E79C  50 25
    LDY #$00                            ; E79E  a0 00
    LDA area_id                         ; E7A0  a5 6c
    CMP #$60                            ; E7A2  c9 60
    BEQ loc_31_E7B9                     ; E7A4  f0 13
    CMP #$64                            ; E7A6  c9 64
    BEQ loc_31_E7B9                     ; E7A8  f0 0f
    CMP #$68                            ; E7AA  c9 68
    BEQ loc_31_E7B9                     ; E7AC  f0 0b
    LDA obj1_flash                      ; E7AE  ad 10 07
    AND #$BF                            ; E7B1  29 bf
    STA obj1_flash                      ; E7B3  8d 10 07
    JMP loc_31_E7C3                     ; E7B6  4c c3 e7
loc_31_E7B9:
    LDA #$0C                            ; E7B9  a9 0c
    STA obj_active_s1E                  ; E7BB  8d be 04
    LDX #$1E                            ; E7BE  a2 1e
    JMP TempToObjPos                    ; E7C0  4c 97 98
loc_31_E7C3:
    RTS                                 ; E7C3  60
AreaFollowerHook:  ; area $1C: F053 & !F045 -> set F045 + slot 14 follower $0C; else clr F053
    LDA area_id                         ; E7C4  a5 6c
    CMP #$CE                            ; E7C6  c9 ce
    BEQ loc_31_E822                     ; E7C8  f0 58
    CMP #$1C                            ; E7CA  c9 1c
    BNE loc_31_E81A                     ; E7CC  d0 4c
    LDA story_flags_F040                ; E7CE  ad 88 64
    ASL A                               ; E7D1  0a
    ASL A                               ; E7D2  0a
    BMI loc_31_E81A                     ; E7D3  30 45
    LDA story_flags_F050                ; E7D5  ad 8a 64
    AND #$08                            ; E7D8  29 08
    BEQ loc_31_E822                     ; E7DA  f0 46
    LDA story_flags_F040                ; E7DC  ad 88 64
    ORA #$20                            ; E7DF  09 20
    STA story_flags_F040                ; E7E1  8d 88 64
    LDX #$0E                            ; E7E4  a2 0e
    LDA #$32                            ; E7E6  a9 32
    STA obj_active,X                    ; E7E8  9d a0 04
    LDA #$00                            ; E7EB  a9 00
    STA obj_coll,X                      ; E7ED  9d a0 03
    STA obj_timer2,X                    ; E7F0  9d 20 06
    STA obj_act_timer,X                 ; E7F3  9d 40 06
    LDA obj_flags,X                     ; E7F6  bd 80 03
    AND #$EF                            ; E7F9  29 ef
    STA obj_flags,X                     ; E7FB  9d 80 03
    LDA #$0C                            ; E7FE  a9 0c
    STA obj_param1,X                    ; E800  9d 80 06
    LDA #$01                            ; E803  a9 01
    STA obj_anim,X                      ; E805  9d e0 04
    LDA #$FF                            ; E808  a9 ff
    STA obj_phase,X                     ; E80A  9d 00 06
    LDA #$03                            ; E80D  a9 03
    STA obj_flags2,X                    ; E80F  9d 40 03
    LDA #$FF                            ; E812  a9 ff
    STA obj_phase,X                     ; E814  9d 00 06
    JMP TempToObjPos                    ; E817  4c 97 98
loc_31_E81A:
    LDA story_flags_F050                ; E81A  ad 8a 64
    AND #$F7                            ; E81D  29 f7
    STA story_flags_F050                ; E81F  8d 8a 64
loc_31_E822:
    RTS                                 ; E822  60
sub_31_E823:
    LDA area_id                         ; E823  a5 6c
    CMP #$8C                            ; E825  c9 8c
    BNE loc_31_E844                     ; E827  d0 1b
    LDA $6484                           ; E829  ad 84 64
    AND #$80                            ; E82C  29 80
    BEQ loc_31_E844                     ; E82E  f0 14
    LDA #$FF                            ; E830  a9 ff
    STA chr_r4_play                     ; E832  8d f4 07
    LDA #$51                            ; E835  a9 51
    STA chr_r5_play                     ; E837  8d f5 07
    LDA #$06                            ; E83A  a9 06
    STA pal_id_spr2                     ; E83C  8d e6 07
    LDA #$0A                            ; E83F  a9 0a
    STA pal_id_spr3                     ; E841  8d e7 07
loc_31_E844:
    RTS                                 ; E844  60
sub_31_E845:
    JSR WaitPpuQueue                    ; E845  20 76 c6
    LDA #$1A                            ; E848  a9 1a
    JSR SetR6                           ; E84A  20 18 c4
    LDA pal_id_bg3                      ; E84D  ad e3 07
    PHA                                 ; E850  48
    LDA #$AD                            ; E851  a9 ad
    STA pal_id_bg3                      ; E853  8d e3 07
    JSR PaletteBuild                    ; E856  20 0e 8c
    LDX #$00                            ; E859  a2 00
loc_31_E85B:
    LDA #$04                            ; E85B  a9 04
    STA tmp_11                          ; E85D  85 11
    LDY #$00                            ; E85F  a0 00
loc_31_E861:
    LDA #$04                            ; E861  a9 04
    STA jmp_trampoline                  ; E863  85 10
loc_31_E865:
    LDA attr_shadow,Y                   ; E865  b9 00 61
    ORA $E8B6,X                         ; E868  1d b6 e8
    STA attr_shadow,Y                   ; E86B  99 00 61
    LDA $6101,Y                         ; E86E  b9 01 61
    ORA $E8B7,X                         ; E871  1d b7 e8
    STA $6101,Y                         ; E874  99 01 61
    LDA $6108,Y                         ; E877  b9 08 61
    ORA $E8B8,X                         ; E87A  1d b8 e8
    STA $6108,Y                         ; E87D  99 08 61
    LDA $6109,Y                         ; E880  b9 09 61
    ORA $E8B9,X                         ; E883  1d b9 e8
    STA $6109,Y                         ; E886  99 09 61
    INY                                 ; E889  c8
    INY                                 ; E88A  c8
    DEC jmp_trampoline                  ; E88B  c6 10
    BNE loc_31_E865                     ; E88D  d0 d6
    TYA                                 ; E88F  98
    CLC                                 ; E890  18
    ADC #$08                            ; E891  69 08
    TAY                                 ; E893  a8
    DEC tmp_11                          ; E894  c6 11
    BNE loc_31_E861                     ; E896  d0 c9
    STX jmp_trampoline                  ; E898  86 10
    JSR sub_30_C739                     ; E89A  20 39 c7
    LDX jmp_trampoline                  ; E89D  a6 10
    LDY #$02                            ; E89F  a0 02
    JSR sub_30_C176                     ; E8A1  20 76 c1
    INX                                 ; E8A4  e8
    INX                                 ; E8A5  e8
    INX                                 ; E8A6  e8
    INX                                 ; E8A7  e8
    CPX #$40                            ; E8A8  e0 40
    BNE loc_31_E85B                     ; E8AA  d0 af
    LDA #$02                            ; E8AC  a9 02
    JSR Bank26_FadeA                    ; E8AE  20 3d 8c
    PLA                                 ; E8B1  68
    STA pal_id_bg3                      ; E8B2  8d e3 07
    RTS                                 ; E8B5  60
    .db $03,$00,$00,$00,$33,$00,$00,$00,$33,$00,$03,$00,$33,$00,$33,$00; 31:E8B6
    .db $33,$00,$F3,$00,$33,$00,$F3,$30,$33,$00,$F3,$F0,$33,$00,$F3,$FC; 31:E8C6
    .db $33,$C0,$F3,$FC,$33,$CC,$F3,$FC,$33,$CF,$F3,$FC,$3F,$CF,$F3,$FC; 31:E8D6
    .db $FF,$CF,$F3,$FC,$FF,$CF,$FF,$FC,$FF,$CF,$FF,$FF,$FF,$FF,$FF,$FF; 31:E8E6
CameraTick:  ; s46 renamed (was a 2nd CameraUpdate): $07D7 1-7F -> DEC + skip follow/ExitLookup; else follow + ExitLookup
    LDA boss_cam_lock                   ; E8F6  ad d7 07
    BEQ loc_31_E903                     ; E8F9  f0 08
    BMI loc_31_E903                     ; E8FB  30 06
    DEC boss_cam_lock                   ; E8FD  ce d7 07
    JMP loc_31_EADF                     ; E900  4c df ea
loc_31_E903:
    JSR CameraUpdate                    ; E903  20 0a e9
    JSR ExitLookup                      ; E906  20 09 eb
    RTS                                 ; E909  60
CameraUpdate:  ; $3C-$3F -> $34-$37; 26:9824; CameraClampX; render BG column/row when the 8px cell changes (s39)
    JSR CameraFollow                    ; E90A  20 ad e9
    LDA #$00                            ; E90D  a9 00
    STA tmp_2E                          ; E90F  85 2e
    STA tmp_2F                          ; E911  85 2f
    LDA zp_3C                           ; E913  a5 3c
    STA pos_x_lo                        ; E915  85 34
    LDA zp_3D                           ; E917  a5 3d
    STA pos_x_hi                        ; E919  85 35
    LDA zp_3E                           ; E91B  a5 3e
    STA pos_y_lo                        ; E91D  85 36
    LDA zp_3F                           ; E91F  a5 3f
    STA pos_y_hi                        ; E921  85 37
    LDA #$1A                            ; E923  a9 1a
    JSR SetR6                           ; E925  20 18 c4
    JSR TempAddOffset                   ; E928  20 24 98
    JSR CameraClampX                    ; E92B  20 72 ea
    LDX #$03                            ; E92E  a2 03
loc_31_E930:
    LDA pos_x_lo,X                      ; E930  b5 34
    STA tmp_28,X                        ; E932  95 28
    DEX                                 ; E934  ca
    BPL loc_31_E930                     ; E935  10 f9
    LDA zp_3D                           ; E937  a5 3d
    STA pos_x_hi                        ; E939  85 35
    LDA zp_3C                           ; E93B  a5 3c
    STA pos_x_lo                        ; E93D  85 34
    EOR tmp_28                          ; E93F  45 28
    AND #$F8                            ; E941  29 f8
    BEQ loc_31_E948                     ; E943  f0 03
    JSR BgColRender                     ; E945  20 87 ec
loc_31_E948:
    LDA tmp_28                          ; E948  a5 28
    STA pos_x_lo                        ; E94A  85 34
    LDA tmp_29                          ; E94C  a5 29
    STA pos_x_hi                        ; E94E  85 35
    LDA zp_3F                           ; E950  a5 3f
    STA pos_y_hi                        ; E952  85 37
    LDA zp_3E                           ; E954  a5 3e
    STA pos_y_lo                        ; E956  85 36
    EOR tmp_2A                          ; E958  45 2a
    AND #$F8                            ; E95A  29 f8
    BEQ loc_31_E961                     ; E95C  f0 03
    JSR BgRowRender                     ; E95E  20 91 eb
loc_31_E961:
    LDA tmp_2F                          ; E961  a5 2f
    BEQ loc_31_E968                     ; E963  f0 03
    JSR sub_30_C739                     ; E965  20 39 c7
loc_31_E968:
    LDA area_w                          ; E968  ad fc 62
    BNE loc_31_E971                     ; E96B  d0 04
    STA cam_ofs_x                       ; E96D  85 3a
    BEQ loc_31_E988                     ; E96F  f0 17
loc_31_E971:
    LDY cam_ofs_x                       ; E971  a4 3a
    LDA tmp_30                          ; E973  a5 30
    BEQ loc_31_E979                     ; E975  f0 02
    BPL loc_31_E981                     ; E977  10 08
loc_31_E979:
    CPY #$01                            ; E979  c0 01
    BEQ loc_31_E986                     ; E97B  f0 09
    INY                                 ; E97D  c8
    JMP loc_31_E986                     ; E97E  4c 86 e9
loc_31_E981:
    CPY #$F8                            ; E981  c0 f8
    BEQ loc_31_E986                     ; E983  f0 01
    DEY                                 ; E985  88
loc_31_E986:
    STY cam_ofs_x                       ; E986  84 3a
loc_31_E988:
    LDX #$03                            ; E988  a2 03
loc_31_E98A:
    LDA tmp_28,X                        ; E98A  b5 28
    STA zp_3C,X                         ; E98C  95 3c
    STA pos_x_lo,X                      ; E98E  95 34
    DEX                                 ; E990  ca
    BPL loc_31_E98A                     ; E991  10 f7
loc_31_E993:
    LDA cam_ofs_x                       ; E993  a5 3a
    STA tmp_30                          ; E995  85 30
    LDA cam_ofs_y                       ; E997  a5 3b
    STA tmp_31                          ; E999  85 31
    LDA #$1A                            ; E99B  a9 1a
    JSR SetR6                           ; E99D  20 18 c4
    JSR TempAddOffset                   ; E9A0  20 24 98
    LDX #$03                            ; E9A3  a2 03
loc_31_E9A5:
    LDA pos_x_lo,X                      ; E9A5  b5 34
    STA scroll_shadow0,X                ; E9A7  95 02
    DEX                                 ; E9A9  ca
    BPL loc_31_E9A5                     ; E9AA  10 f9
    RTS                                 ; E9AC  60
CameraFollow:  ; s46: camera step $30/$31: Y dead zone $68-$77; X dead zone $68-$9F while L/R held or knockback; else 1 px re-centre; max 3 px/frame
    LDA #$00                            ; E9AD  a9 00
    STA tmp_30                          ; E9AF  85 30
    STA tmp_31                          ; E9B1  85 31
    STA ram_07D5                        ; E9B3  8d d5 07
    BIT boss_cam_lock                   ; E9B6  2c d7 07
    BPL loc_31_E9BE                     ; E9B9  10 03
    JMP loc_31_EA41                     ; E9BB  4c 41 ea
loc_31_E9BE:
    LDA obj_y_lo                        ; E9BE  a5 b0
    SEC                                 ; E9C0  38
    SBC zp_3E                           ; E9C1  e5 3e
    BCS loc_31_E9C7                     ; E9C3  b0 02
    SBC #$0F                            ; E9C5  e9 0f
loc_31_E9C7:
    STA jmp_trampoline                  ; E9C7  85 10
    SEC                                 ; E9C9  38
    SBC #$68                            ; E9CA  e9 68
    BCC loc_31_E9D5                     ; E9CC  90 07
    LDA jmp_trampoline                  ; E9CE  a5 10
    SEC                                 ; E9D0  38
    SBC #$78                            ; E9D1  e9 78
    BCC loc_31_E9DA                     ; E9D3  90 05
loc_31_E9D5:
    JSR CamStepClamp                    ; E9D5  20 2f ea
    STA tmp_31                          ; E9D8  85 31
loc_31_E9DA:
    LDA obj_flags2                      ; E9DA  ad 40 03
    BMI loc_31_E9E5                     ; E9DD  30 06
    LDA pad_prev                        ; E9DF  a5 43
    AND #$03                            ; E9E1  29 03
    BEQ loc_31_E9FE                     ; E9E3  f0 19
loc_31_E9E5:
    LDA obj_x_lo                        ; E9E5  a5 70
    SEC                                 ; E9E7  38
    SBC zp_3C                           ; E9E8  e5 3c
    STA jmp_trampoline                  ; E9EA  85 10
    SEC                                 ; E9EC  38
    SBC #$68                            ; E9ED  e9 68
    BCC loc_31_E9F8                     ; E9EF  90 07
    LDA jmp_trampoline                  ; E9F1  a5 10
    SEC                                 ; E9F3  38
    SBC #$A0                            ; E9F4  e9 a0
    BCC loc_31_E9FD                     ; E9F6  90 05
loc_31_E9F8:
    JSR CamStepClamp                    ; E9F8  20 2f ea
    STA tmp_30                          ; E9FB  85 30
loc_31_E9FD:
    RTS                                 ; E9FD  60
loc_31_E9FE:
    LDA obj_screen_x                    ; E9FE  ad c0 05
    SEC                                 ; EA01  38
    SBC #$88                            ; EA02  e9 88
    ASL A                               ; EA04  0a
    LDA obj_x_lo                        ; EA05  a5 70
    AND #$F0                            ; EA07  29 f0
    BCS loc_31_EA0E                     ; EA09  b0 03
    CLC                                 ; EA0B  18
    ADC #$08                            ; EA0C  69 08
loc_31_EA0E:
    SEC                                 ; EA0E  38
    SBC zp_3C                           ; EA0F  e5 3c
    STA jmp_trampoline                  ; EA11  85 10
    SEC                                 ; EA13  38
    SBC #$68                            ; EA14  e9 68
    BCC loc_31_EA1F                     ; EA16  90 07
    LDA jmp_trampoline                  ; EA18  a5 10
    SEC                                 ; EA1A  38
    SBC #$98                            ; EA1B  e9 98
    BCC loc_31_EA2E                     ; EA1D  90 0f
loc_31_EA1F:
    BEQ loc_31_EA2E                     ; EA1F  f0 0d
    LDY #$00                            ; EA21  a0 00
    STY tmp_30                          ; EA23  84 30
    TAY                                 ; EA25  a8
    BPL loc_31_EA2C                     ; EA26  10 04
    DEC tmp_30                          ; EA28  c6 30
    DEC tmp_30                          ; EA2A  c6 30
loc_31_EA2C:
    INC tmp_30                          ; EA2C  e6 30
loc_31_EA2E:
    RTS                                 ; EA2E  60
CamStepClamp:  ; s46: clamp signed step to -3..+3
    BEQ loc_31_EA40                     ; EA2F  f0 0f
    BPL loc_31_EA3A                     ; EA31  10 07
    CMP #$FE                            ; EA33  c9 fe
    BCS loc_31_EA40                     ; EA35  b0 09
    LDA #$FD                            ; EA37  a9 fd
    RTS                                 ; EA39  60
loc_31_EA3A:
    CMP #$03                            ; EA3A  c9 03
    BCC loc_31_EA40                     ; EA3C  90 02
    LDA #$03                            ; EA3E  a9 03
loc_31_EA40:
    RTS                                 ; EA40  60
loc_31_EA41:
    LDA tmp_08                          ; EA41  a5 08
    LSR A                               ; EA43  4a
    BCC loc_31_EA71                     ; EA44  90 2b
    LDX #$00                            ; EA46  a2 00
    LDY #$00                            ; EA48  a0 00
    JSR sub_31_EA5F                     ; EA4A  20 5f ea
    INX                                 ; EA4D  e8
    LDY #$02                            ; EA4E  a0 02
    JSR sub_31_EA5F                     ; EA50  20 5f ea
    LDA zp_3C                           ; EA53  a5 3c
    ORA zp_3E                           ; EA55  05 3e
    BNE loc_31_EA71                     ; EA57  d0 18
    LDA #$01                            ; EA59  a9 01
    STA ram_07D5                        ; EA5B  8d d5 07
    RTS                                 ; EA5E  60
sub_31_EA5F:
    LDA a:zp_3C,Y                       ; EA5F  b9 3c 00
    BEQ loc_31_EA71                     ; EA62  f0 0d
    BPL loc_31_EA6A                     ; EA64  10 04
    INC tmp_30,X                        ; EA66  f6 30
    INC tmp_30,X                        ; EA68  f6 30
loc_31_EA6A:
    DEC tmp_30,X                        ; EA6A  d6 30
    LDA #$00                            ; EA6C  a9 00
    STA ram_07D5                        ; EA6E  8d d5 07
loc_31_EA71:
    RTS                                 ; EA71  60
CameraClampX:  ; clamp camera candidate $34/$35 (pos_x) to 0..$62FC screens
    LDY #$00                            ; EA72  a0 00
    LDA area_w                          ; EA74  ad fc 62
    BNE loc_31_EA7F                     ; EA77  d0 06
    STA pos_x_lo                        ; EA79  85 34
    STA pos_x_hi                        ; EA7B  85 35
    BEQ loc_31_EAA1                     ; EA7D  f0 22
loc_31_EA7F:
    LDA pos_x_hi                        ; EA7F  a5 35
    BPL loc_31_EA91                     ; EA81  10 0e
    LDA pos_x_lo                        ; EA83  a5 34
    CMP #$F8                            ; EA85  c9 f8
    BCS loc_31_EAA1                     ; EA87  b0 18
    STY tmp_30                          ; EA89  84 30
    LDA #$F8                            ; EA8B  a9 f8
    STA pos_x_lo                        ; EA8D  85 34
    BNE loc_31_EAA1                     ; EA8F  d0 10
loc_31_EA91:
    CMP area_w                          ; EA91  cd fc 62
    BCC loc_31_EAA1                     ; EA94  90 0b
    LDA #$00                            ; EA96  a9 00
    STA pos_x_lo                        ; EA98  85 34
    LDA area_w                          ; EA9A  ad fc 62
    STA pos_x_hi                        ; EA9D  85 35
    STY tmp_30                          ; EA9F  84 30
loc_31_EAA1:
    LDA area_h                          ; EAA1  ad fd 62
    BNE loc_31_EAB9                     ; EAA4  d0 13
    STA pos_y_hi                        ; EAA6  85 37
    STA pos_y_lo                        ; EAA8  85 36
    LDA #$FC                            ; EAAA  a9 fc
    STA cam_ofs_y                       ; EAAC  85 3b
    LDA area_id                         ; EAAE  a5 6c
    CMP #$5F                            ; EAB0  c9 5f
    BNE loc_31_EAB8                     ; EAB2  d0 04
    LDA #$00                            ; EAB4  a9 00
    STA cam_ofs_y                       ; EAB6  85 3b
loc_31_EAB8:
    RTS                                 ; EAB8  60
loc_31_EAB9:
    LDA #$08                            ; EAB9  a9 08
    STA cam_ofs_y                       ; EABB  85 3b
    LDA pos_y_hi                        ; EABD  a5 37
    BPL loc_31_EAC8                     ; EABF  10 07
    STY pos_y_hi                        ; EAC1  84 37
    STY pos_y_lo                        ; EAC3  84 36
    STY tmp_31                          ; EAC5  84 31
    RTS                                 ; EAC7  60
loc_31_EAC8:
    CMP area_h                          ; EAC8  cd fd 62
    BCC loc_31_EADE                     ; EACB  90 11
    LDA pos_y_lo                        ; EACD  a5 36
    CMP #$28                            ; EACF  c9 28
    BCC loc_31_EADE                     ; EAD1  90 0b
    LDA area_h                          ; EAD3  ad fd 62
    STA pos_y_hi                        ; EAD6  85 37
    LDA #$28                            ; EAD8  a9 28
    STA pos_y_lo                        ; EADA  85 36
    STY tmp_31                          ; EADC  84 31
loc_31_EADE:
    RTS                                 ; EADE  60
loc_31_EADF:
    LDA ram_07D6                        ; EADF  ad d6 07
    BEQ loc_31_EB04                     ; EAE2  f0 20
    DEC ram_07D6                        ; EAE4  ce d6 07
    BNE loc_31_EAEE                     ; EAE7  d0 05
    LDA #$00                            ; EAE9  a9 00
    STA cam_ofs_x                       ; EAEB  85 3a
    RTS                                 ; EAED  60
loc_31_EAEE:
    LDA tmp_08                          ; EAEE  a5 08
    AND #$03                            ; EAF0  29 03
    TAX                                 ; EAF2  aa
    LDA $EB05,X                         ; EAF3  bd 05 eb
    STA cam_ofs_x                       ; EAF6  85 3a
    LDX #$03                            ; EAF8  a2 03
loc_31_EAFA:
    LDA zp_3C,X                         ; EAFA  b5 3c
    STA pos_x_lo,X                      ; EAFC  95 34
    DEX                                 ; EAFE  ca
    BPL loc_31_EAFA                     ; EAFF  10 f9
    JMP loc_31_E993                     ; EB01  4c 93 e9
loc_31_EB04:
    RTS                                 ; EB04  60
    .db $00,$FC,$00,$04                 ; 31:EB05
ExitLookup:  ; player 16px cell vs exit list (xc yc area entry)*;FF; sub_state 8 -> load area
    LDA obj_x_lo                        ; EB09  a5 70
    LSR A                               ; EB0B  4a
    LSR A                               ; EB0C  4a
    LSR A                               ; EB0D  4a
    LSR A                               ; EB0E  4a
    STA tmp_12                          ; EB0F  85 12
    LDA obj_x_hi                        ; EB11  a5 90
    ASL A                               ; EB13  0a
    ASL A                               ; EB14  0a
    ASL A                               ; EB15  0a
    ASL A                               ; EB16  0a
    ORA tmp_12                          ; EB17  05 12
    STA tmp_12                          ; EB19  85 12
    LDA obj_y_lo                        ; EB1B  a5 b0
    LSR A                               ; EB1D  4a
    LSR A                               ; EB1E  4a
    LSR A                               ; EB1F  4a
    LSR A                               ; EB20  4a
    STA tmp_13                          ; EB21  85 13
    LDA obj_y_hi                        ; EB23  a5 d0
    ASL A                               ; EB25  0a
    ASL A                               ; EB26  0a
    ASL A                               ; EB27  0a
    ASL A                               ; EB28  0a
    ORA tmp_13                          ; EB29  05 13
    STA tmp_13                          ; EB2B  85 13
    LDA #$03                            ; EB2D  a9 03
    JSR AreaSubTable                    ; EB2F  20 a5 c7
    LDY #$00                            ; EB32  a0 00
loc_31_EB34:
    LDA (jmp_trampoline),Y              ; EB34  b1 10
    INY                                 ; EB36  c8
    STA tmp_14                          ; EB37  85 14
    LDA (jmp_trampoline),Y              ; EB39  b1 10
    INY                                 ; EB3B  c8
    STA tmp_15                          ; EB3C  85 15
    LDA tmp_14                          ; EB3E  a5 14
    CMP #$FF                            ; EB40  c9 ff
    BEQ loc_31_EB54                     ; EB42  f0 10
    LDA tmp_15                          ; EB44  a5 15
    CMP tmp_13                          ; EB46  c5 13
    BNE loc_31_EB50                     ; EB48  d0 06
    LDA tmp_14                          ; EB4A  a5 14
    CMP tmp_12                          ; EB4C  c5 12
    BEQ loc_31_EB55                     ; EB4E  f0 05
loc_31_EB50:
    INY                                 ; EB50  c8
    INY                                 ; EB51  c8
    BNE loc_31_EB34                     ; EB52  d0 e0
loc_31_EB54:
    RTS                                 ; EB54  60
loc_31_EB55:
    LDA sub_state                       ; EB55  a5 41
    CMP #$08                            ; EB57  c9 08
    BNE loc_31_EB6C                     ; EB59  d0 11
    LDA (jmp_trampoline),Y              ; EB5B  b1 10
    STA area_id                         ; EB5D  85 6c
    INY                                 ; EB5F  c8
    LDA (jmp_trampoline),Y              ; EB60  b1 10
    STA area_entry                      ; EB62  85 6d
    LDA #$01                            ; EB64  a9 01
    STA sub_state                       ; EB66  85 41
    PLA                                 ; EB68  68
    PLA                                 ; EB69  68
    PLA                                 ; EB6A  68
    PLA                                 ; EB6B  68
loc_31_EB6C:
    RTS                                 ; EB6C  60
PpuScrollWrite:  ; s51: PPUSCROLL <- $07D8 then $07DA; ppuctrl_shadow b0-1 = ($07DB b0)<<1 | ($07D9 b0) -> PPUCTRL (web: mainloop.js ppuScrollWrite)
    LDA scroll_x_irq                    ; EB6D  ad d8 07
    STA PPUSCROLL                       ; EB70  8d 05 20
    LDA ram_07DA                        ; EB73  ad da 07
    STA PPUSCROLL                       ; EB76  8d 05 20
    LDA $07D9                           ; EB79  ad d9 07
    LSR A                               ; EB7C  4a
    LDA ram_07DB                        ; EB7D  ad db 07
    ROL A                               ; EB80  2a
    AND #$03                            ; EB81  29 03
    STA $52                             ; EB83  85 52
    LDA ppuctrl_shadow                  ; EB85  a5 00
    AND #$FC                            ; EB87  29 fc
    ORA $52                             ; EB89  05 52
    STA ppuctrl_shadow                  ; EB8B  85 00
    STA PPUCTRL                         ; EB8D  8d 00 20
    RTS                                 ; EB90  60
BgRowRender:  ; row render entry: $C72B(A=0); INC pos_y_hi ($37) if $31>=0; -> ScreenRowRender
    LDA #$00                            ; EB91  a9 00
    JSR sub_30_C72B                     ; EB93  20 2b c7
    LDA tmp_31                          ; EB96  a5 31
    BMI loc_31_EB9C                     ; EB98  30 02
    INC pos_y_hi                        ; EB9A  e6 37
loc_31_EB9C:
    JSR ScreenAtCamera                  ; EB9C  20 1f ef
    STA tmp_20                          ; EB9F  85 20
    JSR ScreenVisitedBit                ; EBA1  20 6c ec
    STA tmp_1E                          ; EBA4  85 1e
    TYA                                 ; EBA6  98
    AND #$F8                            ; EBA7  29 f8
    STA jmp_trampoline                  ; EBA9  85 10
    INY                                 ; EBAB  c8
    TYA                                 ; EBAC  98
    AND #$07                            ; EBAD  29 07
    ORA jmp_trampoline                  ; EBAF  05 10
    TAY                                 ; EBB1  a8
    LDA screen_grid,Y                   ; EBB2  b9 00 63
    STA tmp_21                          ; EBB5  85 21
    JSR ScreenVisitedBit                ; EBB7  20 6c ec
    STA tmp_1F                          ; EBBA  85 1f
    LDA tmp_20                          ; EBBC  a5 20
    JSR ScreenPagePtr                   ; EBBE  20 36 ef
ScreenRowRender:  ; reads metatile ids from screen page(s) into $6000 buffer then expands + queues to PPU
    LDA pos_y_lo                        ; EBC1  a5 36
    AND #$F0                            ; EBC3  29 f0
    STA jmp_trampoline                  ; EBC5  85 10
    LDA pos_x_lo                        ; EBC7  a5 34
    LSR A                               ; EBC9  4a
    LSR A                               ; EBCA  4a
    LSR A                               ; EBCB  4a
    LSR A                               ; EBCC  4a
    TAY                                 ; EBCD  a8
    LDA pos_x_lo                        ; EBCE  a5 34
    AND #$F0                            ; EBD0  29 f0
    LSR A                               ; EBD2  4a
    LSR A                               ; EBD3  4a
    LSR A                               ; EBD4  4a
    TAX                                 ; EBD5  aa
    LDA pos_x_lo                        ; EBD6  a5 34
    AND #$08                            ; EBD8  29 08
    LSR A                               ; EBDA  4a
    LSR A                               ; EBDB  4a
    LSR A                               ; EBDC  4a
    CLC                                 ; EBDD  18
    ADC #$10                            ; EBDE  69 10
    STA tmp_13                          ; EBE0  85 13
    JSR ScreenSubstPtr                  ; EBE2  20 89 ed
    LDA area_f62FF                      ; EBE5  ad ff 62
    BPL loc_31_EBEF                     ; EBE8  10 05
    LDA #$0A                            ; EBEA  a9 0a
    JSR SetR6                           ; EBEC  20 18 c4
loc_31_EBEF:
    LDA (jmp_trampoline),Y              ; EBEF  b1 10
    JSR ScreenTileSubst                 ; EBF1  20 99 ed
    STA wram_buf,X                      ; EBF4  9d 00 60
    INY                                 ; EBF7  c8
    INX                                 ; EBF8  e8
    INX                                 ; EBF9  e8
    DEC tmp_13                          ; EBFA  c6 13
    BEQ loc_31_EC11                     ; EBFC  f0 13
    CPY #$10                            ; EBFE  c0 10
    BNE loc_31_EBEF                     ; EC00  d0 ed
    LDA tmp_21                          ; EC02  a5 21
    JSR ScreenPagePtr                   ; EC04  20 36 ef
    LDA tmp_1F                          ; EC07  a5 1f
    STA tmp_1E                          ; EC09  85 1e
    LDX #$00                            ; EC0B  a2 00
    LDY #$00                            ; EC0D  a0 00
    BEQ loc_31_EBEF                     ; EC0F  f0 de
loc_31_EC11:
    JSR MetatileAttr                    ; EC11  20 e5 ed
    LDA #$04                            ; EC14  a9 04
    JSR SwitchBank16                    ; EC16  20 0e c4
    LDA pos_y_lo                        ; EC19  a5 36
    AND #$08                            ; EC1B  29 08
    LSR A                               ; EC1D  4a
    LSR A                               ; EC1E  4a
    STA tmp_12                          ; EC1F  85 12
    LDA #$01                            ; EC21  a9 01
    STA tmp_13                          ; EC23  85 13
    LDX #$00                            ; EC25  a2 00
    JSR MetatileExpand                  ; EC27  20 ba ed
    JSR CamNtAddr                       ; EC2A  20 64 ed
    JSR sub_30_C43E                     ; EC2D  20 3e c4
    LDX tmp_0B                          ; EC30  a6 0b
    LDA tmp_11                          ; EC32  a5 11
    ORA ram_0744                        ; EC34  0d 44 07
    STA ppu_queue,X                     ; EC37  9d 00 62
    LDA jmp_trampoline                  ; EC3A  a5 10
    AND #$E0                            ; EC3C  29 e0
    STA ppu_queue_1,X                   ; EC3E  9d 01 62
    LDA #$20                            ; EC41  a9 20
    STA ppu_queue_2,X                   ; EC43  9d 02 62
    LDA #$00                            ; EC46  a9 00
    STA ppu_queue_3,X                   ; EC48  9d 03 62
    TXA                                 ; EC4B  8a
    CLC                                 ; EC4C  18
    ADC #$04                            ; EC4D  69 04
    AND #$1F                            ; EC4F  29 1f
    STA tmp_0B                          ; EC51  85 0b
    LDA ppumask_shadow                  ; EC53  a5 01
    AND #$18                            ; EC55  29 18
    BEQ loc_31_EC5C                     ; EC57  f0 03
    JMP sub_30_C436                     ; EC59  4c 36 c4
loc_31_EC5C:
    LDA ppuctrl_shadow                  ; EC5C  a5 00
    PHA                                 ; EC5E  48
    AND #$7F                            ; EC5F  29 7f
    STA ppuctrl_shadow                  ; EC61  85 00
    JSR PpuQueueFlush                   ; EC63  20 7d c6
    PLA                                 ; EC66  68
    STA ppuctrl_shadow                  ; EC67  85 00
    JMP sub_30_C436                     ; EC69  4c 36 c4
ScreenVisitedBit:  ; A = visited bit of screen index Y in $62F0 bitmap
    TYA                                 ; EC6C  98
    LSR A                               ; EC6D  4a
    LSR A                               ; EC6E  4a
    LSR A                               ; EC6F  4a
    TAX                                 ; EC70  aa
    TYA                                 ; EC71  98
    AND #$07                            ; EC72  29 07
    STA jmp_trampoline                  ; EC74  85 10
    LDA visited_map,X                   ; EC76  bd f0 62
    LDX jmp_trampoline                  ; EC79  a6 10
    AND $EC7F,X                         ; EC7B  3d 7f ec
    RTS                                 ; EC7E  60
    .db $01,$02,$04,$08,$10,$20,$40,$80 ; 31:EC7F
BgColRender:  ; column render entry: $C72B(A=$20); INC pos_x_hi ($35) if $30>=0; no $62FF bank-10 check
    LDA #$20                            ; EC87  a9 20
    JSR sub_30_C72B                     ; EC89  20 2b c7
    LDA #$00                            ; EC8C  a9 00
    STA tmp_18                          ; EC8E  85 18
    LDA #$01                            ; EC90  a9 01
    STA tmp_19                          ; EC92  85 19
    LDA tmp_30                          ; EC94  a5 30
    BMI loc_31_EC9A                     ; EC96  30 02
    INC pos_x_hi                        ; EC98  e6 35
loc_31_EC9A:
    JSR ScreenAtCamera                  ; EC9A  20 1f ef
    STA tmp_20                          ; EC9D  85 20
    JSR ScreenVisitedBit                ; EC9F  20 6c ec
    STA tmp_1E                          ; ECA2  85 1e
    TYA                                 ; ECA4  98
    CLC                                 ; ECA5  18
    ADC #$08                            ; ECA6  69 08
    BPL loc_31_ECAC                     ; ECA8  10 02
    AND #$07                            ; ECAA  29 07
loc_31_ECAC:
    CMP #$60                            ; ECAC  c9 60
    BCC loc_31_ECB2                     ; ECAE  90 02
    SBC #$5F                            ; ECB0  e9 5f
loc_31_ECB2:
    TAY                                 ; ECB2  a8
    LDA screen_grid,Y                   ; ECB3  b9 00 63
    STA tmp_21                          ; ECB6  85 21
    JSR ScreenVisitedBit                ; ECB8  20 6c ec
    STA tmp_1F                          ; ECBB  85 1f
    LDA tmp_20                          ; ECBD  a5 20
    JSR ScreenPagePtr                   ; ECBF  20 36 ef
    LDA pos_x_lo                        ; ECC2  a5 34
    AND #$F0                            ; ECC4  29 f0
    LSR A                               ; ECC6  4a
    LSR A                               ; ECC7  4a
    LSR A                               ; ECC8  4a
    LSR A                               ; ECC9  4a
    STA jmp_trampoline                  ; ECCA  85 10
    LDA pos_y_lo                        ; ECCC  a5 36
    AND #$F0                            ; ECCE  29 f0
    ORA jmp_trampoline                  ; ECD0  05 10
    TAY                                 ; ECD2  a8
    LDA #$00                            ; ECD3  a9 00
    STA jmp_trampoline                  ; ECD5  85 10
    LDA pos_y_lo                        ; ECD7  a5 36
    AND #$F0                            ; ECD9  29 f0
    LSR A                               ; ECDB  4a
    LSR A                               ; ECDC  4a
    LSR A                               ; ECDD  4a
    CLC                                 ; ECDE  18
    ADC #$20                            ; ECDF  69 20
    TAX                                 ; ECE1  aa
    LDA pos_y_lo                        ; ECE2  a5 36
    AND #$08                            ; ECE4  29 08
    LSR A                               ; ECE6  4a
    LSR A                               ; ECE7  4a
    LSR A                               ; ECE8  4a
    CLC                                 ; ECE9  18
    ADC #$0F                            ; ECEA  69 0f
    STA tmp_13                          ; ECEC  85 13
    LDA #$00                            ; ECEE  a9 00
    STA tmp_14                          ; ECF0  85 14
    JSR ScreenSubstPtr                  ; ECF2  20 89 ed
loc_31_ECF5:
    LDA (jmp_trampoline),Y              ; ECF5  b1 10
    JSR ScreenTileSubst                 ; ECF7  20 99 ed
    STA wram_buf,X                      ; ECFA  9d 00 60
    INX                                 ; ECFD  e8
    INX                                 ; ECFE  e8
    TYA                                 ; ECFF  98
    CLC                                 ; ED00  18
    ADC #$10                            ; ED01  69 10
    TAY                                 ; ED03  a8
    DEC tmp_13                          ; ED04  c6 13
    BEQ loc_31_ED1D                     ; ED06  f0 15
    CMP #$F0                            ; ED08  c9 f0
    BCC loc_31_ECF5                     ; ED0A  90 e9
    LDA tmp_1F                          ; ED0C  a5 1f
    STA tmp_1E                          ; ED0E  85 1e
    LDA tmp_21                          ; ED10  a5 21
    JSR ScreenPagePtr                   ; ED12  20 36 ef
    TYA                                 ; ED15  98
    AND #$0F                            ; ED16  29 0f
    TAY                                 ; ED18  a8
    LDX #$20                            ; ED19  a2 20
    BNE loc_31_ECF5                     ; ED1B  d0 d8
loc_31_ED1D:
    JSR MetatileAttrCol                 ; ED1D  20 31 ee
    LDA #$04                            ; ED20  a9 04
    JSR SwitchBank16                    ; ED22  20 0e c4
    LDA pos_x_lo                        ; ED25  a5 34
    AND #$08                            ; ED27  29 08
    LSR A                               ; ED29  4a
    LSR A                               ; ED2A  4a
    LSR A                               ; ED2B  4a
    STA tmp_12                          ; ED2C  85 12
    LDA #$02                            ; ED2E  a9 02
    STA tmp_13                          ; ED30  85 13
    LDX #$20                            ; ED32  a2 20
    JSR MetatileExpand                  ; ED34  20 ba ed
    JSR CamNtAddr                       ; ED37  20 64 ed
    JSR sub_30_C43E                     ; ED3A  20 3e c4
    LDX tmp_0B                          ; ED3D  a6 0b
    LDA tmp_11                          ; ED3F  a5 11
    AND #$30                            ; ED41  29 30
    ORA #$40                            ; ED43  09 40
    STA ppu_queue,X                     ; ED45  9d 00 62
    LDA jmp_trampoline                  ; ED48  a5 10
    AND #$1F                            ; ED4A  29 1f
    STA ppu_queue_1,X                   ; ED4C  9d 01 62
    LDA #$1E                            ; ED4F  a9 1e
    STA ppu_queue_2,X                   ; ED51  9d 02 62
    LDA #$20                            ; ED54  a9 20
    STA ppu_queue_3,X                   ; ED56  9d 03 62
    TXA                                 ; ED59  8a
    CLC                                 ; ED5A  18
    ADC #$04                            ; ED5B  69 04
    AND #$1F                            ; ED5D  29 1f
    STA tmp_0B                          ; ED5F  85 0b
    JMP sub_30_C436                     ; ED61  4c 36 c4
CamNtAddr:  ; $10/$11 = nametable address for camera ($34/$36)
    LDA pos_x_lo                        ; ED64  a5 34
    LSR A                               ; ED66  4a
    LSR A                               ; ED67  4a
    LSR A                               ; ED68  4a
    STA jmp_trampoline                  ; ED69  85 10
    LDA #$20                            ; ED6B  a9 20
    STA tmp_11                          ; ED6D  85 11
    LDA pos_y_lo                        ; ED6F  a5 36
    AND #$F8                            ; ED71  29 f8
    ASL A                               ; ED73  0a
    ASL A                               ; ED74  0a
    ORA jmp_trampoline                  ; ED75  05 10
    STA jmp_trampoline                  ; ED77  85 10
    LDA pos_y_lo                        ; ED79  a5 36
    LSR A                               ; ED7B  4a
    LSR A                               ; ED7C  4a
    LSR A                               ; ED7D  4a
    LSR A                               ; ED7E  4a
    LSR A                               ; ED7F  4a
    LSR A                               ; ED80  4a
    CLC                                 ; ED81  18
    ADC #$20                            ; ED82  69 20
    ORA tmp_11                          ; ED84  05 11
    STA tmp_11                          ; ED86  85 11
    RTS                                 ; ED88  60
ScreenSubstPtr:  ; $23/$24 = $BE00+($67&$3C)*8 (bank 9 visited-screen metatile substitution)
    LDA zp_67                           ; ED89  a5 67
    AND #$3C                            ; ED8B  29 3c
    ASL A                               ; ED8D  0a
    ASL A                               ; ED8E  0a
    ASL A                               ; ED8F  0a
    STA tmp_23                          ; ED90  85 23
    LDA #$BE                            ; ED92  a9 be
    ADC #$00                            ; ED94  69 00
    STA tmp_24                          ; ED96  85 24
    RTS                                 ; ED98  60
ScreenTileSubst:  ; metatile <$20 in a visited screen -> bank 9 ($23)+M replacement
    CMP #$20                            ; ED99  c9 20
    BCC loc_31_ED9E                     ; ED9B  90 01
    RTS                                 ; ED9D  60
loc_31_ED9E:
    STY tmp_25                          ; ED9E  84 25
    LDY tmp_1E                          ; EDA0  a4 1e
    BEQ loc_31_EDB7                     ; EDA2  f0 13
    TAY                                 ; EDA4  a8
    LDA r7_shadow                       ; EDA5  a5 6f
    PHA                                 ; EDA7  48
    LDA #$09                            ; EDA8  a9 09
    JSR SetR7                           ; EDAA  20 27 c4
    LDA (tmp_23),Y                      ; EDAD  b1 23
    STA tmp_26                          ; EDAF  85 26
    PLA                                 ; EDB1  68
    JSR SetR7                           ; EDB2  20 27 c4
    LDA tmp_26                          ; EDB5  a5 26
loc_31_EDB7:
    LDY tmp_25                          ; EDB7  a4 25
    RTS                                 ; EDB9  60
MetatileExpand:  ; $6000 metatile ids -> tile ids via 4 pages at $67&$FC +0..3 (banks 8/9)
    LDA zp_67                           ; EDBA  a5 67
    AND #$FC                            ; EDBC  29 fc
    ORA tmp_12                          ; EDBE  05 12
    STA tmp_14                          ; EDC0  85 14
    LDA #$10                            ; EDC2  a9 10
    STA tmp_15                          ; EDC4  85 15
loc_31_EDC6:
    LDA tmp_14                          ; EDC6  a5 14
    STA zp_67                           ; EDC8  85 67
    LDY wram_buf,X                      ; EDCA  bc 00 60
    LDA (zp_66),Y                       ; EDCD  b1 66
    STA wram_buf,X                      ; EDCF  9d 00 60
    LDA zp_67                           ; EDD2  a5 67
    CLC                                 ; EDD4  18
    ADC tmp_13                          ; EDD5  65 13
    STA zp_67                           ; EDD7  85 67
    LDA (zp_66),Y                       ; EDD9  b1 66
    STA wram_6001,X                     ; EDDB  9d 01 60
    INX                                 ; EDDE  e8
    INX                                 ; EDDF  e8
    DEC tmp_15                          ; EDE0  c6 15
    BNE loc_31_EDC6                     ; EDE2  d0 e2
    RTS                                 ; EDE4  60
MetatileAttr:  ; attribute pass; ptr table 31:EEEF[($67&$3C)>>1] in bank 9
    LDA tmp_31                          ; EDE5  a5 31
    LSR A                               ; EDE7  4a
    LSR A                               ; EDE8  4a
    LSR A                               ; EDE9  4a
    LSR A                               ; EDEA  4a
    EOR pos_y_lo                        ; EDEB  45 36
    AND #$08                            ; EDED  29 08
    ORA tmp_2F                          ; EDEF  05 2f
    STA tmp_2F                          ; EDF1  85 2f
    BNE loc_31_EDF6                     ; EDF3  d0 01
    RTS                                 ; EDF5  60
loc_31_EDF6:
    LDA #$09                            ; EDF6  a9 09
    JSR SetR7                           ; EDF8  20 27 c4
    LDA zp_67                           ; EDFB  a5 67
    AND #$3C                            ; EDFD  29 3c
    LSR A                               ; EDFF  4a
    TAY                                 ; EE00  a8
    LDA MetatileAttrPtrTable,Y          ; EE01  b9 ef ee
    STA jmp_trampoline                  ; EE04  85 10
    LDA $EEF0,Y                         ; EE06  b9 f0 ee
    STA tmp_11                          ; EE09  85 11
    LDA pos_y_lo                        ; EE0B  a5 36
    LSR A                               ; EE0D  4a
    LSR A                               ; EE0E  4a
    LSR A                               ; EE0F  4a
    LSR A                               ; EE10  4a
    TAX                                 ; EE11  aa
    LDY AttrRowOffset,X                 ; EE12  bc 07 ef
    AND #$01                            ; EE15  29 01
    TAX                                 ; EE17  aa
    LDA $EF1B,X                         ; EE18  bd 1b ef
    STA tmp_12                          ; EE1B  85 12
    LDA $EF1D,X                         ; EE1D  bd 1d ef
    STA tmp_13                          ; EE20  85 13
    ORA tmp_12                          ; EE22  05 12
    EOR #$FF                            ; EE24  49 ff
    STA tmp_14                          ; EE26  85 14
    LDX #$00                            ; EE28  a2 00
    LDA #$20                            ; EE2A  a9 20
    STA tmp_16                          ; EE2C  85 16
    JMP loc_31_EE80                     ; EE2E  4c 80 ee
MetatileAttrCol:  ; column twin of MetatileAttr (step 8 into $6100)
    LDA tmp_30                          ; EE31  a5 30
    LSR A                               ; EE33  4a
    LSR A                               ; EE34  4a
    LSR A                               ; EE35  4a
    LSR A                               ; EE36  4a
    EOR pos_x_lo                        ; EE37  45 34
    AND #$08                            ; EE39  29 08
    ORA tmp_2F                          ; EE3B  05 2f
    STA tmp_2F                          ; EE3D  85 2f
    BNE loc_31_EE42                     ; EE3F  d0 01
    RTS                                 ; EE41  60
loc_31_EE42:
    LDA #$09                            ; EE42  a9 09
    JSR SetR7                           ; EE44  20 27 c4
    LDA zp_67                           ; EE47  a5 67
    AND #$3C                            ; EE49  29 3c
    LSR A                               ; EE4B  4a
    TAY                                 ; EE4C  a8
    LDA MetatileAttrPtrTable,Y          ; EE4D  b9 ef ee
    STA jmp_trampoline                  ; EE50  85 10
    LDA $EEF0,Y                         ; EE52  b9 f0 ee
    STA tmp_11                          ; EE55  85 11
    LDA pos_x_lo                        ; EE57  a5 34
    LSR A                               ; EE59  4a
    LSR A                               ; EE5A  4a
    LSR A                               ; EE5B  4a
    LSR A                               ; EE5C  4a
    LSR A                               ; EE5D  4a
    TAY                                 ; EE5E  a8
    LDA pos_x_lo                        ; EE5F  a5 34
    AND #$10                            ; EE61  29 10
    LSR A                               ; EE63  4a
    LSR A                               ; EE64  4a
    LSR A                               ; EE65  4a
    TAX                                 ; EE66  aa
    LDA $EF1B,X                         ; EE67  bd 1b ef
    STA tmp_12                          ; EE6A  85 12
    LDA $EF1C,X                         ; EE6C  bd 1c ef
    STA tmp_13                          ; EE6F  85 13
    ORA tmp_12                          ; EE71  05 12
    EOR #$FF                            ; EE73  49 ff
    STA tmp_14                          ; EE75  85 14
    LDX #$20                            ; EE77  a2 20
    LDA #$40                            ; EE79  a9 40
    STA tmp_16                          ; EE7B  85 16
    JMP loc_31_EEA4                     ; EE7D  4c a4 ee
loc_31_EE80:
    JSR MetatileAttrBits                ; EE80  20 cc ee
    AND tmp_12                          ; EE83  25 12
    STA tmp_15                          ; EE85  85 15
    INX                                 ; EE87  e8
    INX                                 ; EE88  e8
    JSR MetatileAttrBits                ; EE89  20 cc ee
    AND tmp_13                          ; EE8C  25 13
    ORA tmp_15                          ; EE8E  05 15
    STA tmp_15                          ; EE90  85 15
    LDA tmp_14                          ; EE92  a5 14
    AND attr_shadow,Y                   ; EE94  39 00 61
    ORA tmp_15                          ; EE97  05 15
    STA attr_shadow,Y                   ; EE99  99 00 61
    INY                                 ; EE9C  c8
    INX                                 ; EE9D  e8
    INX                                 ; EE9E  e8
    CPX tmp_16                          ; EE9F  e4 16
    BNE loc_31_EE80                     ; EEA1  d0 dd
    RTS                                 ; EEA3  60
loc_31_EEA4:
    JSR MetatileAttrBits                ; EEA4  20 cc ee
    AND tmp_12                          ; EEA7  25 12
    STA tmp_15                          ; EEA9  85 15
    INX                                 ; EEAB  e8
    INX                                 ; EEAC  e8
    JSR MetatileAttrBits                ; EEAD  20 cc ee
    AND tmp_13                          ; EEB0  25 13
    ORA tmp_15                          ; EEB2  05 15
    STA tmp_15                          ; EEB4  85 15
    LDA tmp_14                          ; EEB6  a5 14
    AND attr_shadow,Y                   ; EEB8  39 00 61
    ORA tmp_15                          ; EEBB  05 15
    STA attr_shadow,Y                   ; EEBD  99 00 61
    TYA                                 ; EEC0  98
    CLC                                 ; EEC1  18
    ADC #$08                            ; EEC2  69 08
    TAY                                 ; EEC4  a8
    INX                                 ; EEC5  e8
    INX                                 ; EEC6  e8
    CPX tmp_16                          ; EEC7  e4 16
    BNE loc_31_EEA4                     ; EEC9  d0 d9
    RTS                                 ; EECB  60
MetatileAttrBits:  ; A = attr byte for metatile $6000;X: (tbl[M>>2]>>2*(M&3))&3 -> $EF17 replicate
    STY tmp_21                          ; EECC  84 21
    LDA wram_buf,X                      ; EECE  bd 00 60
    STA tmp_22                          ; EED1  85 22
    LSR A                               ; EED3  4a
    LSR A                               ; EED4  4a
    TAY                                 ; EED5  a8
    LDA tmp_22                          ; EED6  a5 22
    AND #$03                            ; EED8  29 03
    STA tmp_20                          ; EEDA  85 20
    LDA (jmp_trampoline),Y              ; EEDC  b1 10
loc_31_EEDE:
    DEC tmp_20                          ; EEDE  c6 20
    BMI loc_31_EEE6                     ; EEE0  30 04
    LSR A                               ; EEE2  4a
    LSR A                               ; EEE3  4a
    BPL loc_31_EEDE                     ; EEE4  10 f8
loc_31_EEE6:
    AND #$03                            ; EEE6  29 03
    TAY                                 ; EEE8  a8
    LDA AttrReplicate,Y                 ; EEE9  b9 17 ef
    LDY tmp_21                          ; EEEC  a4 21
    RTS                                 ; EEEE  60
MetatileAttrPtrTable:  ; ptr16 per tileset (bank 9)
    .db $00,$B0,$40,$B0,$80,$B0,$C0,$B0,$00,$B1,$40,$B1,$80,$B1,$C0,$B1; 31:EEEF
    .db $00,$B2,$40,$B2,$80,$B2,$C0,$B2 ; 31:EEFF
AttrRowOffset:  ; 16 bytes 00 00 08 08 .. 38 38: $6100 row offset for pos_y>>4
    .db $00,$00,$08,$08,$10,$10,$18,$18,$20,$20,$28,$28,$30,$30,$38,$38; 31:EF07
AttrReplicate:  ; 00 55 AA FF: 2-bit pal into all 4 quadrants; $EF1B/$EF1D half masks 03 30 / 0C C0
    .db $00,$55,$AA,$FF,$03,$30,$0C,$C0 ; 31:EF17
ScreenAtCamera:  ; A = $6300[(pos_y_hi<<3&$78)|(pos_x_hi&7)] screen id ($34-$37 cursor)
    LDA pos_y_hi                        ; EF1F  a5 37
    ASL A                               ; EF21  0a
    ASL A                               ; EF22  0a
    ASL A                               ; EF23  0a
    AND #$78                            ; EF24  29 78
    STA jmp_trampoline                  ; EF26  85 10
    LDA pos_x_hi                        ; EF28  a5 35
    AND #$07                            ; EF2A  29 07
    ORA jmp_trampoline                  ; EF2C  05 10
    TAY                                 ; EF2E  a8
    LDA screen_grid,Y                   ; EF2F  b9 00 63
    RTS                                 ; EF32  60
ScreenPageAtCamera:  ; ScreenAtCamera then ScreenPagePtr; no direct JSR/JMP xrefs (s61)
    JSR ScreenAtCamera                  ; EF33  20 1f ef
ScreenPagePtr:  ; screen id S: SwitchBank16(S>>6) and $10/$11 = $8000+(S&$3F)*$100 (raw 16x15 metatile page)
    PHA                                 ; EF36  48
    AND #$3F                            ; EF37  29 3f
    ORA #$80                            ; EF39  09 80
    STA tmp_11                          ; EF3B  85 11
    PLA                                 ; EF3D  68
    ROL A                               ; EF3E  2a
    ROL A                               ; EF3F  2a
    ROL A                               ; EF40  2a
    AND #$03                            ; EF41  29 03
    JSR SwitchBank16                    ; EF43  20 0e c4
    LDA pos_y_lo                        ; EF46  a5 36
    AND #$F0                            ; EF48  29 f0
    STA tmp_12                          ; EF4A  85 12
    LDA pos_x_lo                        ; EF4C  a5 34
    LSR A                               ; EF4E  4a
    LSR A                               ; EF4F  4a
    LSR A                               ; EF50  4a
    LSR A                               ; EF51  4a
    ORA tmp_12                          ; EF52  05 12
    RTS                                 ; EF54  60
PlayerStatusTick:  ; s46: per frame (SubstPlay): poison/damage floor/area $1A hazard -4 HP; status 1/2; MP/HP regen items; pit fall; invuln countdown
    LDA tmp_08                          ; EF55  a5 08
    ASL A                               ; EF57  0a
    BNE loc_31_EF63                     ; EF58  d0 09
    LDA obj1_flash                      ; EF5A  ad 10 07
    AND #$0F                            ; EF5D  29 0f
    CMP #$03                            ; EF5F  c9 03
    BEQ loc_31_EF8C                     ; EF61  f0 29
loc_31_EF63:
    LDA equip_accessory                 ; EF63  ad 16 07
    CMP #$13                            ; EF66  c9 13
    BEQ loc_31_EF77                     ; EF68  f0 0d
    LDA obj_timer2                      ; EF6A  ad 20 06
    BNE loc_31_EF77                     ; EF6D  d0 08
    LDA obj_spawn_param_s01             ; EF6F  ad 61 05
    ORA player_bg_attr                  ; EF72  0d 41 06
    BMI loc_31_EF8C                     ; EF75  30 15
loc_31_EF77:
    LDA area_id                         ; EF77  a5 6c
    CMP #$1A                            ; EF79  c9 1a
    BNE loc_31_EFBC                     ; EF7B  d0 3f
    LDA equip_accessory                 ; EF7D  ad 16 07
    CMP #$0D                            ; EF80  c9 0d
    BEQ loc_31_EFBC                     ; EF82  f0 38
    LDA tmp_08                          ; EF84  a5 08
    AND #$07                            ; EF86  29 07
    BNE loc_31_EFBC                     ; EF88  d0 32
    BEQ loc_31_EF92                     ; EF8A  f0 06
loc_31_EF8C:
    LDA tmp_08                          ; EF8C  a5 08
    AND #$1F                            ; EF8E  29 1f
    BNE loc_31_EFBC                     ; EF90  d0 2a
loc_31_EF92:
    LDA player_hp                       ; EF92  ad c1 03
    SEC                                 ; EF95  38
    SBC #$04                            ; EF96  e9 04
    BCS loc_31_EF9C                     ; EF98  b0 02
    LDA #$00                            ; EF9A  a9 00
loc_31_EF9C:
    STA player_hp                       ; EF9C  8d c1 03
    LDA #$36                            ; EF9F  a9 36
    JSR PlaySound                       ; EFA1  20 25 c1
    LDX #$03                            ; EFA4  a2 03
loc_31_EFA6:
    LDA #$30                            ; EFA6  a9 30
    STA wram_6150,X                     ; EFA8  9d 50 61
    DEX                                 ; EFAB  ca
    BNE loc_31_EFA6                     ; EFAC  d0 f8
    JSR WaitFrame                       ; EFAE  20 69 c1
    LDA #$1A                            ; EFB1  a9 1a
    JSR SetR6                           ; EFB3  20 18 c4
    JSR HudHpBar                        ; EFB6  20 c0 8c
    JSR PaletteBuild                    ; EFB9  20 0e 8c
loc_31_EFBC:
    LDA #$1A                            ; EFBC  a9 1a
    JSR SetR6                           ; EFBE  20 18 c4
    LDA obj1_flash                      ; EFC1  ad 10 07
    AND #$0F                            ; EFC4  29 0f
    CMP #$01                            ; EFC6  c9 01
    BNE loc_31_EFDB                     ; EFC8  d0 11
    LDA orbit_level                     ; EFCA  ad c0 06
    CMP #$04                            ; EFCD  c9 04
    BNE loc_31_EFDB                     ; EFCF  d0 0a
    LDA #$00                            ; EFD1  a9 00
    STA orbit_level                     ; EFD3  8d c0 06
    LDA #$31                            ; EFD6  a9 31
    JSR PlaySound                       ; EFD8  20 25 c1
loc_31_EFDB:
    LDA obj1_flash                      ; EFDB  ad 10 07
    AND #$0F                            ; EFDE  29 0f
    CMP #$02                            ; EFE0  c9 02
    BNE loc_31_F01D                     ; EFE2  d0 39
    LDA obj_freeze                      ; EFE4  ad a0 05
    BNE loc_31_EFFC                     ; EFE7  d0 13
    LDA obj1_flash                      ; EFE9  ad 10 07
    AND #$F0                            ; EFEC  29 f0
    STA obj1_flash                      ; EFEE  8d 10 07
    LDA #$00                            ; EFF1  a9 00
    STA pal_id_spr0                     ; EFF3  8d e4 07
    JSR PaletteBuild                    ; EFF6  20 0e 8c
    JMP loc_31_F01D                     ; EFF9  4c 1d f0
loc_31_EFFC:
    LDA magic_sel                       ; EFFC  ad 12 07
    CMP #$05                            ; EFFF  c9 05
    BNE loc_31_F01D                     ; F001  d0 1a
    LDA pad_new                         ; F003  a5 4b
    AND #$80                            ; F005  29 80
    BEQ loc_31_F01D                     ; F007  f0 14
    LDA sub_state                       ; F009  a5 41
    CMP #$08                            ; F00B  c9 08
    BNE loc_31_F01D                     ; F00D  d0 0e
    LDA #$1A                            ; F00F  a9 1a
    JSR SetR6                           ; F011  20 18 c4
    LDA #$18                            ; F014  a9 18
    JSR SpendMp                         ; F016  20 e8 9a
    LDA #$18                            ; F019  a9 18
    STA sub_state                       ; F01B  85 41
loc_31_F01D:
    LDA equip_accessory                 ; F01D  ad 16 07
    CMP #$11                            ; F020  c9 11
    BNE loc_31_F03E                     ; F022  d0 1a
    LDA pad_dir                         ; F024  a5 49
    BPL loc_31_F03E                     ; F026  10 16
    LDA tmp_08                          ; F028  a5 08
    AND #$3F                            ; F02A  29 3f
    BNE loc_31_F03E                     ; F02C  d0 10
    LDA mp_cur                          ; F02E  ad 08 07
    CMP mp_max                          ; F031  cd 09 07
    BEQ loc_31_F03E                     ; F034  f0 08
    INC mp_cur                          ; F036  ee 08 07
    LDA #$04                            ; F039  a9 04
    JSR HudRedraw                       ; F03B  20 46 8e
loc_31_F03E:
    LDA equip_armor                     ; F03E  ad 13 07
    CMP #$08                            ; F041  c9 08
    BNE loc_31_F05F                     ; F043  d0 1a
    LDA pad_dir                         ; F045  a5 49
    BPL loc_31_F05F                     ; F047  10 16
    LDA player_hp_max                   ; F049  ad c0 03
    CMP player_hp                       ; F04C  cd c1 03
    BEQ loc_31_F05F                     ; F04F  f0 0e
    LDA tmp_08                          ; F051  a5 08
    AND #$0F                            ; F053  29 0f
    BNE loc_31_F05F                     ; F055  d0 08
    INC player_hp                       ; F057  ee c1 03
    LDA #$03                            ; F05A  a9 03
    JSR HudHpBar                        ; F05C  20 c0 8c
loc_31_F05F:
    LDA area_id                         ; F05F  a5 6c
    CMP #$52                            ; F061  c9 52
    BNE loc_31_F068                     ; F063  d0 03
    DEC $04FE                           ; F065  ce fe 04
loc_31_F068:
    LDA obj_timer2                      ; F068  ad 20 06
    BNE loc_31_F089                     ; F06B  d0 1c
    LDA player_bg_attr                  ; F06D  ad 41 06
    LSR A                               ; F070  4a
    BCC loc_31_F089                     ; F071  90 16
    LDA ride_platform_slot              ; F073  ad 41 05
    BNE loc_31_F089                     ; F076  d0 11
    LDA #$01                            ; F078  a9 01
    STA sub_state                       ; F07A  85 41
    LDA area_entry                      ; F07C  a5 6d
    AND #$1F                            ; F07E  29 1f
    ORA #$60                            ; F080  09 60
    STA area_entry                      ; F082  85 6d
    LDA #$00                            ; F084  a9 00
    STA player_bg_attr                  ; F086  8d 41 06
loc_31_F089:
    LDA invuln_timer                    ; F089  ad 1a 07
    BEQ loc_31_F09B                     ; F08C  f0 0d
    DEC invuln_timer                    ; F08E  ce 1a 07
    LDA obj_flags_s01                   ; F091  ad 81 03
    ORA #$04                            ; F094  09 04
    STA obj_flags_s01                   ; F096  8d 81 03
    BNE loc_31_F0A3                     ; F099  d0 08
loc_31_F09B:
    LDA obj_flags_s01                   ; F09B  ad 81 03
    AND #$FB                            ; F09E  29 fb
    STA obj_flags_s01                   ; F0A0  8d 81 03
loc_31_F0A3:
    RTS                                 ; F0A3  60
BootSaveCheck:  ; every boot: verify/repair save slots 0/1 (primary vs backup; checksum; AMI tag) and current image $7D00 vs mirror $6D00; bank 23 mapped at $A000 (was BootSub)
    LDA #$00                            ; F0A4  a9 00
    STA save_reset_flag                 ; F0A6  8d 01 70
    LDA r7_shadow                       ; F0A9  a5 6f
    STA boot_r7_save                    ; F0AB  8d 05 70
    LDA #$17                            ; F0AE  a9 17
    JSR SetR7                           ; F0B0  20 27 c4
    LDX #$00                            ; F0B3  a2 00
loc_31_F0B5:
    LDA save_slot0,X                    ; F0B5  bd 00 71
    CMP $7400,X                         ; F0B8  dd 00 74
    BNE loc_31_F0E7                     ; F0BB  d0 2a
    LDA $7200,X                         ; F0BD  bd 00 72
    CMP $7500,X                         ; F0C0  dd 00 75
    BNE loc_31_F0E7                     ; F0C3  d0 22
    LDA $7300,X                         ; F0C5  bd 00 73
    CMP $7600,X                         ; F0C8  dd 00 76
    BNE loc_31_F0E7                     ; F0CB  d0 1a
    DEX                                 ; F0CD  ca
    BNE loc_31_F0B5                     ; F0CE  d0 e5
    LDA #$41                            ; F0D0  a9 41
    CMP $71FD                           ; F0D2  cd fd 71
    BNE SaveSlot0Reset                  ; F0D5  d0 48
    LDA #$4D                            ; F0D7  a9 4d
    CMP $71FE                           ; F0D9  cd fe 71
    BNE SaveSlot0Reset                  ; F0DC  d0 41
    LDA #$49                            ; F0DE  a9 49
    CMP $71FF                           ; F0E0  cd ff 71
    BNE SaveSlot0Reset                  ; F0E3  d0 3a
    BEQ BootCheckSlot1                  ; F0E5  f0 59
loc_31_F0E7:
    LDX #$00                            ; F0E7  a2 00
    JSR SaveSumSlot                     ; F0E9  20 3d f2
    LDA save_csum0_a                    ; F0EC  ad f0 70
    CMP tmp_16                          ; F0EF  c5 16
    BNE loc_31_F103                     ; F0F1  d0 10
    LDA #$00                            ; F0F3  a9 00
    JSR SlotCopy                        ; F0F5  20 73 f2
    LDA tmp_16                          ; F0F8  a5 16
    STA save_csum0_a                    ; F0FA  8d f0 70
    STA save_csum0_b                    ; F0FD  8d f1 70
    JMP BootCheckSlot1                  ; F100  4c 40 f1
loc_31_F103:
    LDX #$01                            ; F103  a2 01
    JSR SaveSumSlot                     ; F105  20 3d f2
    LDA save_csum0_b                    ; F108  ad f1 70
    CMP tmp_16                          ; F10B  c5 16
    BNE SaveSlot0Reset                  ; F10D  d0 10
    LDA #$01                            ; F10F  a9 01
    JSR SlotCopy                        ; F111  20 73 f2
    LDA tmp_16                          ; F114  a5 16
    STA save_csum0_a                    ; F116  8d f0 70
    STA save_csum0_b                    ; F119  8d f1 70
    JMP BootCheckSlot1                  ; F11C  4c 40 f1
SaveSlot0Reset:  ; slot 0 invalid: $7100/$7400 <- template 23:BE00 + 2 zero pages; $7001=$FF
    LDX #$00                            ; F11F  a2 00
loc_31_F121:
    LDA NewSaveTemplate,X               ; F121  bd 00 be
    STA save_slot0,X                    ; F124  9d 00 71
    STA $7400,X                         ; F127  9d 00 74
    LDA #$00                            ; F12A  a9 00
    STA $7200,X                         ; F12C  9d 00 72
    STA $7300,X                         ; F12F  9d 00 73
    STA $7500,X                         ; F132  9d 00 75
    STA $7600,X                         ; F135  9d 00 76
    DEX                                 ; F138  ca
    BNE loc_31_F121                     ; F139  d0 e6
    LDA #$FF                            ; F13B  a9 ff
    STA save_reset_flag                 ; F13D  8d 01 70
BootCheckSlot1:  ; slot 1 check (same as slot 0; primary-ok path falls into the backup check)
    LDX #$00                            ; F140  a2 00
loc_31_F142:
    LDA save_slot1,X                    ; F142  bd 00 77
    CMP $7A00,X                         ; F145  dd 00 7a
    BNE loc_31_F174                     ; F148  d0 2a
    LDA $7800,X                         ; F14A  bd 00 78
    CMP $7B00,X                         ; F14D  dd 00 7b
    BNE loc_31_F174                     ; F150  d0 22
    LDA $7900,X                         ; F152  bd 00 79
    CMP $7C00,X                         ; F155  dd 00 7c
    BNE loc_31_F174                     ; F158  d0 1a
    DEX                                 ; F15A  ca
    BNE loc_31_F142                     ; F15B  d0 e5
    LDA #$41                            ; F15D  a9 41
    CMP $77FD                           ; F15F  cd fd 77
    BNE SaveSlot1Reset                  ; F162  d0 3f
    LDA #$4D                            ; F164  a9 4d
    CMP $77FE                           ; F166  cd fe 77
    BNE SaveSlot1Reset                  ; F169  d0 38
    LDA #$49                            ; F16B  a9 49
    CMP $77FF                           ; F16D  cd ff 77
    BNE SaveSlot1Reset                  ; F170  d0 31
    BEQ BootCheckCurrent                ; F172  f0 6e
loc_31_F174:
    LDX #$02                            ; F174  a2 02
    JSR SaveSumSlot                     ; F176  20 3d f2
    LDA save_csum1_a                    ; F179  ad f2 70
    CMP tmp_16                          ; F17C  c5 16
    BNE loc_31_F18A                     ; F17E  d0 0a
    LDA #$02                            ; F180  a9 02
    JSR SlotCopy                        ; F182  20 73 f2
    LDA tmp_16                          ; F185  a5 16
    STA save_csum1_a                    ; F187  8d f2 70
loc_31_F18A:
    LDX #$03                            ; F18A  a2 03
    JSR SaveSumSlot                     ; F18C  20 3d f2
    LDA save_csum1_b                    ; F18F  ad f3 70
    CMP tmp_16                          ; F192  c5 16
    BNE SaveSlot1Reset                  ; F194  d0 0d
    LDA #$03                            ; F196  a9 03
    JSR SlotCopy                        ; F198  20 73 f2
    LDA tmp_16                          ; F19B  a5 16
    STA save_csum1_b                    ; F19D  8d f3 70
    JMP BootCheckCurrent                ; F1A0  4c e2 f1
SaveSlot1Reset:  ; slot 1 invalid: $7700/$7A00 <- template 23:BE00 + 2 zero pages; $7001=$FF
    LDX #$00                            ; F1A3  a2 00
loc_31_F1A5:
    LDA NewSaveTemplate,X               ; F1A5  bd 00 be
    STA save_slot1,X                    ; F1A8  9d 00 77
    STA $7A00,X                         ; F1AB  9d 00 7a
    LDA #$00                            ; F1AE  a9 00
    STA $7800,X                         ; F1B0  9d 00 78
    STA $7B00,X                         ; F1B3  9d 00 7b
    STA $7900,X                         ; F1B6  9d 00 79
    STA $7C00,X                         ; F1B9  9d 00 7c
    DEX                                 ; F1BC  ca
    BNE loc_31_F1A5                     ; F1BD  d0 e6
    LDA #$FF                            ; F1BF  a9 ff
    STA save_reset_flag                 ; F1C1  8d 01 70
    JMP BootCheckCurrent                ; F1C4  4c e2 f1
BootSaveRestoreR7:  ; SetR7($7005) and return
    LDA boot_r7_save                    ; F1C7  ad 05 70
    JMP SetR7                           ; F1CA  4c 27 c4
ColdSaveInit:  ; cold boot only: $700E/$700F=0; unconditionally re-inits the current save image $7D00-$7FFF + mirror $6D00 from template 23:BE00 (was ColdInitSub)
    LDA #$00                            ; F1CD  a9 00
    STA wram_700E                       ; F1CF  8d 0e 70
    STA wram_700F                       ; F1D2  8d 0f 70
    LDA r7_shadow                       ; F1D5  a5 6f
    STA boot_r7_save                    ; F1D7  8d 05 70
    LDA #$17                            ; F1DA  a9 17
    JSR SetR7                           ; F1DC  20 27 c4
    JMP CurSaveReset                    ; F1DF  4c 19 f2
BootCheckCurrent:  ; compare $7D00-$7FFF with mirror $6D00-$6FFF + AMI tag at $7DFD; mismatch -> CurSaveReset (no checksum)
    LDX #$00                            ; F1E2  a2 00
loc_31_F1E4:
    LDA save_mirror,X                   ; F1E4  bd 00 6d
    CMP save_cur,X                      ; F1E7  dd 00 7d
    BNE loc_31_F216                     ; F1EA  d0 2a
    LDA save_mirror_flags,X             ; F1EC  bd 00 6e
    CMP save_flags,X                    ; F1EF  dd 00 7e
    BNE loc_31_F216                     ; F1F2  d0 22
    LDA save_mirror_npc,X               ; F1F4  bd 00 6f
    CMP save_npc_state,X                ; F1F7  dd 00 7f
    BNE loc_31_F216                     ; F1FA  d0 1a
    DEX                                 ; F1FC  ca
    BNE loc_31_F1E4                     ; F1FD  d0 e5
    LDA #$41                            ; F1FF  a9 41
    CMP save_tag                        ; F201  cd fd 7d
    BNE CurSaveReset                    ; F204  d0 13
    LDA #$4D                            ; F206  a9 4d
    CMP $7DFE                           ; F208  cd fe 7d
    BNE CurSaveReset                    ; F20B  d0 0c
    LDA #$49                            ; F20D  a9 49
    CMP $7DFF                           ; F20F  cd ff 7d
    BNE CurSaveReset                    ; F212  d0 05
    BEQ BootSaveRestoreR7               ; F214  f0 b1
loc_31_F216:
    JMP CurSaveReset                    ; F216  4c 19 f2
CurSaveReset:  ; $7D00/$6D00 <- template 23:BE00; $7E00/$7F00/$6E00/$6F00 zeroed; $7001=$FF
    LDX #$00                            ; F219  a2 00
loc_31_F21B:
    LDA NewSaveTemplate,X               ; F21B  bd 00 be
    STA save_mirror,X                   ; F21E  9d 00 6d
    STA save_cur,X                      ; F221  9d 00 7d
    LDA #$00                            ; F224  a9 00
    STA save_mirror_flags,X             ; F226  9d 00 6e
    STA save_flags,X                    ; F229  9d 00 7e
    STA save_mirror_npc,X               ; F22C  9d 00 6f
    STA save_npc_state,X                ; F22F  9d 00 7f
    DEX                                 ; F232  ca
    BNE loc_31_F21B                     ; F233  d0 e6
    LDA #$FF                            ; F235  a9 ff
    STA save_reset_flag                 ; F237  8d 01 70
    JMP BootSaveRestoreR7               ; F23A  4c c7 f1
SaveSumSlot:  ; X = page-table index (31:F26D): adds 3 pages into $16 (8-bit) WITHOUT clearing $16 first (seed quirk)
    CLC                                 ; F23D  18
    LDA SaveSumPages,X                  ; F23E  bd 6d f2
    STA tmp_11                          ; F241  85 11
    ADC #$01                            ; F243  69 01
    STA tmp_13                          ; F245  85 13
    ADC #$01                            ; F247  69 01
    STA tmp_15                          ; F249  85 15
    LDA #$00                            ; F24B  a9 00
    STA jmp_trampoline                  ; F24D  85 10
    STA tmp_12                          ; F24F  85 12
    STA tmp_14                          ; F251  85 14
    TAY                                 ; F253  a8
loc_31_F254:
    LDA (jmp_trampoline),Y              ; F254  b1 10
    CLC                                 ; F256  18
    ADC tmp_16                          ; F257  65 16
    STA tmp_16                          ; F259  85 16
    LDA (tmp_12),Y                      ; F25B  b1 12
    CLC                                 ; F25D  18
    ADC tmp_16                          ; F25E  65 16
    STA tmp_16                          ; F260  85 16
    LDA (tmp_14),Y                      ; F262  b1 14
    CLC                                 ; F264  18
    ADC tmp_16                          ; F265  65 16
    STA tmp_16                          ; F267  85 16
    INY                                 ; F269  c8
    BNE loc_31_F254                     ; F26A  d0 e8
    RTS                                 ; F26C  60
SaveSumPages:  ; 6 page bases for SaveSumSlot: 71 74 77 7A 7D 7D
    .db $71,$74,$77,$7A,$7D,$7D         ; 31:F26D
SlotCopy:  ; A = pair index: copy 3 pages src->dst from SlotCopyPages 31:F298
    ASL A                               ; F273  0a
    TAX                                 ; F274  aa
    LDA SlotCopyPages,X                 ; F275  bd 98 f2
    STA tmp_11                          ; F278  85 11
    LDA $F299,X                         ; F27A  bd 99 f2
    STA tmp_13                          ; F27D  85 13
    LDA #$00                            ; F27F  a9 00
    STA jmp_trampoline                  ; F281  85 10
    STA tmp_12                          ; F283  85 12
    TAY                                 ; F285  a8
    TAX                                 ; F286  aa
loc_31_F287:
    LDA (jmp_trampoline),Y              ; F287  b1 10
    STA (tmp_12),Y                      ; F289  91 12
    INY                                 ; F28B  c8
    BNE loc_31_F287                     ; F28C  d0 f9
    INC tmp_11                          ; F28E  e6 11
    INC tmp_13                          ; F290  e6 13
    INX                                 ; F292  e8
    CPX #$03                            ; F293  e0 03
    BNE loc_31_F287                     ; F295  d0 f0
    RTS                                 ; F297  60
SlotCopyPages:  ; 6 src/dst page pairs: 71>74 74>71 77>7A 7A>77 7D>7D 7D>7D
    .db $71,$74,$74,$71,$77,$7A,$7A,$77,$7D,$7D,$7D,$7D; 31:F298
Reset:  ; RESET vector entry
    SEI                                 ; F2A4  78
    CLD                                 ; F2A5  d8
    LDX #$FF                            ; F2A6  a2 ff
    TXS                                 ; F2A8  9a
    LDA #$00                            ; F2A9  a9 00
    STA PPUCTRL                         ; F2AB  8d 00 20
    STA PPUMASK                         ; F2AE  8d 01 20
    LDX #$02                            ; F2B1  a2 02
loc_31_F2B3:
    BIT PPUSTATUS                       ; F2B3  2c 02 20
    BPL loc_31_F2B3                     ; F2B6  10 fb
loc_31_F2B8:
    BIT PPUSTATUS                       ; F2B8  2c 02 20
    BMI loc_31_F2B8                     ; F2BB  30 fb
    DEX                                 ; F2BD  ca
    BNE loc_31_F2B3                     ; F2BE  d0 f3
    LDA #$08                            ; F2C0  a9 08
    STA PPUCTRL                         ; F2C2  8d 00 20
    LDA #$00                            ; F2C5  a9 00
    STA nmi_disable                     ; F2C7  85 60
    STA DMC_FREQ                        ; F2C9  8d 10 40
    LDA #$40                            ; F2CC  a9 40
    STA JOY2_FRAMECNT                   ; F2CE  8d 17 40
    LDA #$01                            ; F2D1  a9 01
    STA EndingInit                      ; F2D3  8d 00 a0
    LDA #$80                            ; F2D6  a9 80
    STA $A001                           ; F2D8  8d 01 a0
    STA $E000                           ; F2DB  8d 00 e0
    LDA #$00                            ; F2DE  a9 00
    STA $8000                           ; F2E0  8d 00 80
    LDA #$03                            ; F2E3  a9 03
    STA irq_mode                        ; F2E5  85 51
    LDA #$00                            ; F2E7  a9 00
    STA snd_queue_count                 ; F2E9  8d 00 01
    STA snd_queue_lock                  ; F2EC  8d 01 01
    STA snd_cur_music                   ; F2EF  8d 02 01
    STA nmi_busy_flag                   ; F2F2  85 06
    STA nmi_missed_flag                 ; F2F4  85 07
    LDA IrqDefaultPtr                   ; F2F6  ad 22 f4
    STA a:irq_vector                    ; F2F9  8d 54 00
    LDA $F423                           ; F2FC  ad 23 f4
    STA a:$0055                         ; F2FF  8d 55 00
    LDA PPUSTATUS                       ; F302  ad 02 20
    LDA #$10                            ; F305  a9 10
    TAX                                 ; F307  aa
loc_31_F308:
    STA PPUADDR                         ; F308  8d 06 20
    STA PPUADDR                         ; F30B  8d 06 20
    EOR #$10                            ; F30E  49 10
    DEX                                 ; F310  ca
    BNE loc_31_F308                     ; F311  d0 f5
    LDX #$1F                            ; F313  a2 1f
    LDA #$FF                            ; F315  a9 ff
loc_31_F317:
    STA ppu_queue,X                     ; F317  9d 00 62
    STA pal_buf,X                       ; F31A  9d 40 61
    DEX                                 ; F31D  ca
    BPL loc_31_F317                     ; F31E  10 f7
    LDA #$0C                            ; F320  a9 0c
    JSR SwitchBank16                    ; F322  20 0e c4
    JSR SndInit                         ; F325  20 20 80
    LDA #$00                            ; F328  a9 00
    STA tmp_0A                          ; F32A  85 0a
    STA tmp_0B                          ; F32C  85 0b
    STA ram_0744                        ; F32E  8d 44 07
    LDA #$02                            ; F331  a9 02
    STA main_state                      ; F333  85 40
    LDA #$00                            ; F335  a9 00
    STA frame_ready                     ; F337  85 09
    STA ppumask_shadow                  ; F339  85 01
    LDX #$00                            ; F33B  a2 00
    TXA                                 ; F33D  8a
loc_31_F33E:
    STA $0700,X                         ; F33E  9d 00 07
    DEX                                 ; F341  ca
    BNE loc_31_F33E                     ; F342  d0 fa
    LDA #$00                            ; F344  a9 00
    STA $44                             ; F346  85 44
    STA $46                             ; F348  85 46
    STA $48                             ; F34A  85 48
    STA $4A                             ; F34C  85 4a
    STA zp_4C                           ; F34E  85 4c
    STA $4E                             ; F350  85 4e
    LDA #$88                            ; F352  a9 88
    STA ppuctrl_shadow                  ; F354  85 00
    STA PPUCTRL                         ; F356  8d 00 20
    LDA #$09                            ; F359  a9 09
    STA irq_delay                       ; F35B  8d fe 07
    CLI                                 ; F35E  58
    LDA warmboot_sig                    ; F35F  ad 82 01
    CMP #$55                            ; F362  c9 55
    BNE ColdBootInit                    ; F364  d0 0e
    LDA $0183                           ; F366  ad 83 01
    CMP #$53                            ; F369  c9 53
    BNE ColdBootInit                    ; F36B  d0 07
    LDA $0184                           ; F36D  ad 84 01
    CMP #$4F                            ; F370  c9 4f
    BEQ WarmBootContinue                ; F372  f0 2b
ColdBootInit:  ; taken when $0182-0184 != 'USO' signature
    JSR ColdSaveInit                    ; F374  20 cd f1
    LDA #$00                            ; F377  a9 00
    STA boot_ab_flag                    ; F379  85 0f
    LDA #$55                            ; F37B  a9 55
    STA warmboot_sig                    ; F37D  8d 82 01
    LDA #$53                            ; F380  a9 53
    STA $0183                           ; F382  8d 83 01
    LDA #$4F                            ; F385  a9 4f
    STA $0184                           ; F387  8d 84 01
    JSR ReadPads                        ; F38A  20 80 fe
    LDA pad_prev                        ; F38D  a5 43
    CMP #$C0                            ; F38F  c9 c0
    BNE loc_31_F397                     ; F391  d0 04
    LDA #$01                            ; F393  a9 01
    STA boot_ab_flag                    ; F395  85 0f
loc_31_F397:
    LDA #$09                            ; F397  a9 09
    JSR SwitchBank16                    ; F399  20 0e c4
    JSR IntroColdBoot                   ; F39C  20 28 80
WarmBootContinue:  ; common path after cold/warm boot
    JSR BootSaveCheck                   ; F39F  20 a4 f0
CopySramBlock:  ; copy $7D00-$7EFF -> $6480-$657F then JMP MainLoop
    LDX #$00                            ; F3A2  a2 00
loc_31_F3A4:
    LDA save_cur,X                      ; F3A4  bd 00 7d
    STA story_flags,X                   ; F3A7  9d 80 64
    LDA save_flags,X                    ; F3AA  bd 00 7e
    STA npc_state,X                     ; F3AD  9d 80 65
    INX                                 ; F3B0  e8
    BNE loc_31_F3A4                     ; F3B1  d0 f1
    JMP MainLoop                        ; F3B3  4c 00 c9
NMI:  ; NMI vector (vblank); skips work if $60!=0 or $06!=0
    PHA                                 ; F3B6  48
    LDA PPUSTATUS                       ; F3B7  ad 02 20
    LDA nmi_disable                     ; F3BA  a5 60
    BNE loc_31_F3C6                     ; F3BC  d0 08
    LDA nmi_busy_flag                   ; F3BE  a5 06
    BEQ loc_31_F3C8                     ; F3C0  f0 06
    LDA #$01                            ; F3C2  a9 01
    STA nmi_missed_flag                 ; F3C4  85 07
loc_31_F3C6:
    PLA                                 ; F3C6  68
    RTI                                 ; F3C7  40
loc_31_F3C8:
    TYA                                 ; F3C8  98
    PHA                                 ; F3C9  48
    TXA                                 ; F3CA  8a
    PHA                                 ; F3CB  48
    LDA frame_ready                     ; F3CC  a5 09
    BNE loc_31_F40A                     ; F3CE  d0 3a
    STA OAMADDR                         ; F3D0  8d 03 20
    LDA #$02                            ; F3D3  a9 02
    STA OAMDMA                          ; F3D5  8d 14 40
    LDA scroll_shadow0                  ; F3D8  a5 02
    STA scroll_x_irq                    ; F3DA  8d d8 07
    LDA cam_x_hi                        ; F3DD  a5 03
    STA $07D9                           ; F3DF  8d d9 07
    LDA cam_y_lo                        ; F3E2  a5 04
    STA ram_07DA                        ; F3E4  8d da 07
    LDA cam_y_hi                        ; F3E7  a5 05
    STA ram_07DB                        ; F3E9  8d db 07
    LDA irq_mode                        ; F3EC  a5 51
    CMP #$09                            ; F3EE  c9 09
    BEQ loc_31_F3F6                     ; F3F0  f0 04
    CMP #$07                            ; F3F2  c9 07
    BNE loc_31_F40A                     ; F3F4  d0 14
loc_31_F3F6:
    LDA $83                             ; F3F6  a5 83
    STA scroll_x_irq                    ; F3F8  8d d8 07
    LDA $A3                             ; F3FB  a5 a3
    STA $07D9                           ; F3FD  8d d9 07
    LDA $C3                             ; F400  a5 c3
    STA ram_07DA                        ; F402  8d da 07
    LDA $E3                             ; F405  a5 e3
    STA ram_07DB                        ; F407  8d db 07
loc_31_F40A:
    JSR PpuQueueFlush                   ; F40A  20 7d c6
    LDX #$00                            ; F40D  a2 00
    JSR PaletteUpload                   ; F40F  20 cb f8
    LDA ppumask_shadow                  ; F412  a5 01
    STA PPUMASK                         ; F414  8d 01 20
    JSR NmiModeDispatch                 ; F417  20 ad f6
    INC frame_ready                     ; F41A  e6 09
    PLA                                 ; F41C  68
    TAX                                 ; F41D  aa
    PLA                                 ; F41E  68
    TAY                                 ; F41F  a8
    PLA                                 ; F420  68
IrqDefault:  ; default IRQ handler target (initial $0054/$0055)
    RTI                                 ; F421  40
IrqDefaultPtr:  ; .dw $F421 - Reset copies this word to $0054/$0055 (LDA $F422/$F423)
    .db $21,$F4                         ; 31:F422
SetIrqHandler:  ; A=index -> $56; $0054/$0055 = IrqHandlerTable[A]
    STA irq_handler_idx                 ; F424  85 56
    ASL A                               ; F426  0a
    TAX                                 ; F427  aa
    LDA IrqHandlerTable,X               ; F428  bd 33 f4
    STA irq_vector                      ; F42B  85 54
    LDA $F434,X                         ; F42D  bd 34 f4
    STA $55                             ; F430  85 55
    RTS                                 ; F432  60
IrqHandlerTable:  ; ptr16[8] IRQ handlers: F455 F488 F4E5 F54B F5E1 F60E F691 F61A
    .dw IrqHandler0                     ; 31:F433
    .dw IrqHandler1Hud                  ; 31:F435
    .dw IrqMsgWindowTop                 ; 31:F437
    .dw IrqMsgWindowEnd                 ; 31:F439
    .dw IrqRowStretch                   ; 31:F43B
    .dw IrqOffSound                     ; 31:F43D
    .dw IrqHandler6                     ; 31:F43F
    .dw IrqMenuChrSplit                 ; 31:F441
IRQ:  ; IRQ vector (MMC3 scanline IRQ); ack then JMP ($0054)
    PHA                                 ; F443  48
    TXA                                 ; F444  8a
    PHA                                 ; F445  48
    TYA                                 ; F446  98
    PHA                                 ; F447  48
    STA $E000                           ; F448  8d 00 e0
    STA $E001                           ; F44B  8d 01 e0
    JMP (irq_vector)                    ; F44E  6c 54 00
DelayX:  ; busy loop DEX/BPL
    DEX                                 ; F451  ca
    BPL DelayX                          ; F452  10 fd
    RTS                                 ; F454  60
IrqHandler0:  ; s65: playfield vertical-wrap split: PPUADDR $2000+(scroll_x>>3) (NT0 row 0) + fine X; arms H1 HUD at $BC-$57
    LDX irq_delay                       ; F455  ae fe 07
    JSR DelayX                          ; F458  20 51 f4
    LDA PPUSTATUS                       ; F45B  ad 02 20
    LDA #$00                            ; F45E  a9 00
    STA PPUADDR                         ; F460  8d 06 20
    LDA scroll_x_irq                    ; F463  ad d8 07
    LSR A                               ; F466  4a
    LSR A                               ; F467  4a
    LSR A                               ; F468  4a
    STA PPUADDR                         ; F469  8d 06 20
    LDA scroll_x_irq                    ; F46C  ad d8 07
    STA PPUSCROLL                       ; F46F  8d 05 20
    LDA #$BC                            ; F472  a9 bc
    SEC                                 ; F474  38
    SBC zp_57                           ; F475  e5 57
    STA BitMask                         ; F477  8d 00 c0
    STA $C001                           ; F47A  8d 01 c0
    LDA #$01                            ; F47D  a9 01
    JSR SetIrqHandler                   ; F47F  20 24 f4
    PLA                                 ; F482  68
    TAY                                 ; F483  a8
    PLA                                 ; F484  68
    TAX                                 ; F485  aa
    PLA                                 ; F486  68
    RTI                                 ; F487  40
IrqHandler1Hud:  ; s41: gameplay HUD split (line 190 in tools.emu): sprites off; PPUADDR $2B00; scroll X 0; CHR R0=$3C R1=$38; SoundUpdate with $06=1
    STA $E000                           ; F488  8d 00 e0
    LDA PPUSTATUS                       ; F48B  ad 02 20
    LDX irq_delay                       ; F48E  ae fe 07
    INX                                 ; F491  e8
    INX                                 ; F492  e8
    INX                                 ; F493  e8
    JSR DelayX                          ; F494  20 51 f4
    LDA ppumask_shadow                  ; F497  a5 01
    AND #$0E                            ; F499  29 0e
    ORA #$08                            ; F49B  09 08
    STA PPUMASK                         ; F49D  8d 01 20
    LDA PPUSTATUS                       ; F4A0  ad 02 20
    LDA #$2B                            ; F4A3  a9 2b
    STA PPUADDR                         ; F4A5  8d 06 20
    LDA #$00                            ; F4A8  a9 00
    STA PPUADDR                         ; F4AA  8d 06 20
    LDA #$00                            ; F4AD  a9 00
    STA PPUSCROLL                       ; F4AF  8d 05 20
    LDX #$00                            ; F4B2  a2 00
    STX $8000                           ; F4B4  8e 00 80
    LDA #$3C                            ; F4B7  a9 3c
    STA $8001                           ; F4B9  8d 01 80
    INX                                 ; F4BC  e8
    STX $8000                           ; F4BD  8e 00 80
    LDA #$38                            ; F4C0  a9 38
    STA $8001                           ; F4C2  8d 01 80
    LDA a:mmc3_banksel_shadow           ; F4C5  ad 50 00
    STA $8000                           ; F4C8  8d 00 80
    LDA #$01                            ; F4CB  a9 01
    STA nmi_busy_flag                   ; F4CD  85 06
    JSR SoundUpdate                     ; F4CF  20 fe f7
    DEC nmi_busy_flag                   ; F4D2  c6 06
    LDA nmi_missed_flag                 ; F4D4  a5 07
    BEQ loc_31_F4DB                     ; F4D6  f0 03
    JSR NmiModeDispatch                 ; F4D8  20 ad f6
loc_31_F4DB:
    LDA #$00                            ; F4DB  a9 00
    STA nmi_missed_flag                 ; F4DD  85 07
    PLA                                 ; F4DF  68
    TAY                                 ; F4E0  a8
    PLA                                 ; F4E1  68
    TAX                                 ; F4E2  aa
    PLA                                 ; F4E3  68
    RTI                                 ; F4E4  40
IrqMsgWindowTop:  ; s65: IRQ handler 2 (irq_mode 1/9): PPUADDR $2800 (NT2 message window) + ApplyChrAlt; arms H3 after $48 lines; precomputes resume addr $5E/$5F
    LDX irq_delay                       ; F4E5  ae fe 07
    JSR DelayX                          ; F4E8  20 51 f4
    NOP                                 ; F4EB  ea
    NOP                                 ; F4EC  ea
    NOP                                 ; F4ED  ea
    NOP                                 ; F4EE  ea
    NOP                                 ; F4EF  ea
    LDA PPUSTATUS                       ; F4F0  ad 02 20
    LDA #$28                            ; F4F3  a9 28
    STA PPUADDR                         ; F4F5  8d 06 20
    LDA #$00                            ; F4F8  a9 00
    STA PPUADDR                         ; F4FA  8d 06 20
    STA PPUSCROLL                       ; F4FD  8d 05 20
    JSR ApplyChrAlt                     ; F500  20 4f f6
    LDY #$48                            ; F503  a0 48
    STY BitMask                         ; F505  8c 00 c0
    STY $C001                           ; F508  8c 01 c0
    DEY                                 ; F50B  88
    DEY                                 ; F50C  88
    TYA                                 ; F50D  98
    CLC                                 ; F50E  18
    ADC zp_57                           ; F50F  65 57
    STA zp_57                           ; F511  85 57
    LDA #$03                            ; F513  a9 03
    JSR SetIrqHandler                   ; F515  20 24 f4
    LDA ram_07DA                        ; F518  ad da 07
    CLC                                 ; F51B  18
    ADC #$08                            ; F51C  69 08
    CLC                                 ; F51E  18
    ADC zp_57                           ; F51F  65 57
    BCS loc_31_F527                     ; F521  b0 04
    CMP #$F0                            ; F523  c9 f0
    BCC loc_31_F529                     ; F525  90 02
loc_31_F527:
    ADC #$10                            ; F527  69 10
loc_31_F529:
    AND #$F8                            ; F529  29 f8
    LSR A                               ; F52B  4a
    LSR A                               ; F52C  4a
    TAY                                 ; F52D  a8
    LDA zp_57                           ; F52E  a5 57
    CLC                                 ; F530  18
    ADC #$08                            ; F531  69 08
    STA zp_57                           ; F533  85 57
    LDA $F5A2,Y                         ; F535  b9 a2 f5
    STA zp_5E                           ; F538  85 5e
    LDA scroll_x_irq                    ; F53A  ad d8 07
    LSR A                               ; F53D  4a
    LSR A                               ; F53E  4a
    LSR A                               ; F53F  4a
    ORA NtRowAddrTable,Y                ; F540  19 a1 f5
    STA zp_5F                           ; F543  85 5f
    PLA                                 ; F545  68
    TAY                                 ; F546  a8
    PLA                                 ; F547  68
    TAX                                 ; F548  aa
    PLA                                 ; F549  68
    RTI                                 ; F54A  40
IrqMsgWindowEnd:  ; s65: IRQ handler 3 (chain from H2 only): PPUADDR $5E/$5F (NT0 playfield row) + fine X + ApplyChrPlay; arms H0 (wrap) or H1 (HUD)
    LDX irq_delay                       ; F54B  ae fe 07
    JSR DelayX                          ; F54E  20 51 f4
    LDA PPUSTATUS                       ; F551  ad 02 20
    LDA zp_5E                           ; F554  a5 5e
    STA PPUADDR                         ; F556  8d 06 20
    LDA zp_5F                           ; F559  a5 5f
    STA PPUADDR                         ; F55B  8d 06 20
    LDA scroll_x_irq                    ; F55E  ad d8 07
    STA PPUSCROLL                       ; F561  8d 05 20
    JSR ApplyChrPlay                    ; F564  20 83 f8
    LDA #$EF                            ; F567  a9 ef
    SEC                                 ; F569  38
    SBC ram_07DA                        ; F56A  ed da 07
    CMP #$BC                            ; F56D  c9 bc
    BCS IrqMsgWindowEndHud              ; F56F  b0 1a
    CMP zp_57                           ; F571  c5 57
    BCC IrqMsgWindowEndHud              ; F573  90 16
    TAY                                 ; F575  a8
    SBC zp_57                           ; F576  e5 57
    STA BitMask                         ; F578  8d 00 c0
    STA $C001                           ; F57B  8d 01 c0
    STY zp_57                           ; F57E  84 57
    LDA #$00                            ; F580  a9 00
    JSR SetIrqHandler                   ; F582  20 24 f4
    PLA                                 ; F585  68
    TAY                                 ; F586  a8
    PLA                                 ; F587  68
    TAX                                 ; F588  aa
    PLA                                 ; F589  68
    RTI                                 ; F58A  40
IrqMsgWindowEndHud:  ; s65: H3 tail: arm H1 at $BE-$57
    LDA #$BE                            ; F58B  a9 be
    SEC                                 ; F58D  38
    SBC zp_57                           ; F58E  e5 57
    STA BitMask                         ; F590  8d 00 c0
    STA $C001                           ; F593  8d 01 c0
    LDA #$01                            ; F596  a9 01
    JSR SetIrqHandler                   ; F598  20 24 f4
    PLA                                 ; F59B  68
    TAY                                 ; F59C  a8
    PLA                                 ; F59D  68
    TAX                                 ; F59E  aa
    PLA                                 ; F59F  68
    RTI                                 ; F5A0  40
NtRowAddrTable:  ; s65: 32 words $2000..$23E0 = NT0 row addresses (hi read at F5A2+Y; lo at F5A1+Y) by IRQ handlers 2 and 4
    .db $00,$20,$20,$20,$40,$20,$60,$20,$80,$20,$A0,$20,$C0,$20,$E0,$20; 31:F5A1
    .db $00,$21,$20,$21,$40,$21,$60,$21,$80,$21,$A0,$21,$C0,$21,$E0,$21; 31:F5B1
    .db $00,$22,$20,$22,$40,$22,$60,$22,$80,$22,$A0,$22,$C0,$22,$E0,$22; 31:F5C1
    .db $00,$23,$20,$23,$40,$23,$60,$23,$80,$23,$A0,$23,$C0,$23,$E0,$23; 31:F5D1
IrqRowStretch:  ; s65: IRQ handler 4 (dead irq_mode 4): PPUADDR NtRowAddrTable[$5A++] every $58+1 lines; $59 rows then falls into F60E
    LDX irq_delay                       ; F5E1  ae fe 07
    JSR DelayX                          ; F5E4  20 51 f4
    LDA PPUSTATUS                       ; F5E7  ad 02 20
    LDA chr_r2_alt                      ; F5EA  a5 5a
    ASL A                               ; F5EC  0a
    TAY                                 ; F5ED  a8
    LDA $F5A2,Y                         ; F5EE  b9 a2 f5
    STA PPUADDR                         ; F5F1  8d 06 20
    LDA NtRowAddrTable,Y                ; F5F4  b9 a1 f5
    STA PPUADDR                         ; F5F7  8d 06 20
    INC chr_r2_alt                      ; F5FA  e6 5a
    DEC chr_r1_alt                      ; F5FC  c6 59
    BMI IrqOffSound                     ; F5FE  30 0e
    LDA chr_r0_alt                      ; F600  a5 58
    STA BitMask                         ; F602  8d 00 c0
    STA $C001                           ; F605  8d 01 c0
    PLA                                 ; F608  68
    TAY                                 ; F609  a8
    PLA                                 ; F60A  68
    TAX                                 ; F60B  aa
    PLA                                 ; F60C  68
    RTI                                 ; F60D  40
IrqOffSound:  ; s65: IRQ handler 5: IRQ off + SoundUpdate; reached only from F5FE (H4 end) and never via SetIrqHandler
    STA $E000                           ; F60E  8d 00 e0
    JSR SoundUpdate                     ; F611  20 fe f7
    PLA                                 ; F614  68
    TAY                                 ; F615  a8
    PLA                                 ; F616  68
    TAX                                 ; F617  aa
    PLA                                 ; F618  68
    RTI                                 ; F619  40
IrqMenuChrSplit:  ; s65: IRQ handler 7 (irq_mode 6 equipment menu; line 119): CHR R0/R1 <- $5E/$5F; arms H1 HUD at $BC-$57
    LDX irq_delay                       ; F61A  ae fe 07
    JSR DelayX                          ; F61D  20 51 f4
    LDX #$00                            ; F620  a2 00
    STX $8000                           ; F622  8e 00 80
    LDA zp_5E                           ; F625  a5 5e
    STA $8001                           ; F627  8d 01 80
    INX                                 ; F62A  e8
    STX $8000                           ; F62B  8e 00 80
    LDA zp_5F                           ; F62E  a5 5f
    STA $8001                           ; F630  8d 01 80
    LDA a:mmc3_banksel_shadow           ; F633  ad 50 00
    STA $8000                           ; F636  8d 00 80
    LDA #$BC                            ; F639  a9 bc
    SEC                                 ; F63B  38
    SBC zp_57                           ; F63C  e5 57
    STA BitMask                         ; F63E  8d 00 c0
    STA $C001                           ; F641  8d 01 c0
    LDA #$01                            ; F644  a9 01
    JSR SetIrqHandler                   ; F646  20 24 f4
    PLA                                 ; F649  68
    TAY                                 ; F64A  a8
    PLA                                 ; F64B  68
    TAX                                 ; F64C  aa
    PLA                                 ; F64D  68
    RTI                                 ; F64E  40
ApplyChrAlt:  ; write $58-$5D to MMC3 R0-R5
    LDA #$00                            ; F64F  a9 00
    STA $8000                           ; F651  8d 00 80
    LDA chr_r0_alt                      ; F654  a5 58
    STA $8001                           ; F656  8d 01 80
    LDA #$01                            ; F659  a9 01
    STA $8000                           ; F65B  8d 00 80
    LDA chr_r1_alt                      ; F65E  a5 59
    STA $8001                           ; F660  8d 01 80
    LDA #$02                            ; F663  a9 02
    STA $8000                           ; F665  8d 00 80
    LDA chr_r2_alt                      ; F668  a5 5a
    STA $8001                           ; F66A  8d 01 80
    LDA #$03                            ; F66D  a9 03
    STA $8000                           ; F66F  8d 00 80
    LDA chr_r3_alt                      ; F672  a5 5b
    STA $8001                           ; F674  8d 01 80
    LDA #$04                            ; F677  a9 04
    STA $8000                           ; F679  8d 00 80
    LDA chr_r4_alt                      ; F67C  a5 5c
    STA $8001                           ; F67E  8d 01 80
    LDA #$05                            ; F681  a9 05
    STA $8000                           ; F683  8d 00 80
    LDA chr_r5_alt                      ; F686  a5 5d
    STA $8001                           ; F688  8d 01 80
    LDA mmc3_banksel_shadow             ; F68B  a5 50
    STA $8000                           ; F68D  8d 00 80
    RTS                                 ; F690  60
IrqHandler6:  ; s41: title/intro IRQ handler (line 137 in tools.emu); set every frame by bank 18 intro (irq_mode 5); s65: PPUADDR <- $59:$5A (intro: $2800 at line $89=137)
    LDX irq_delay                       ; F691  ae fe 07
    JSR DelayX                          ; F694  20 51 f4
    STA $E000                           ; F697  8d 00 e0
    LDA PPUSTATUS                       ; F69A  ad 02 20
    LDA chr_r1_alt                      ; F69D  a5 59
    STA PPUADDR                         ; F69F  8d 06 20
    LDA chr_r2_alt                      ; F6A2  a5 5a
    STA PPUADDR                         ; F6A4  8d 06 20
    PLA                                 ; F6A7  68
    TAY                                 ; F6A8  a8
    PLA                                 ; F6A9  68
    TAX                                 ; F6AA  aa
    PLA                                 ; F6AB  68
    RTI                                 ; F6AC  40
NmiModeDispatch:  ; s41: $57=0; JMP (JmpTable31_F6C2[irq_mode]); called every NMI (F417) and from IRQ handler 1 when $07 (missed NMI) set
    LDA #$00                            ; F6AD  a9 00
    STA zp_57                           ; F6AF  85 57
    LDA irq_mode                        ; F6B1  a5 51
    ASL A                               ; F6B3  0a
    TAX                                 ; F6B4  aa
    LDA JmpTable31_F6C2,X               ; F6B5  bd c2 f6
    STA $52                             ; F6B8  85 52
    LDA $F6C3,X                         ; F6BA  bd c3 f6
    STA $53                             ; F6BD  85 53
    JMP ($0052)                         ; F6BF  6c 52 00
JmpTable31_F6C2:  ; NMI-mode routine table [irq_mode] used by NmiModeDispatch 31:F6AD: F6D6 F731 F751 F771 F782 F7C5 F7DB F6D6 0000 F731 (s41; mode 8 null)
    .dw NmiMode0                        ; 31:F6C2
    .dw NmiMode1                        ; 31:F6C4
    .dw NmiMode2                        ; 31:F6C6
    .dw NmiMode3                        ; 31:F6C8
    .dw NmiMode4                        ; 31:F6CA
    .dw NmiMode5                        ; 31:F6CC
    .dw NmiMode6                        ; 31:F6CE
    .dw NmiMode0                        ; 31:F6D0
    .dw $0000                           ; 31:F6D2
    .dw NmiMode1                        ; 31:F6D4
NmiMode0:  ; s41: irq_mode 0/7 NMI routine; JMP SoundUpdate only when PPUMASK shadow $01&$18=0 (rendering off so no HUD IRQ)
    LDA ppumask_shadow                  ; F6D6  a5 01
    AND #$18                            ; F6D8  29 18
    BNE loc_31_F6DF                     ; F6DA  d0 03
    JMP SoundUpdate                     ; F6DC  4c fe f7
loc_31_F6DF:
    JSR NmiScrollSet                    ; F6DF  20 1b f7
    JSR ApplyChrPlay                    ; F6E2  20 83 f8
    LDA ppuctrl_shadow                  ; F6E5  a5 00
    LSR A                               ; F6E7  4a
    LSR A                               ; F6E8  4a
    BCS NmiMode0HudIrq                  ; F6E9  b0 1a
    LDA #$EF                            ; F6EB  a9 ef
    SEC                                 ; F6ED  38
    SBC ram_07DA                        ; F6EE  ed da 07
    CMP #$BC                            ; F6F1  c9 bc
    BCS NmiMode0HudIrq                  ; F6F3  b0 10
    STA BitMask                         ; F6F5  8d 00 c0
    STA zp_57                           ; F6F8  85 57
    STA $C001                           ; F6FA  8d 01 c0
    STA $E001                           ; F6FD  8d 01 e0
    LDA #$00                            ; F700  a9 00
    JMP SetIrqHandler                   ; F702  4c 24 f4
NmiMode0HudIrq:  ; s51: SetIrqHandler(1); latch = $BE - $57 (= $BE unless handler 0 was armed first); IRQ on. Reached when ppuctrl_shadow b1 set or $EF-$07DA >= $BC; else F6F5 arms handler 0 at latch $EF-$07DA
    LDA #$01                            ; F705  a9 01
    JSR SetIrqHandler                   ; F707  20 24 f4
    LDA #$BE                            ; F70A  a9 be
    SEC                                 ; F70C  38
    SBC zp_57                           ; F70D  e5 57
    STA zp_57                           ; F70F  85 57
    STA BitMask                         ; F711  8d 00 c0
    STA $C001                           ; F714  8d 01 c0
    STA $E001                           ; F717  8d 01 e0
    RTS                                 ; F71A  60
NmiScrollSet:  ; s51: PpuScrollWrite with $07DB temporarily = ($07DA >= $E8); called by NmiMode0 F6DF (web: nmiScrollSet)
    LDX ram_07DB                        ; F71B  ae db 07
    LDA #$00                            ; F71E  a9 00
    LDY ram_07DA                        ; F720  ac da 07
    CPY #$E8                            ; F723  c0 e8
    ADC #$00                            ; F725  69 00
    STA ram_07DB                        ; F727  8d db 07
    JSR PpuScrollWrite                  ; F72A  20 6d eb
    STX ram_07DB                        ; F72D  8e db 07
    RTS                                 ; F730  60
NmiMode1:  ; s64: irq_mode 1/9 NMI routine: NmiScrollSet + ApplyChrPlay; arm IRQ handler 2 at latch ((07DA&7)^7)+7
    JSR NmiScrollSet                    ; F731  20 1b f7
    JSR ApplyChrPlay                    ; F734  20 83 f8
    LDA ram_07DA                        ; F737  ad da 07
    AND #$07                            ; F73A  29 07
    EOR #$07                            ; F73C  49 07
    CLC                                 ; F73E  18
    ADC #$07                            ; F73F  69 07
    STA BitMask                         ; F741  8d 00 c0
    STA zp_57                           ; F744  85 57
    STA $C001                           ; F746  8d 01 c0
    STA $E001                           ; F749  8d 01 e0
    LDA #$02                            ; F74C  a9 02
    JMP SetIrqHandler                   ; F74E  4c 24 f4
NmiMode2:  ; s64: irq_mode 2 (pause/status): PPUCTRL NT2 ($2800); scroll 0;0; ApplyChrAlt; rendering on -> NmiMode0HudIrq else JMP SoundUpdate
    LDA ppuctrl_shadow                  ; F751  a5 00
    AND #$FC                            ; F753  29 fc
    ORA #$02                            ; F755  09 02
    STA PPUCTRL                         ; F757  8d 00 20
    LDA #$00                            ; F75A  a9 00
    STA PPUSCROLL                       ; F75C  8d 05 20
    STA PPUSCROLL                       ; F75F  8d 05 20
    JSR ApplyChrAlt                     ; F762  20 4f f6
    LDA ppumask_shadow                  ; F765  a5 01
    AND #$18                            ; F767  29 18
    BNE loc_31_F76E                     ; F769  d0 03
    JMP SoundUpdate                     ; F76B  4c fe f7
loc_31_F76E:
    JMP NmiMode0HudIrq                  ; F76E  4c 05 f7
NmiMode3:  ; s57: irq_mode 3 (title/menus): PPUMASK<-$01; PpuScrollWrite; ApplyChrPlay; MMC3 IRQ off; JMP SoundUpdate (web: mainloop.js nmiMode3)
    LDA ppumask_shadow                  ; F771  a5 01
    STA PPUMASK                         ; F773  8d 01 20
    JSR PpuScrollWrite                  ; F776  20 6d eb
    JSR ApplyChrPlay                    ; F779  20 83 f8
    STA $E000                           ; F77C  8d 00 e0
    JMP SoundUpdate                     ; F77F  4c fe f7
NmiMode4:  ; s64: irq_mode 4 (dead: shop-door effect stubbed out): R1 alt = F7B5[$58]; $58!=0 -> handler 4 at line 4 (+stray STA $F451); $58=0 -> BG off + IRQ off
    JSR PpuScrollWrite                  ; F782  20 6d eb
    JSR ApplyChrPlay                    ; F785  20 83 f8
    LDA chr_r0_alt                      ; F788  a5 58
    BEQ loc_31_F7AA                     ; F78A  f0 1e
    LDY chr_r0_alt                      ; F78C  a4 58
    LDA NmiMode4R1Table,Y               ; F78E  b9 b5 f7
    STA chr_r1_alt                      ; F791  85 59
    LDA #$00                            ; F793  a9 00
    STA chr_r2_alt                      ; F795  85 5a
    LDA #$04                            ; F797  a9 04
    STA BitMask                         ; F799  8d 00 c0
    STA DelayX                          ; F79C  8d 51 f4
    STA $C001                           ; F79F  8d 01 c0
    STA $E001                           ; F7A2  8d 01 e0
    LDA #$04                            ; F7A5  a9 04
    JMP SetIrqHandler                   ; F7A7  4c 24 f4
loc_31_F7AA:
    LDA ppumask_shadow                  ; F7AA  a5 01
    AND #$F7                            ; F7AC  29 f7
    STA a:ppumask_shadow                ; F7AE  8d 01 00
    STA $E000                           ; F7B1  8d 00 e0
    RTS                                 ; F7B4  60
NmiMode4R1Table:  ; s64: 16 values indexed by $58 for irq_mode 4; s65: really the H4 row count ($59) not a CHR bank
    .db $00,$1C,$1C,$1C,$1C,$1C,$1C,$1C,$17,$15,$12,$11,$11,$0E,$0E,$0D; 31:F7B5
NmiMode5:  ; s64: irq_mode 5 (cold intro): PpuScrollWrite + ApplyChrPlay; arm IRQ handler 6 at latch $58
    JSR PpuScrollWrite                  ; F7C5  20 6d eb
    JSR ApplyChrPlay                    ; F7C8  20 83 f8
    LDA chr_r0_alt                      ; F7CB  a5 58
    STA BitMask                         ; F7CD  8d 00 c0
    STA $C001                           ; F7D0  8d 01 c0
    STA $E001                           ; F7D3  8d 01 e0
    LDA #$06                            ; F7D6  a9 06
    JMP SetIrqHandler                   ; F7D8  4c 24 f4
NmiMode6:  ; s64: irq_mode 6 (equipment menu): PPUCTRL shadow; scroll 0;0; ApplyChrAlt; arm IRQ handler 7 at latch $77
    LDA ppuctrl_shadow                  ; F7DB  a5 00
    STA PPUCTRL                         ; F7DD  8d 00 20
    LDA #$00                            ; F7E0  a9 00
    STA PPUSCROLL                       ; F7E2  8d 05 20
    STA PPUSCROLL                       ; F7E5  8d 05 20
    JSR ApplyChrAlt                     ; F7E8  20 4f f6
    LDA #$07                            ; F7EB  a9 07
    JSR SetIrqHandler                   ; F7ED  20 24 f4
    LDA #$77                            ; F7F0  a9 77
    STA zp_57                           ; F7F2  85 57
    STA BitMask                         ; F7F4  8d 00 c0
    STA $C001                           ; F7F7  8d 01 c0
    STA $E001                           ; F7FA  8d 01 e0
    RTS                                 ; F7FD  60
SoundUpdate:  ; from NMI F4CF / IRQ F611 (+JMP F6DC F76B F77F); maps 24/25; drains 1 queued id -> SndCommand; R7=25; JSR SndTick; restore banks
    LDY #$06                            ; F7FE  a0 06
    LDX #$18                            ; F800  a2 18
    STY $8000                           ; F802  8c 00 80
    STX $8001                           ; F805  8e 01 80
    INY                                 ; F808  c8
    INX                                 ; F809  e8
    STY $8000                           ; F80A  8c 00 80
    STX $8001                           ; F80D  8e 01 80
    LDA snd_cur_music                   ; F810  ad 02 01
    BPL loc_31_F843                     ; F813  10 2e
    LDA snd_fade_timer                  ; F815  a5 fe
    BPL loc_31_F82B                     ; F817  10 12
    LDA snd_ch_reg0                     ; F819  ad 68 01
    ORA $0169                           ; F81C  0d 69 01
    ORA $016A                           ; F81F  0d 6a 01
    ORA $016B                           ; F822  0d 6b 01
    AND #$0F                            ; F825  29 0f
    BNE loc_31_F843                     ; F827  d0 1a
    BEQ loc_31_F832                     ; F829  f0 07
loc_31_F82B:
    LDA snd_music_id                    ; F82B  ad 11 01
    CMP #$FF                            ; F82E  c9 ff
    BNE loc_31_F843                     ; F830  d0 11
loc_31_F832:
    LDA snd_cur_music                   ; F832  ad 02 01
    AND #$7F                            ; F835  29 7f
    STA snd_cur_music                   ; F837  8d 02 01
    STA snd_cmd                         ; F83A  8d 10 01
    JSR SndCommand                      ; F83D  20 48 80
    JMP loc_31_F866                     ; F840  4c 66 f8
loc_31_F843:
    LDA snd_queue_lock                  ; F843  ad 01 01
    BNE loc_31_F866                     ; F846  d0 1e
    LDY snd_queue_count                 ; F848  ac 00 01
    BEQ loc_31_F866                     ; F84B  f0 19
    LDA snd_queue                       ; F84D  ad 03 01
    STA snd_cmd                         ; F850  8d 10 01
    JSR SndCommand                      ; F853  20 48 80
    LDY #$00                            ; F856  a0 00
loc_31_F858:
    LDA $0104,Y                         ; F858  b9 04 01
    STA snd_queue,Y                     ; F85B  99 03 01
    INY                                 ; F85E  c8
    CPY #$06                            ; F85F  c0 06
    BNE loc_31_F858                     ; F861  d0 f5
    DEC snd_queue_count                 ; F863  ce 00 01
loc_31_F866:
    LDA #$07                            ; F866  a9 07
    STA $8000                           ; F868  8d 00 80
    LDA #$19                            ; F86B  a9 19
    STA $8001                           ; F86D  8d 01 80
    JSR SndTick                         ; F870  20 86 81
    LDX mmc3_banksel_shadow             ; F873  a6 50
    LDA r7_shadow                       ; F875  a5 6f
    JSR SetR7                           ; F877  20 27 c4
    LDA cur_bank16_x2                   ; F87A  a5 6e
    JSR SetR6                           ; F87C  20 18 c4
    STX $8000                           ; F87F  8e 00 80
    RTS                                 ; F882  60
ApplyChrPlay:  ; write $07F0-$07F5 to MMC3 R0-R5; JSR'd from F779/F785/F7C8
    LDA #$00                            ; F883  a9 00
    STA $8000                           ; F885  8d 00 80
    LDA chr_r0_play                     ; F888  ad f0 07
    STA $8001                           ; F88B  8d 01 80
    LDA #$01                            ; F88E  a9 01
    STA $8000                           ; F890  8d 00 80
    LDA chr_r1_play                     ; F893  ad f1 07
    STA $8001                           ; F896  8d 01 80
    LDA #$02                            ; F899  a9 02
    STA $8000                           ; F89B  8d 00 80
    LDA chr_r2_play                     ; F89E  ad f2 07
    STA $8001                           ; F8A1  8d 01 80
    LDA #$03                            ; F8A4  a9 03
    STA $8000                           ; F8A6  8d 00 80
    LDA chr_r3_play                     ; F8A9  ad f3 07
    STA $8001                           ; F8AC  8d 01 80
    LDA #$04                            ; F8AF  a9 04
    STA $8000                           ; F8B1  8d 00 80
    LDA chr_r4_play                     ; F8B4  ad f4 07
    STA $8001                           ; F8B7  8d 01 80
    LDA #$05                            ; F8BA  a9 05
    STA $8000                           ; F8BC  8d 00 80
    LDA chr_r5_play                     ; F8BF  ad f5 07
    STA $8001                           ; F8C2  8d 01 80
    LDA mmc3_banksel_shadow             ; F8C5  a5 50
    STA $8000                           ; F8C7  8d 00 80
    RTS                                 ; F8CA  60
PaletteUpload:  ; NMI (31:F40F X=0): skip if $51 b7; PPUMASK=0; $3F00 <- pal_buf $6140-$615F (backdrop forced into every group); PPUADDR $3F00 then $0000
    BIT irq_mode                        ; F8CB  24 51
    BPL loc_31_F8D0                     ; F8CD  10 01
    RTS                                 ; F8CF  60
loc_31_F8D0:
    LDA #$00                            ; F8D0  a9 00
    STA PPUMASK                         ; F8D2  8d 01 20
    LDA ppuctrl_shadow                  ; F8D5  a5 00
    AND #$FB                            ; F8D7  29 fb
    STA PPUCTRL                         ; F8D9  8d 00 20
    LDA PPUSTATUS                       ; F8DC  ad 02 20
    LDA #$3F                            ; F8DF  a9 3f
    STA PPUADDR                         ; F8E1  8d 06 20
    LDA #$00                            ; F8E4  a9 00
    STA PPUADDR                         ; F8E6  8d 06 20
    LDA pal_buf,X                       ; F8E9  bd 40 61
    STA PPUDATA                         ; F8EC  8d 07 20
    LDA $6141,X                         ; F8EF  bd 41 61
    STA PPUDATA                         ; F8F2  8d 07 20
    LDA $6142,X                         ; F8F5  bd 42 61
    STA PPUDATA                         ; F8F8  8d 07 20
    LDA $6143,X                         ; F8FB  bd 43 61
    STA PPUDATA                         ; F8FE  8d 07 20
    LDA pal_buf,X                       ; F901  bd 40 61
    STA PPUDATA                         ; F904  8d 07 20
    LDA $6145,X                         ; F907  bd 45 61
    STA PPUDATA                         ; F90A  8d 07 20
    LDA $6146,X                         ; F90D  bd 46 61
    STA PPUDATA                         ; F910  8d 07 20
    LDA $6147,X                         ; F913  bd 47 61
    STA PPUDATA                         ; F916  8d 07 20
    LDA pal_buf,X                       ; F919  bd 40 61
    STA PPUDATA                         ; F91C  8d 07 20
    LDA $6149,X                         ; F91F  bd 49 61
    STA PPUDATA                         ; F922  8d 07 20
    LDA $614A,X                         ; F925  bd 4a 61
    STA PPUDATA                         ; F928  8d 07 20
    LDA $614B,X                         ; F92B  bd 4b 61
    STA PPUDATA                         ; F92E  8d 07 20
    LDA pal_buf,X                       ; F931  bd 40 61
    STA PPUDATA                         ; F934  8d 07 20
    LDA $614D,X                         ; F937  bd 4d 61
    STA PPUDATA                         ; F93A  8d 07 20
    LDA $614E,X                         ; F93D  bd 4e 61
    STA PPUDATA                         ; F940  8d 07 20
    LDA $614F,X                         ; F943  bd 4f 61
    STA PPUDATA                         ; F946  8d 07 20
    LDA pal_buf,X                       ; F949  bd 40 61
    STA PPUDATA                         ; F94C  8d 07 20
    LDA $6151,X                         ; F94F  bd 51 61
    STA PPUDATA                         ; F952  8d 07 20
    LDA $6152,X                         ; F955  bd 52 61
    STA PPUDATA                         ; F958  8d 07 20
    LDA $6153,X                         ; F95B  bd 53 61
    STA PPUDATA                         ; F95E  8d 07 20
    LDA pal_buf,X                       ; F961  bd 40 61
    STA PPUDATA                         ; F964  8d 07 20
    LDA $6155,X                         ; F967  bd 55 61
    STA PPUDATA                         ; F96A  8d 07 20
    LDA $6156,X                         ; F96D  bd 56 61
    STA PPUDATA                         ; F970  8d 07 20
    LDA $6157,X                         ; F973  bd 57 61
    STA PPUDATA                         ; F976  8d 07 20
    LDA pal_buf,X                       ; F979  bd 40 61
    STA PPUDATA                         ; F97C  8d 07 20
    LDA $6159,X                         ; F97F  bd 59 61
    STA PPUDATA                         ; F982  8d 07 20
    LDA $615A,X                         ; F985  bd 5a 61
    STA PPUDATA                         ; F988  8d 07 20
    LDA $615B,X                         ; F98B  bd 5b 61
    STA PPUDATA                         ; F98E  8d 07 20
    LDA pal_buf,X                       ; F991  bd 40 61
    STA PPUDATA                         ; F994  8d 07 20
    LDA $615D,X                         ; F997  bd 5d 61
    STA PPUDATA                         ; F99A  8d 07 20
    LDA $615E,X                         ; F99D  bd 5e 61
    STA PPUDATA                         ; F9A0  8d 07 20
    LDA $615F,X                         ; F9A3  bd 5f 61
    STA PPUDATA                         ; F9A6  8d 07 20
    LDA #$3F                            ; F9A9  a9 3f
    STA PPUADDR                         ; F9AB  8d 06 20
    LDA #$00                            ; F9AE  a9 00
    STA PPUADDR                         ; F9B0  8d 06 20
    STA PPUADDR                         ; F9B3  8d 06 20
    STA PPUADDR                         ; F9B6  8d 06 20
    RTS                                 ; F9B9  60
StaleText31_F9BA:  ; dead filler to F9FF: copy of 23:B9BA text
    .db $AD,$69,$6E,$63,$72,$65,$61,$73,$86,$2E,$00,$01,$02,$09,$05,$59; 31:F9BA
    .db $6F,$75,$72,$20,$05,$04,$05,$03,$02,$09,$07,$AD,$D9,$6C,$6F,$77; 31:F9CA
    .db $2E,$00,$01,$02,$20,$F7,$A4,$D9,$6D,$61,$6E,$79,$20,$69,$74,$65; 31:F9DA
    .db $6D,$73,$21,$00,$01,$02,$09,$04,$48,$65,$27,$73,$20,$61,$6C,$72; 31:F9EA
    .db $65,$61,$64,$79,$20,$64         ; 31:F9FA
SndDmcSample:  ; the only DMC sample: 993 bytes $FA00-$FDE0 ($4012=$E8 $4013=$3E); drum kit = this sample at different rates
    .db $FE,$9C,$14,$80,$3F,$F8,$65,$B3,$E4,$49,$7D,$C8,$B4,$49,$4D,$8F; 31:FA00
    .db $70,$72,$8E,$E3,$39,$33,$E2,$CC,$8F,$9D,$13,$A6,$F5,$7F,$C3,$E1; 31:FA10
    .db $82,$F3,$43,$F0,$7C,$1C,$8C,$BD,$03,$00,$18,$00,$02,$3C,$9B,$99; 31:FA20
    .db $C7,$81,$87,$59,$E4,$AC,$F4,$FF,$7F,$E1,$C1,$71,$08,$3F,$4C,$FC; 31:FA30
    .db $86,$FB,$1D,$60,$8E,$34,$19,$B9,$49,$9B,$46,$A6,$34,$31,$09,$C9; 31:FA40
    .db $6C,$32,$B7,$8C,$79,$4B,$B3,$CC,$A4,$49,$72,$E4,$69,$06,$0F,$C6; 31:FA50
    .db $60,$06,$E6,$8D,$1D,$3E,$E1,$9B,$D9,$86,$3C,$0F,$1E,$16,$C9,$6C; 31:FA60
    .db $C2,$39,$70,$FA,$30,$C9,$CC,$16,$1C,$67,$E1,$92,$65,$E2,$EE,$6D; 31:FA70
    .db $C2,$F1,$D8,$19,$72,$61,$B6,$27,$A2,$8D,$C9,$C0,$32,$39,$C7,$46; 31:FA80
    .db $E4,$63,$FE,$2C,$DC,$CE,$B0,$20,$79,$1E,$1C,$1B,$1C,$A6,$9B,$67; 31:FA90
    .db $8C,$E6,$C9,$CB,$64,$9E,$B1,$2F,$6A,$DA,$79,$26,$3E,$3E,$C7,$BC; 31:FAA0
    .db $C9,$03,$76,$78,$92,$9F,$8C,$36,$CC,$B2,$F0,$9B,$17,$67,$26,$19; 31:FAB0
    .db $C6,$36,$FE,$E3,$4C,$CC,$3D,$33,$C7,$63,$E9,$FC,$F1,$D9,$D9,$20; 31:FAC0
    .db $CF,$6C,$CC,$F6,$40,$6E,$96,$76,$23,$4B,$92,$B7,$61,$BC,$73,$E6; 31:FAD0
    .db $61,$8A,$E6,$30,$93,$39,$93,$F1,$F8,$3C,$F8,$C1,$27,$BF,$CB,$79; 31:FAE0
    .db $E4,$C9,$4E,$86,$19,$07,$9E,$4D,$EE,$30,$1F,$9B,$CC,$F2,$9C,$CB; 31:FAF0
    .db $3C,$C1,$E7,$61,$E3,$9C,$27,$19,$96,$DD,$3C,$3E,$C9,$2C,$7F,$A3; 31:FB00
    .db $39,$33,$F6,$F3,$33,$3B,$20,$DB,$DC,$E1,$1B,$8C,$CB,$36,$99,$8F; 31:FB10
    .db $99,$81,$3D,$6E,$53,$CE,$C2,$19,$7A,$19,$64,$62,$33,$8E,$89,$87; 31:FB20
    .db $C7,$66,$32,$EB,$68,$66,$27,$0C,$73,$71,$B9,$39,$86,$BC,$9B,$E1; 31:FB30
    .db $BC,$43,$E6,$9B,$99,$CF,$38,$8C,$C7,$33,$47,$F8,$C3,$6F,$CC,$C7; 31:FB40
    .db $70,$83,$7D,$E1,$B0,$67,$4E,$CE,$76,$DA,$DC,$3E,$F8,$13,$2F,$09; 31:FB50
    .db $BB,$E7,$76,$36,$D9,$8F,$FF,$96,$D0,$B9,$F3,$65,$3E,$71,$06,$33; 31:FB60
    .db $DC,$CD,$6C,$E6,$19,$9B,$F6,$31,$73,$C7,$8D,$99,$6C,$3C,$68,$C7; 31:FB70
    .db $3D,$1D,$C7,$B8,$F1,$B8,$7B,$D8,$CE,$78,$E6,$CC,$67,$4C,$F3,$B6; 31:FB80
    .db $31,$C9,$0E,$66,$63,$31,$E7,$D9,$F9,$6C,$C2,$7C,$10,$9F,$33,$23; 31:FB90
    .db $D1,$CD,$E1,$CC,$C4,$B9,$9B,$67,$76,$98,$73,$C6,$67,$06,$99,$79; 31:FBA0
    .db $C6,$1C,$87,$E3,$F1,$CC,$86,$B3,$30,$67,$66,$1B,$63,$44,$0E,$3B; 31:FBB0
    .db $B6,$33,$9E,$23,$B3,$6B,$86,$CF,$F0,$B1,$84,$F9,$CC,$4E,$8C,$EB; 31:FBC0
    .db $E6,$73,$36,$36,$F2,$C7,$0F,$F6,$9F,$F9,$C9,$0F,$8B,$0F,$67,$3E; 31:FBD0
    .db $FE,$13,$F2,$8E,$EC,$E7,$F2,$EF,$62,$13,$D9,$3C,$E3,$D9,$10,$CF; 31:FBE0
    .db $60,$3E,$39,$FC,$00,$8E,$CC,$9D,$CC,$07,$F3,$C1,$67,$BF,$3F,$DE; 31:FBF0
    .db $F3,$73,$83,$9F,$DC,$18,$3E,$BF,$B3,$99,$C1,$33,$66,$30,$3F,$E3; 31:FC00
    .db $F0,$E3,$31,$B8,$FC,$E7,$6E,$32,$93,$8F,$49,$8E,$73,$C2,$8C,$39; 31:FC10
    .db $3C,$1F,$F1,$73,$EF,$97,$CD,$CF,$63,$CE,$C6,$CE,$F6,$67,$30,$1E; 31:FC20
    .db $C6,$F5,$F1,$39,$78,$6E,$B8,$B9,$E1,$C9,$C6,$DC,$FC,$37,$4C,$6E; 31:FC30
    .db $26,$FB,$38,$37,$98,$33,$0F,$C7,$3F,$33,$9E,$87,$8F,$DD,$03,$3F; 31:FC40
    .db $66,$2C,$F9,$C0,$BD,$43,$32,$3B,$99,$3F,$B9,$5F,$C6,$3F,$11,$8F; 31:FC50
    .db $99,$0E,$8F,$C3,$EC,$C6,$27,$9F,$83,$E7,$33,$63,$E6,$EC,$B9,$E3; 31:FC60
    .db $B3,$23,$5F,$CE,$67,$26,$E3,$1F,$76,$06,$2E,$FB,$3B,$3E,$78,$BD; 31:FC70
    .db $9B,$D9,$7C,$33,$E3,$C3,$F0,$38,$32,$0C,$0F,$CF,$D8,$98,$99,$19; 31:FC80
    .db $D9,$07,$27,$1C,$E7,$67,$1C,$ED,$23,$67,$86,$BB,$23,$13,$97,$7F; 31:FC90
    .db $E3,$7E,$CF,$7F,$CE,$F1,$FC,$E7,$31,$F3,$FE,$20,$3E,$EF,$E1,$1F; 31:FCA0
    .db $67,$A7,$E3,$EE,$CF,$C7,$E4,$1C,$F0,$E0,$C8,$83,$F1,$4F,$DE,$7C; 31:FCB0
    .db $07,$46,$3E,$99,$3C,$98,$9D,$FB,$19,$3C,$F1,$F8,$1D,$0F,$4E,$06; 31:FCC0
    .db $F7,$39,$23,$C7,$CC,$27,$BB,$1F,$83,$47,$FE,$C3,$7E,$DC,$9F,$CC; 31:FCD0
    .db $E4,$9B,$FC,$CC,$CF,$8C,$C3,$8D,$CD,$C5,$3D,$19,$E7,$71,$3B,$32; 31:FCE0
    .db $7F,$70,$78,$1E,$86,$93,$37,$72,$7C,$EC,$36,$FF,$CC,$B3,$C3,$87; 31:FCF0
    .db $6F,$1E,$69,$8E,$87,$07,$FE,$81,$C3,$19,$CB,$47,$FE,$4F,$71,$07; 31:FD00
    .db $EB,$67,$B3,$FB,$6C,$C6,$DC,$27,$76,$41,$FC,$C7,$BE,$FF,$FC,$E0; 31:FD10
    .db $D1,$67,$3C,$DF,$F1,$41,$E7,$F1,$8D,$C7,$D8,$A1,$FB,$BF,$07,$FF; 31:FD20
    .db $9E,$19,$7F,$66,$0E,$1C,$7B,$7C,$FD,$39,$66,$30,$BE,$78,$CC,$3C; 31:FD30
    .db $0E,$7E,$6E,$83,$FE,$8C,$F3,$FF,$7D,$66,$F8,$E8,$7F,$66,$06,$8F; 31:FD40
    .db $77,$74,$7F,$34,$FC,$FD,$DE,$97,$1F,$0F,$36,$2F,$8E,$3F,$88,$CF; 31:FD50
    .db $E7,$3F,$3E,$9A,$99,$F8,$C1,$DD,$99,$E1,$F7,$C7,$8D,$D9,$79,$FE; 31:FD60
    .db $70,$46,$C7,$BD,$FD,$EB,$3D,$B8,$F1,$DE,$33,$FF,$CC,$7C,$70,$E0; 31:FD70
    .db $CD,$ED,$E1,$67,$3F,$CA,$8F,$0E,$DF,$3F,$FF,$F8,$01,$B7,$39,$C8; 31:FD80
    .db $B1,$0F,$3C,$38,$FE,$C4,$F4,$66,$98,$8F,$87,$38,$F9,$F2,$7E,$B2; 31:FD90
    .db $1F,$C0,$1F,$F3,$01,$F0,$78,$DF,$CE,$F9,$FC,$C7,$DF,$DD,$9D,$0F; 31:FDA0
    .db $3D,$E7,$C7,$47,$FE,$B1,$1F,$DE,$65,$CC,$1C,$9B,$1F,$50,$FC,$BB; 31:FDB0
    .db $CF,$3B,$98,$0F,$0F,$40,$0E,$48,$7C,$F0,$FE,$E3,$93,$F3,$0F,$9F; 31:FDC0
    .db $33,$C1,$DF,$EB,$7F,$FE,$60,$10,$4F,$CC,$8F,$0E,$FE,$EF,$00,$00; 31:FDD0
StaleSave31_FDE0:  ; dead filler to FDFF: copy of 23:BDE0 load descriptors + SaveAllowed
    .db $00,$64,$7F,$00,$00,$7E,$80,$64,$FF,$01,$80,$7D,$02,$07,$1C,$00; 31:FDE0
    .db $B0,$7D,$DC,$07,$23,$00,$A6,$6C,$BD,$00,$BF,$0A,$60,$F8,$A9,$00; 31:FDF0
ShopDoorOpenStub:  ; s64: bare RTS called by ShopWalkIn 30:DA0C; the real body FE01 (irq_mode 4; $58 $0B->6) is disabled
    RTS                                 ; FE00  60
ShopDoorOpen:  ; s64: dead (no callers; stubbed by FE00 RTS): irq_mode=4; $58 = $0B down to 6; 4 frames per step
    LDA #$04                            ; FE01  a9 04
    STA irq_mode                        ; FE03  85 51
    LDA #$0B                            ; FE05  a9 0b
    STA chr_r0_alt                      ; FE07  85 58
loc_31_FE09:
    LDY #$04                            ; FE09  a0 04
    JSR sub_30_C176                     ; FE0B  20 76 c1
    DEC chr_r0_alt                      ; FE0E  c6 58
    LDA chr_r0_alt                      ; FE10  a5 58
    CMP #$06                            ; FE12  c9 06
    BNE loc_31_FE09                     ; FE14  d0 f3
    RTS                                 ; FE16  60
ShopDoorCloseStub:  ; s64: bare RTS called by ShopWalkOut 30:DA72; the real body FE18 (irq_mode 4; $58 6->1) is disabled
    RTS                                 ; FE17  60
ShopDoorClose:  ; s64: dead (no callers; stubbed by FE17 RTS): irq_mode=4; $58 = 6 down to 1; 4 frames per step
    LDA #$04                            ; FE18  a9 04
    STA irq_mode                        ; FE1A  85 51
    LDA #$06                            ; FE1C  a9 06
    STA chr_r0_alt                      ; FE1E  85 58
loc_31_FE20:
    LDY #$04                            ; FE20  a0 04
    JSR sub_30_C176                     ; FE22  20 76 c1
    DEC chr_r0_alt                      ; FE25  c6 58
    LDA chr_r0_alt                      ; FE27  a5 58
    CMP #$01                            ; FE29  c9 01
    BNE loc_31_FE20                     ; FE2B  d0 f3
    RTS                                 ; FE2D  60
StalePad31_FE2E:  ; dead $FF pad to FE7F (8 stale bytes at FE78)
    .db $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF; 31:FE2E
    .db $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF; 31:FE3E
    .db $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF; 31:FE4E
    .db $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF; 31:FE5E
    .db $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$1E,$00,$28,$00,$3C,$00; 31:FE6E
    .db $32,$00                         ; 31:FE7E
ReadPads:  ; per-frame gameplay pad read (pads X=1/0): $43 prev $4B new $49 dir $4D act (A/B hold counters $45/$47); was ColdInitSub2
    LDX #$01                            ; FE80  a2 01
loc_31_FE82:
    JSR ReadPadStable                   ; FE82  20 17 ff
    LDA pad_raw                         ; FE85  a5 4f
    AND #$0F                            ; FE87  29 0f
    TAY                                 ; FE89  a8
    LDA PadDirTable,Y                   ; FE8A  b9 d0 fe
    STA pad_dir,X                       ; FE8D  95 49
    LDA pad_prev,X                      ; FE8F  b5 43
    EOR #$FF                            ; FE91  49 ff
    AND pad_raw                         ; FE93  25 4f
    STA pad_new,X                       ; FE95  95 4b
    LDA pad_raw                         ; FE97  a5 4f
    STA pad_prev,X                      ; FE99  95 43
    LDY #$00                            ; FE9B  a0 00
    LDA pad_new,X                       ; FE9D  b5 4b
    BPL loc_31_FEA3                     ; FE9F  10 02
    STY pad_hold_a,X                    ; FEA1  94 45
loc_31_FEA3:
    ASL A                               ; FEA3  0a
    BPL loc_31_FEA8                     ; FEA4  10 02
    STY pad_hold_b,X                    ; FEA6  94 47
loc_31_FEA8:
    CLC                                 ; FEA8  18
    BIT pad_raw                         ; FEA9  24 4f
    BPL loc_31_FEB8                     ; FEAB  10 0b
    INC pad_hold_a,X                    ; FEAD  f6 45
    LDA pad_hold_a,X                    ; FEAF  b5 45
    AND #$1F                            ; FEB1  29 1f
    BNE loc_31_FEB8                     ; FEB3  d0 03
    DEC pad_hold_a,X                    ; FEB5  d6 45
    SEC                                 ; FEB7  38
loc_31_FEB8:
    BVC loc_31_FEC6                     ; FEB8  50 0c
    INC pad_hold_b,X                    ; FEBA  f6 47
    LDA pad_hold_b,X                    ; FEBC  b5 47
    AND #$1F                            ; FEBE  29 1f
    BNE loc_31_FEC6                     ; FEC0  d0 04
    LDY #$80                            ; FEC2  a0 80
    BNE loc_31_FEC6                     ; FEC4  d0 00
loc_31_FEC6:
    TYA                                 ; FEC6  98
    ROR A                               ; FEC7  6a
    ORA pad_new,X                       ; FEC8  15 4b
    STA pad_act,X                       ; FECA  95 4d
    DEX                                 ; FECC  ca
    BPL loc_31_FE82                     ; FECD  10 b3
    RTS                                 ; FECF  60
PadDirTable:  ; u8[16] d-pad nibble (U8 D4 L2 R1) -> direction 0-7 clockwise from up; FF = none/opposed
    .db $FF,$02,$06,$FF,$04,$03,$05,$04,$00,$01,$07,$00,$FF,$02,$06,$FF; 31:FED0
ReadPadsMenu:  ; menu pad read: $4B new presses + d-pad auto-repeat (delay $0C then every 3 frames via $45)
    LDX #$01                            ; FEE0  a2 01
loc_31_FEE2:
    JSR ReadPadStable                   ; FEE2  20 17 ff
    LDA pad_prev,X                      ; FEE5  b5 43
    EOR #$FF                            ; FEE7  49 ff
    AND pad_raw                         ; FEE9  25 4f
    STA pad_new,X                       ; FEEB  95 4b
    LDA pad_new,X                       ; FEED  b5 4b
    AND #$0F                            ; FEEF  29 0f
    BEQ loc_31_FEF7                     ; FEF1  f0 04
    LDA #$0C                            ; FEF3  a9 0c
    STA pad_hold_a,X                    ; FEF5  95 45
loc_31_FEF7:
    LDA pad_prev,X                      ; FEF7  b5 43
    EOR pad_raw                         ; FEF9  45 4f
    AND #$0F                            ; FEFB  29 0f
    BNE loc_31_FF0F                     ; FEFD  d0 10
    DEC pad_hold_a,X                    ; FEFF  d6 45
    BNE loc_31_FF0F                     ; FF01  d0 0c
    LDA #$03                            ; FF03  a9 03
    STA pad_hold_a,X                    ; FF05  95 45
    LDA pad_raw                         ; FF07  a5 4f
    AND #$0F                            ; FF09  29 0f
    ORA pad_new,X                       ; FF0B  15 4b
    STA pad_new,X                       ; FF0D  95 4b
loc_31_FF0F:
    LDA pad_raw                         ; FF0F  a5 4f
    STA pad_prev,X                      ; FF11  95 43
    DEX                                 ; FF13  ca
    BPL loc_31_FEE2                     ; FF14  10 cc
    RTS                                 ; FF16  60
ReadPadStable:  ; read pad X until two reads agree -> $4F
    JSR ReadPadRaw                      ; FF17  20 28 ff
    LDA pad_raw                         ; FF1A  a5 4f
    STA pad_dir,X                       ; FF1C  95 49
    JSR ReadPadRaw                      ; FF1E  20 28 ff
    LDA pad_raw                         ; FF21  a5 4f
    CMP pad_dir,X                       ; FF23  d5 49
    BNE ReadPadStable                   ; FF25  d0 f0
    RTS                                 ; FF27  60
ReadPadRaw:  ; strobe $4016 and shift 8 bits of JOY1+X into $4F (A=b7 B=b6 Sel=b5 Start=b4 U D L R)
    LDY #$01                            ; FF28  a0 01
    STY JOY1                            ; FF2A  8c 16 40
    DEY                                 ; FF2D  88
    STY JOY1                            ; FF2E  8c 16 40
    STY pad_raw                         ; FF31  84 4f
    LDY #$07                            ; FF33  a0 07
    CLC                                 ; FF35  18
loc_31_FF36:
    LDA JOY1,X                          ; FF36  bd 16 40
    AND #$03                            ; FF39  29 03
    BEQ loc_31_FF3E                     ; FF3B  f0 01
    SEC                                 ; FF3D  38
loc_31_FF3E:
    ROL pad_raw                         ; FF3E  26 4f
    DEY                                 ; FF40  88
    BPL loc_31_FF36                     ; FF41  10 f3
    RTS                                 ; FF43  60
    .db $00,$00,$00,$81,$00,$00,$00,$00,$00,$00,$00,$00,$81,$00,$00,$00; 31:FF44
    .db $00,$00,$00,$00,$81,$00,$00,$00,$00,$00,$80,$00,$00,$81,$81,$00; 31:FF54
    .db $00,$81,$00,$00,$00,$00,$00,$00,$00,$00,$80,$00,$81,$81,$81,$81; 31:FF64
    .db $00,$00,$00,$00,$81,$00,$00,$00,$00,$00,$00,$00; 31:FF74
FarCall_C25D:  ; push $6E/$6F; JSR $C25D; JMP RestoreBanks
    LDA cur_bank16_x2                   ; FF80  a5 6e
    PHA                                 ; FF82  48
    LDA r7_shadow                       ; FF83  a5 6f
    PHA                                 ; FF85  48
    JSR ObjInit                         ; FF86  20 5d c2
    JMP RestoreBanks                    ; FF89  4c db ff
FarCall_B14_800E:  ; push $6E/$6F; SwitchBank16(14); JSR 28:$800E; JMP RestoreBanks
    LDA cur_bank16_x2                   ; FF8C  a5 6e
    PHA                                 ; FF8E  48
    LDA r7_shadow                       ; FF8F  a5 6f
    PHA                                 ; FF91  48
    LDA #$0E                            ; FF92  a9 0e
    JSR SwitchBank16                    ; FF94  20 0e c4
    JSR ObjSpriteDraw                   ; FF97  20 0e 80
    JMP RestoreBanks                    ; FF9A  4c db ff
FarCall_EB91:  ; push $6E/$6F; JSR $EB91; JMP RestoreBanks
    LDA cur_bank16_x2                   ; FF9D  a5 6e
    PHA                                 ; FF9F  48
    LDA r7_shadow                       ; FFA0  a5 6f
    PHA                                 ; FFA2  48
    JSR BgRowRender                     ; FFA3  20 91 eb
    JMP RestoreBanks                    ; FFA6  4c db ff
FarCall_B26_8E46:  ; A->$10; push $6E/$6F; SetR6($1A); JSR 26:$8E46 with A; JMP RestoreBanks
    STA jmp_trampoline                  ; FFA9  85 10
    LDA cur_bank16_x2                   ; FFAB  a5 6e
    PHA                                 ; FFAD  48
    LDA r7_shadow                       ; FFAE  a5 6f
    PHA                                 ; FFB0  48
    LDA #$1A                            ; FFB1  a9 1a
    JSR SetR6                           ; FFB3  20 18 c4
    LDA jmp_trampoline                  ; FFB6  a5 10
    JSR HudRedraw                       ; FFB8  20 46 8e
    JMP RestoreBanks                    ; FFBB  4c db ff
FarCall_B26_8CC0:  ; push $6E/$6F; SetR6($1A); JSR 26:$8CC0; JMP RestoreBanks
    LDA cur_bank16_x2                   ; FFBE  a5 6e
    PHA                                 ; FFC0  48
    LDA r7_shadow                       ; FFC1  a5 6f
    PHA                                 ; FFC3  48
    LDA #$1A                            ; FFC4  a9 1a
    JSR SetR6                           ; FFC6  20 18 c4
    JSR HudHpBar                        ; FFC9  20 c0 8c
    JMP RestoreBanks                    ; FFCC  4c db ff
FarCall_D354:  ; push $6E/$6F; JSR $D354; JMP RestoreBanks
    LDA cur_bank16_x2                   ; FFCF  a5 6e
    PHA                                 ; FFD1  48
    LDA r7_shadow                       ; FFD2  a5 6f
    PHA                                 ; FFD4  48
    JSR ShowMessage                     ; FFD5  20 54 d3
    JMP RestoreBanks                    ; FFD8  4c db ff
RestoreBanks:  ; PLA->SetR7; PLA->JMP SetR6 (pairs with push $6E;$6F)
    PLA                                 ; FFDB  68
    JSR SetR7                           ; FFDC  20 27 c4
    PLA                                 ; FFDF  68
    JMP SetR6                           ; FFE0  4c 18 c4
    .db $81,$81,$81,$00,$00,$81,$81,$00,$00,$81,$00,$00,$81,$00,$81,$00; 31:FFE3
    .db $81,$81,$00,$00,$00,$00,$00     ; 31:FFF3
Vectors:  ; NMI/RESET/IRQ
    .dw NMI                             ; 31:FFFA
    .dw Reset                           ; 31:FFFC
    .dw IRQ                             ; 31:FFFE
