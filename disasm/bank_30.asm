; re-c bank 30  file 0x3C010  CPU $C000-$DFFF  (tools/disasm_all.py)

ppuctrl_shadow = $00
ppumask_shadow = $01
scroll_shadow0 = $02
cam_x_hi = $03
cam_y_lo = $04
cam_y_hi = $05
tmp_08 = $08
frame_ready = $09
tmp_0A = $0A
tmp_0B = $0B
tmp_0C = $0C
jmp_trampoline = $10
tmp_11 = $11
tmp_12 = $12
tmp_13 = $13
tmp_17 = $17
tmp_1F = $1F
tmp_20 = $20
tmp_21 = $21
tmp_22 = $22
tmp_23 = $23
tmp_24 = $24
tmp_25 = $25
tmp_26 = $26
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
main_state = $40
sub_state = $41
pad_prev = $43
pad_new = $4B
zp_4C = $4C
pad_act = $4D
mmc3_banksel_shadow = $50
irq_mode = $51
irq_handler_idx = $56
chr_r0_alt = $58
chr_r1_alt = $59
chr_r2_alt = $5A
chr_r3_alt = $5B
chr_r4_alt = $5C
chr_r5_alt = $5D
nmi_disable = $60
area_id = $6C
area_entry = $6D
cur_bank16_x2 = $6E
r7_shadow = $6F
obj_x_lo = $70
obj_x_lo_s01 = $71
zp_8E = $8E
obj_x_hi = $90
obj_y_lo = $B0
obj_y_lo_s01 = $B1
obj_y_lo_s0D = $BD
obj_y_hi = $D0
snd_queue_count = $0100
snd_queue_lock = $0101
snd_cur_music = $0102
snd_queue = $0103
snd_saved_y = $010F
snd_sfx_priority = $0112
oam_y = $0200
oam_tile = $0201
oam_attr = $0202
oam_x = $0203
obj_meta_id = $0300
obj_meta_id_s01 = $0301
obj_meta_id_s0D = $030D
obj_tile_ofs = $0320
obj_tile_ofs_s01 = $0321
obj_tile_ofs_s1E = $033E
obj_flags2 = $0340
obj_meta_dir = $0360
obj_flags = $0380
obj_flags_s01 = $0381
obj_coll = $03A0
obj_coll_s0D = $03AD
player_hp_max = $03C0
player_hp = $03C1
obj_attack = $03E0
obj_defense = $0400
obj_hitflags = $0420
player_level = $0421
obj_shot_id = $0440
obj_bg_mask = $0460
obj_timer = $0480
obj_active = $04A0
obj_active_s04 = $04A4
obj_active_s0D = $04AD
obj_active_s0E = $04AE
obj_active_s1E = $04BE
obj_active_s1F = $04BF
obj_death_type = $04C0
obj_anim = $04E0
obj_anim_s01 = $04E1
obj_anim_s0D = $04ED
obj_anim_s0E = $04EE
obj_elem_drop = $0500
obj_xp_value = $0520
obj_spawn_b2 = $0540
obj_spawn_param = $0560
obj_spawn_param_s01 = $0561
obj_overlay_id = $0580
obj_overlay_id_s01 = $0581
obj_freeze = $05A0
obj_screen_x = $05C0
obj_screen_y = $05E0
obj_phase = $0600
obj_phase_s0D = $060D
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
gold = $0702
gold_hi = $0703
xp = $0704
xp_hi = $0705
xp_next = $0706
xp_next_hi = $0707
mp_cur = $0708
mp_max = $0709
obj1_flash = $0710
equip_sword = $0711
magic_sel = $0712
equip_armor = $0713
equip_shield = $0714
use_item_sel = $0715
equip_accessory = $0716
equip_ball = $0718
sword_power = $0719
invuln_timer = $071A
opel_revive = $0743
ram_0744 = $0744
ram_0780 = $0780
boss_cam_lock = $07D7
ram_07DC = $07DC
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
chr_r2_play = $07F2
chr_r3_play = $07F3
chr_r4_play = $07F4
chr_r5_play = $07F5
obj_draw_rot = $07FF
PPUCTRL = $2000
PPUADDR = $2006
PPUDATA = $2007
wram_buf = $6000
wram_6001 = $6001
wram_6002 = $6002
wram_6003 = $6003
wram_6004 = $6004
wram_6005 = $6005
wram_6006 = $6006
wram_6007 = $6007
wram_6021 = $6021
wram_6060 = $6060
attr_shadow = $6100
pal_buf = $6140
wram_6150 = $6150
ppu_queue = $6200
ppu_queue_1 = $6201
ppu_queue_2 = $6202
ppu_queue_3 = $6203
visited_map = $62F0
inv_select = $6428
story_flags = $6480
screen_flags = $64C0
npc_state = $6580
menu_save_state = $7000
sub_28_8000 = $8000
NpcScript = $8000
sub_16_8000 = $8000
ObjSpriteDraw = $800E
ObjHideUnusedOam = $80A5
ItemHook2 = $80E3
Telepathy = $816F
ItemGetHook = $826F
AreaHeaderTable = $8300
ItemUseHook = $831E
AreaHeaderTableHi = $8400
TextWindowOpen = $8500
TextPrintBox = $8528
SwordAttackTable = $8BD2
PaletteBuild = $8C0E
Bank26_FadeA = $8C3D
Bank26_FadeB = $8C49
HudHpBar = $8CC0
HudRedraw = $8E46
ObjCollideAll = $8F6E
HitSetFlag = $9011
WakeCutscene = $9400
ShopEntryJumps = $9500
sub_16_9503 = $9503
sub_16_9506 = $9506
sub_16_9509 = $9509
SpawnShot = $972D
TempAddOffset = $9824
TempToObjPos = $9897
ObjPosToTemp = $98A8
ObjMoveSlide = $98D7
ObjBgProbe = $9A30
SpendMp = $9AE8
PlayerUpdate = $9B45
MainState2Init = $9FCC
EndingInit = $A000
MainState3 = $A015
EndingTick = $A100
ObjAiTable = $A314
ObjTypeTable = $AC00
ObjTypeTableHi = $AD00
PlayerDeath = $B900
sub_19_BA65 = $BA65
SubstCureMagic = $BB39
SubstFormRevert = $BB9D
SaveJumpTable = $BC00
SwordRaisePose = $BC04
sub_23_BC06 = $BC06
PauseScreen = $BC40
SubstMesiaReunion = $BC6B
PauseWait = $BDDB
SubstFinalSceneReset = $BDF2
ItemRemapTable = $D40C
sub_31_E038 = $E038
sub_31_E077 = $E077
SubstPlayTail = $E0B8
AreaLoadA = $E144
AreaLoadB = $E148
AreaEnter = $E3E9
CameraTick = $E8F6
ExitLookup = $EB09
BgRowRender = $EB91
PlayerStatusTick = $EF55
ShopDoorOpenStub = $FE00
ShopDoorCloseStub = $FE17
ReadPads = $FE80
ReadPadsMenu = $FEE0
FarCall_C25D = $FF80
FarCall_B14_800E = $FF8C

    .org $C000
BitMask:  ; 01 02 04 08 10 20 40 80
    .db $01,$02,$04,$08,$10,$20,$40,$80 ; 30:C000
sub_30_C008:
    LDA cur_bank16_x2                   ; C008  a5 6e
    PHA                                 ; C00A  48
    LDA #$1A                            ; C00B  a9 1a
    JSR SetR6                           ; C00D  20 18 c4
    LDA player_level                    ; C010  ad 21 04
    LDY equip_accessory                 ; C013  ac 16 07
    CPY #$0E                            ; C016  c0 0e
    BNE loc_30_C01B                     ; C018  d0 01
    ASL A                               ; C01A  0a
loc_30_C01B:
    LDY equip_sword                     ; C01B  ac 11 07
    CLC                                 ; C01E  18
    ADC SwordAttackTable,Y              ; C01F  79 d2 8b
    STA $03E1                           ; C022  8d e1 03
    LDA player_level                    ; C025  ad 21 04
    LDY equip_accessory                 ; C028  ac 16 07
    CPY #$10                            ; C02B  c0 10
    BNE loc_30_C030                     ; C02D  d0 01
    ASL A                               ; C02F  0a
loc_30_C030:
    LDY equip_armor                     ; C030  ac 13 07
    CLC                                 ; C033  18
    ADC $8BC0,Y                         ; C034  79 c0 8b
    STA $0401                           ; C037  8d 01 04
    LDA player_level                    ; C03A  ad 21 04
    LDY equip_accessory                 ; C03D  ac 16 07
    CPY #$14                            ; C040  c0 14
    BNE loc_30_C045                     ; C042  d0 01
    ASL A                               ; C044  0a
loc_30_C045:
    LDY equip_shield                    ; C045  ac 14 07
    CLC                                 ; C048  18
    ADC $8BC9,Y                         ; C049  79 c9 8b
    STA obj_defense                     ; C04C  8d 00 04
    PLA                                 ; C04F  68
    JSR SetR6                           ; C050  20 18 c4
    LDY #$00                            ; C053  a0 00
    LDA obj1_flash                      ; C055  ad 10 07
    AND #$0F                            ; C058  29 0f
    CMP #$02                            ; C05A  c9 02
    BNE loc_30_C060                     ; C05C  d0 02
    LDY #$4D                            ; C05E  a0 4d
loc_30_C060:
    STY pal_id_spr0                     ; C060  8c e4 07
    LDA $6485                           ; C063  ad 85 64
    AND #$F0                            ; C066  29 f0
    STA jmp_trampoline                  ; C068  85 10
    LDA obj1_flash                      ; C06A  ad 10 07
    BMI loc_30_C073                     ; C06D  30 04
    LDA #$00                            ; C06F  a9 00
    BEQ loc_30_C07D                     ; C071  f0 0a
loc_30_C073:
    LSR A                               ; C073  4a
    LSR A                               ; C074  4a
    LSR A                               ; C075  4a
    LSR A                               ; C076  4a
    AND #$03                            ; C077  29 03
    TAY                                 ; C079  a8
    LDA $C121,Y                         ; C07A  b9 21 c1
loc_30_C07D:
    ORA jmp_trampoline                  ; C07D  05 10
    STA $6485                           ; C07F  8d 85 64
    LDA obj1_flash                      ; C082  ad 10 07
    BPL loc_30_C08A                     ; C085  10 03
    JMP loc_30_C107                     ; C087  4c 07 c1
loc_30_C08A:
    AND #$0F                            ; C08A  29 0f
    CMP #$04                            ; C08C  c9 04
    BEQ loc_30_C100                     ; C08E  f0 70
    LDA #$00                            ; C090  a9 00
    STA obj_tile_ofs_s01                ; C092  8d 21 03
    LDA obj_flags_s01                   ; C095  ad 81 03
    ORA #$02                            ; C098  09 02
    STA obj_flags_s01                   ; C09A  8d 81 03
    LDA obj_meta_dir                    ; C09D  ad 60 03
    LSR A                               ; C0A0  4a
    AND #$03                            ; C0A1  29 03
    STA jmp_trampoline                  ; C0A3  85 10
    ORA #$00                            ; C0A5  09 00
    STA obj_meta_id_s01                 ; C0A7  8d 01 03
    LDA #$0C                            ; C0AA  a9 0c
    LDY equip_shield                    ; C0AC  ac 14 07
    BNE loc_30_C0B3                     ; C0AF  d0 02
    LDA #$08                            ; C0B1  a9 08
loc_30_C0B3:
    ORA jmp_trampoline                  ; C0B3  05 10
    STA obj_overlay_id_s01              ; C0B5  8d 81 05
    LDY #$40                            ; C0B8  a0 40
    LDA equip_armor                     ; C0BA  ad 13 07
    BEQ loc_30_C0C0                     ; C0BD  f0 01
    INY                                 ; C0BF  c8
loc_30_C0C0:
    STY chr_r2_play                     ; C0C0  8c f2 07
    LDY equip_sword                     ; C0C3  ac 11 07
    CPY #$05                            ; C0C6  c0 05
    BCC loc_30_C0CC                     ; C0C8  90 02
    LDY #$05                            ; C0CA  a0 05
loc_30_C0CC:
    TYA                                 ; C0CC  98
    BNE loc_30_C0D0                     ; C0CD  d0 01
    INY                                 ; C0CF  c8
loc_30_C0D0:
    STY pal_id_spr1                     ; C0D0  8c e5 07
    TYA                                 ; C0D3  98
    CLC                                 ; C0D4  18
    ADC #$41                            ; C0D5  69 41
    STA chr_r3_play                     ; C0D7  8d f3 07
loc_30_C0DA:
    LDA cur_bank16_x2                   ; C0DA  a5 6e
    PHA                                 ; C0DC  48
    LDA r7_shadow                       ; C0DD  a5 6f
    PHA                                 ; C0DF  48
    LDA #$1A                            ; C0E0  a9 1a
    JSR SetR6                           ; C0E2  20 18 c4
    BIT pal_nofade_mask                 ; C0E5  2c e8 07
    BMI loc_30_C0ED                     ; C0E8  30 03
    JSR PaletteBuild                    ; C0EA  20 0e 8c
loc_30_C0ED:
    LDA #$04                            ; C0ED  a9 04
    JSR HudRedraw                       ; C0EF  20 46 8e
    JSR HudHpBar                        ; C0F2  20 c0 8c
    JSR WaitPpuQueue                    ; C0F5  20 76 c6
    PLA                                 ; C0F8  68
    JSR SetR7                           ; C0F9  20 27 c4
    PLA                                 ; C0FC  68
    JMP SetR6                           ; C0FD  4c 18 c4
loc_30_C100:
    LDA #$64                            ; C100  a9 64
    STA chr_r2_play                     ; C102  8d f2 07
    BNE loc_30_C10C                     ; C105  d0 05
loc_30_C107:
    LDA #$4D                            ; C107  a9 4d
    STA chr_r2_play                     ; C109  8d f2 07
loc_30_C10C:
    LDA obj_flags_s01                   ; C10C  ad 81 03
    AND #$FD                            ; C10F  29 fd
    STA obj_flags_s01                   ; C111  8d 81 03
    LDA #$42                            ; C114  a9 42
    STA chr_r3_play                     ; C116  8d f3 07
    LDA #$80                            ; C119  a9 80
    STA obj_tile_ofs_s01                ; C11B  8d 21 03
    JMP loc_30_C0DA                     ; C11E  4c da c0
    .db $08,$04,$01,$02                 ; 30:C121
PlaySound:  ; A=sound id (Y kept in $010F); music <$20 deduped vs $0102; enqueue to $0103-$0108; 85 callers
    STY snd_saved_y                     ; C125  8c 0f 01
    CMP #$A0                            ; C128  c9 a0
    BCS loc_30_C13F                     ; C12A  b0 13
    BMI loc_30_C13D                     ; C12C  30 0f
    CMP #$20                            ; C12E  c9 20
    BCS loc_30_C13F                     ; C130  b0 0d
    CMP snd_cur_music                   ; C132  cd 02 01
    BEQ loc_30_C165                     ; C135  f0 2e
    STA snd_cur_music                   ; C137  8d 02 01
    JMP loc_30_C13F                     ; C13A  4c 3f c1
loc_30_C13D:
    AND #$7F                            ; C13D  29 7f
loc_30_C13F:
    LDY #$01                            ; C13F  a0 01
    STY snd_queue_lock                  ; C141  8c 01 01
    LDY snd_queue_count                 ; C144  ac 00 01
    CPY #$05                            ; C147  c0 05
    BNE loc_30_C154                     ; C149  d0 09
    LDY $0108                           ; C14B  ac 08 01
    CMP #$20                            ; C14E  c9 20
    BCC loc_30_C160                     ; C150  90 0e
    LDY #$05                            ; C152  a0 05
loc_30_C154:
    STA snd_queue,Y                     ; C154  99 03 01
    INY                                 ; C157  c8
    CPY #$06                            ; C158  c0 06
    BNE loc_30_C15D                     ; C15A  d0 01
    DEY                                 ; C15C  88
loc_30_C15D:
    STY snd_queue_count                 ; C15D  8c 00 01
loc_30_C160:
    LDA #$00                            ; C160  a9 00
    STA snd_queue_lock                  ; C162  8d 01 01
loc_30_C165:
    LDY snd_saved_y                     ; C165  ac 0f 01
    RTS                                 ; C168  60
WaitFrame:  ; clear frame_ready and spin until NMI sets it
    LDA #$00                            ; C169  a9 00
    STA frame_ready                     ; C16B  85 09
loc_30_C16D:
    LDA frame_ready                     ; C16D  a5 09
    BEQ loc_30_C16D                     ; C16F  f0 fc
    LDA #$00                            ; C171  a9 00
    STA frame_ready                     ; C173  85 09
    RTS                                 ; C175  60
sub_30_C176:
    JSR WaitFrame                       ; C176  20 69 c1
    DEY                                 ; C179  88
    BNE sub_30_C176                     ; C17A  d0 fa
    RTS                                 ; C17C  60
OamClipTop:  ; hide OAM sprites 1-63 with Y <= $58-($04&7) (Y=$F0) under the top window
    LDA cam_y_lo                        ; C17D  a5 04
    AND #$07                            ; C17F  29 07
    STA jmp_trampoline                  ; C181  85 10
    LDA #$58                            ; C183  a9 58
    SEC                                 ; C185  38
    SBC jmp_trampoline                  ; C186  e5 10
sub_30_C188:
    STA jmp_trampoline                  ; C188  85 10
    LDX #$04                            ; C18A  a2 04
loc_30_C18C:
    LDA jmp_trampoline                  ; C18C  a5 10
    CMP oam_y,X                         ; C18E  dd 00 02
    BCC loc_30_C198                     ; C191  90 05
    LDA #$F0                            ; C193  a9 f0
    STA oam_y,X                         ; C195  9d 00 02
loc_30_C198:
    INX                                 ; C198  e8
    INX                                 ; C199  e8
    INX                                 ; C19A  e8
    INX                                 ; C19B  e8
    BNE loc_30_C18C                     ; C19C  d0 ee
    RTS                                 ; C19E  60
sub_30_C19F:
    LDA #$F0                            ; C19F  a9 f0
    STA oam_y                           ; C1A1  8d 00 02
    STA oam_tile                        ; C1A4  8d 01 02
    STA oam_attr                        ; C1A7  8d 02 02
    STA oam_x                           ; C1AA  8d 03 02
    RTS                                 ; C1AD  60
sub_30_C1AE:
    LDA cur_bank16_x2                   ; C1AE  a5 6e
    PHA                                 ; C1B0  48
    LDA r7_shadow                       ; C1B1  a5 6f
    PHA                                 ; C1B3  48
    LDA #$0E                            ; C1B4  a9 0e
    JSR SwitchBank16                    ; C1B6  20 0e c4
    LDA #$04                            ; C1B9  a9 04
    STA jmp_trampoline                  ; C1BB  85 10
loc_30_C1BD:
    LDA tmp_20                          ; C1BD  a5 20
    ASL A                               ; C1BF  0a
    ASL A                               ; C1C0  0a
    ASL A                               ; C1C1  0a
    TAX                                 ; C1C2  aa
    LDA wram_buf,X                      ; C1C3  bd 00 60
    STA tmp_12                          ; C1C6  85 12
    LDA wram_6001,X                     ; C1C8  bd 01 60
    STA tmp_13                          ; C1CB  85 13
    LDA wram_6002,X                     ; C1CD  bd 02 60
    STA $031F                           ; C1D0  8d 1f 03
    LDA wram_6003,X                     ; C1D3  bd 03 60
    STA $059F                           ; C1D6  8d 9f 05
    LDA wram_6004,X                     ; C1D9  bd 04 60
    STA $039F                           ; C1DC  8d 9f 03
    ASL A                               ; C1DF  0a
    STA tmp_17                          ; C1E0  85 17
    LDA wram_6005,X                     ; C1E2  bd 05 60
    STA $033F                           ; C1E5  8d 3f 03
    LDA wram_6006,X                     ; C1E8  bd 06 60
    STA $037F                           ; C1EB  8d 7f 03
    LDA wram_6007,X                     ; C1EE  bd 07 60
    STA $04FF                           ; C1F1  8d ff 04
    LDA #$01                            ; C1F4  a9 01
    STA obj_active_s1F                  ; C1F6  8d bf 04
    JSR sub_28_8000                     ; C1F9  20 00 80
    LDA #$00                            ; C1FC  a9 00
    STA obj_active_s1F                  ; C1FE  8d bf 04
    DEC tmp_20                          ; C201  c6 20
    BPL loc_30_C1BD                     ; C203  10 b8
    JSR ObjHideUnusedOam                ; C205  20 a5 80
    PLA                                 ; C208  68
    JSR SetR7                           ; C209  20 27 c4
    PLA                                 ; C20C  68
    JMP SetR6                           ; C20D  4c 18 c4
PadNewButtonIndex:  ; s67: C=1 + A=Y=index 0-7 of the single button in pad_new (A B SEL START U D L R; PadButtonMaskTable); C=0 + A=$FF if none / several
    LDY #$07                            ; C210  a0 07
    LDA pad_new                         ; C212  a5 4b
loc_30_C214:
    CMP PadButtonMaskTable,Y            ; C214  d9 22 c2
    BEQ loc_30_C21F                     ; C217  f0 06
    DEY                                 ; C219  88
    BPL loc_30_C214                     ; C21A  10 f8
    TYA                                 ; C21C  98
    CLC                                 ; C21D  18
    RTS                                 ; C21E  60
loc_30_C21F:
    TYA                                 ; C21F  98
    SEC                                 ; C220  38
    RTS                                 ; C221  60
PadButtonMaskTable:  ; s67: u8[8] 80 40 20 10 08 04 02 01
    .db $80,$40,$20,$10,$08,$04,$02,$01,$A5,$30,$18,$65,$34,$85,$34,$A5; 30:C222
    .db $30,$10,$04,$A9,$FF,$D0,$02,$A9,$00,$65,$35,$85,$35,$A5,$31,$18; 30:C232
    .db $65,$36,$C9,$F0,$B0,$03,$85,$36,$60,$24,$31,$30,$07,$69,$0F,$85; 30:C242
    .db $36,$E6,$37,$60,$69,$EF,$85,$36,$C6,$37,$60; 30:C252
ObjInit:  ; init object slot $10 with type $11 (type 1 = no-op)
    LDA tmp_11                          ; C25D  a5 11
    CMP #$01                            ; C25F  c9 01
    BNE loc_30_C264                     ; C261  d0 01
    RTS                                 ; C263  60
loc_30_C264:
    TXA                                 ; C264  8a
    PHA                                 ; C265  48
    TYA                                 ; C266  98
    PHA                                 ; C267  48
    LDA #$0D                            ; C268  a9 0d
    JSR SetR7                           ; C26A  20 27 c4
    LDA tmp_11                          ; C26D  a5 11
    ASL A                               ; C26F  0a
    TAY                                 ; C270  a8
    LDA ObjTypeTable,Y                  ; C271  b9 00 ac
    STA tmp_12                          ; C274  85 12
    LDA $AC01,Y                         ; C276  b9 01 ac
    STA tmp_13                          ; C279  85 13
    BCC loc_30_C287                     ; C27B  90 0a
    LDA ObjTypeTableHi,Y                ; C27D  b9 00 ad
    STA tmp_12                          ; C280  85 12
    LDA $AD01,Y                         ; C282  b9 01 ad
    STA tmp_13                          ; C285  85 13
loc_30_C287:
    LDY #$00                            ; C287  a0 00
    LDA (tmp_12),Y                      ; C289  b1 12
    BEQ loc_30_C290                     ; C28B  f0 03
    JSR PlaySound                       ; C28D  20 25 c1
loc_30_C290:
    LDY #$01                            ; C290  a0 01
    LDX jmp_trampoline                  ; C292  a6 10
    CLC                                 ; C294  18
loc_30_C295:
    LDA #$00                            ; C295  a9 00
    STA obj_meta_id,X                   ; C297  9d 00 03
    STA obj_defense,X                   ; C29A  9d 00 04
    STA obj_elem_drop,X                 ; C29D  9d 00 05
    STA obj_phase,X                     ; C2A0  9d 00 06
    TXA                                 ; C2A3  8a
    ADC #$20                            ; C2A4  69 20
    TAX                                 ; C2A6  aa
    BCC loc_30_C295                     ; C2A7  90 ec
    LDA tmp_11                          ; C2A9  a5 11
    CMP #$FF                            ; C2AB  c9 ff
    BNE loc_30_C2B4                     ; C2AD  d0 05
    PLA                                 ; C2AF  68
    TAY                                 ; C2B0  a8
    PLA                                 ; C2B1  68
    TAX                                 ; C2B2  aa
    RTS                                 ; C2B3  60
loc_30_C2B4:
    LDA tmp_11                          ; C2B4  a5 11
    PHA                                 ; C2B6  48
    LDA (tmp_12),Y                      ; C2B7  b1 12
    ASL A                               ; C2B9  0a
    STA tmp_11                          ; C2BA  85 11
    INY                                 ; C2BC  c8
    BCC loc_30_C2C5                     ; C2BD  90 06
    LDA (tmp_12),Y                      ; C2BF  b1 12
    STA obj_meta_id,X                   ; C2C1  9d 00 03
    INY                                 ; C2C4  c8
loc_30_C2C5:
    ASL tmp_11                          ; C2C5  06 11
    BCC loc_30_C2CF                     ; C2C7  90 06
    LDA (tmp_12),Y                      ; C2C9  b1 12
    STA obj_tile_ofs,X                  ; C2CB  9d 20 03
    INY                                 ; C2CE  c8
loc_30_C2CF:
    ASL tmp_11                          ; C2CF  06 11
    BCC loc_30_C2D9                     ; C2D1  90 06
    LDA (tmp_12),Y                      ; C2D3  b1 12
    STA obj_flags2,X                    ; C2D5  9d 40 03
    INY                                 ; C2D8  c8
loc_30_C2D9:
    ASL tmp_11                          ; C2D9  06 11
    BCC loc_30_C2E3                     ; C2DB  90 06
    LDA (tmp_12),Y                      ; C2DD  b1 12
    STA obj_meta_dir,X                  ; C2DF  9d 60 03
    INY                                 ; C2E2  c8
loc_30_C2E3:
    ASL tmp_11                          ; C2E3  06 11
    BCC loc_30_C2ED                     ; C2E5  90 06
    LDA (tmp_12),Y                      ; C2E7  b1 12
    STA obj_flags,X                     ; C2E9  9d 80 03
    INY                                 ; C2EC  c8
loc_30_C2ED:
    ASL tmp_11                          ; C2ED  06 11
    BCC loc_30_C2F7                     ; C2EF  90 06
    LDA (tmp_12),Y                      ; C2F1  b1 12
    STA obj_coll,X                      ; C2F3  9d a0 03
    INY                                 ; C2F6  c8
loc_30_C2F7:
    ASL tmp_11                          ; C2F7  06 11
    BCC loc_30_C301                     ; C2F9  90 06
    LDA (tmp_12),Y                      ; C2FB  b1 12
    STA player_hp_max,X                 ; C2FD  9d c0 03
    INY                                 ; C300  c8
loc_30_C301:
    ASL tmp_11                          ; C301  06 11
    BCC loc_30_C30B                     ; C303  90 06
    LDA (tmp_12),Y                      ; C305  b1 12
    STA obj_attack,X                    ; C307  9d e0 03
    INY                                 ; C30A  c8
loc_30_C30B:
    LDA (tmp_12),Y                      ; C30B  b1 12
    ASL A                               ; C30D  0a
    STA tmp_11                          ; C30E  85 11
    INY                                 ; C310  c8
    BCC loc_30_C319                     ; C311  90 06
    LDA (tmp_12),Y                      ; C313  b1 12
    STA obj_defense,X                   ; C315  9d 00 04
    INY                                 ; C318  c8
loc_30_C319:
    ASL tmp_11                          ; C319  06 11
    BCC loc_30_C323                     ; C31B  90 06
    LDA (tmp_12),Y                      ; C31D  b1 12
    STA obj_hitflags,X                  ; C31F  9d 20 04
    INY                                 ; C322  c8
loc_30_C323:
    ASL tmp_11                          ; C323  06 11
    BCC loc_30_C32D                     ; C325  90 06
    LDA (tmp_12),Y                      ; C327  b1 12
    STA obj_shot_id,X                   ; C329  9d 40 04
    INY                                 ; C32C  c8
loc_30_C32D:
    ASL tmp_11                          ; C32D  06 11
    BCC loc_30_C337                     ; C32F  90 06
    LDA (tmp_12),Y                      ; C331  b1 12
    STA obj_bg_mask,X                   ; C333  9d 60 04
    INY                                 ; C336  c8
loc_30_C337:
    ASL tmp_11                          ; C337  06 11
    BCC loc_30_C341                     ; C339  90 06
    LDA (tmp_12),Y                      ; C33B  b1 12
    STA obj_timer,X                     ; C33D  9d 80 04
    INY                                 ; C340  c8
loc_30_C341:
    ASL tmp_11                          ; C341  06 11
    BCC loc_30_C34B                     ; C343  90 06
    LDA (tmp_12),Y                      ; C345  b1 12
    STA obj_active,X                    ; C347  9d a0 04
    INY                                 ; C34A  c8
loc_30_C34B:
    ASL tmp_11                          ; C34B  06 11
    BCC loc_30_C355                     ; C34D  90 06
    LDA (tmp_12),Y                      ; C34F  b1 12
    STA obj_death_type,X                ; C351  9d c0 04
    INY                                 ; C354  c8
loc_30_C355:
    ASL tmp_11                          ; C355  06 11
    BCC loc_30_C35F                     ; C357  90 06
    LDA (tmp_12),Y                      ; C359  b1 12
    STA obj_anim,X                      ; C35B  9d e0 04
    INY                                 ; C35E  c8
loc_30_C35F:
    LDA (tmp_12),Y                      ; C35F  b1 12
    ASL A                               ; C361  0a
    STA tmp_11                          ; C362  85 11
    INY                                 ; C364  c8
    BCC loc_30_C36D                     ; C365  90 06
    LDA (tmp_12),Y                      ; C367  b1 12
    STA obj_elem_drop,X                 ; C369  9d 00 05
    INY                                 ; C36C  c8
loc_30_C36D:
    ASL tmp_11                          ; C36D  06 11
    BCC loc_30_C377                     ; C36F  90 06
    LDA (tmp_12),Y                      ; C371  b1 12
    STA obj_xp_value,X                  ; C373  9d 20 05
    INY                                 ; C376  c8
loc_30_C377:
    ASL tmp_11                          ; C377  06 11
    BCC loc_30_C381                     ; C379  90 06
    LDA (tmp_12),Y                      ; C37B  b1 12
    STA obj_spawn_b2,X                  ; C37D  9d 40 05
    INY                                 ; C380  c8
loc_30_C381:
    ASL tmp_11                          ; C381  06 11
    BCC loc_30_C38B                     ; C383  90 06
    LDA (tmp_12),Y                      ; C385  b1 12
    STA obj_spawn_param,X               ; C387  9d 60 05
    INY                                 ; C38A  c8
loc_30_C38B:
    ASL tmp_11                          ; C38B  06 11
    BCC loc_30_C395                     ; C38D  90 06
    LDA (tmp_12),Y                      ; C38F  b1 12
    STA obj_overlay_id,X                ; C391  9d 80 05
    INY                                 ; C394  c8
loc_30_C395:
    ASL tmp_11                          ; C395  06 11
    BCC loc_30_C39F                     ; C397  90 06
    LDA (tmp_12),Y                      ; C399  b1 12
    STA obj_freeze,X                    ; C39B  9d a0 05
    INY                                 ; C39E  c8
loc_30_C39F:
    ASL tmp_11                          ; C39F  06 11
    BCC loc_30_C3A9                     ; C3A1  90 06
    LDA (tmp_12),Y                      ; C3A3  b1 12
    STA obj_screen_x,X                  ; C3A5  9d c0 05
    INY                                 ; C3A8  c8
loc_30_C3A9:
    ASL tmp_11                          ; C3A9  06 11
    BCC loc_30_C3B3                     ; C3AB  90 06
    LDA (tmp_12),Y                      ; C3AD  b1 12
    STA obj_screen_y,X                  ; C3AF  9d e0 05
    INY                                 ; C3B2  c8
loc_30_C3B3:
    LDA (tmp_12),Y                      ; C3B3  b1 12
    ASL A                               ; C3B5  0a
    STA tmp_11                          ; C3B6  85 11
    INY                                 ; C3B8  c8
    BCC loc_30_C3C1                     ; C3B9  90 06
    LDA (tmp_12),Y                      ; C3BB  b1 12
    STA obj_phase,X                     ; C3BD  9d 00 06
    INY                                 ; C3C0  c8
loc_30_C3C1:
    ASL tmp_11                          ; C3C1  06 11
    BCC loc_30_C3CB                     ; C3C3  90 06
    LDA (tmp_12),Y                      ; C3C5  b1 12
    STA obj_timer2,X                    ; C3C7  9d 20 06
    INY                                 ; C3CA  c8
loc_30_C3CB:
    ASL tmp_11                          ; C3CB  06 11
    BCC loc_30_C3D5                     ; C3CD  90 06
    LDA (tmp_12),Y                      ; C3CF  b1 12
    STA obj_act_timer,X                 ; C3D1  9d 40 06
    INY                                 ; C3D4  c8
loc_30_C3D5:
    ASL tmp_11                          ; C3D5  06 11
    BCC loc_30_C3DF                     ; C3D7  90 06
    LDA (tmp_12),Y                      ; C3D9  b1 12
    STA obj_aux,X                       ; C3DB  9d 60 06
    INY                                 ; C3DE  c8
loc_30_C3DF:
    ASL tmp_11                          ; C3DF  06 11
    BCC loc_30_C3E9                     ; C3E1  90 06
    LDA (tmp_12),Y                      ; C3E3  b1 12
    STA obj_param1,X                    ; C3E5  9d 80 06
    INY                                 ; C3E8  c8
loc_30_C3E9:
    ASL tmp_11                          ; C3E9  06 11
    BCC loc_30_C3F3                     ; C3EB  90 06
    LDA (tmp_12),Y                      ; C3ED  b1 12
    STA obj_param2,X                    ; C3EF  9d a0 06
    INY                                 ; C3F2  c8
loc_30_C3F3:
    ASL tmp_11                          ; C3F3  06 11
    BCC loc_30_C3FD                     ; C3F5  90 06
    LDA (tmp_12),Y                      ; C3F7  b1 12
    STA orbit_level,X                   ; C3F9  9d c0 06
    INY                                 ; C3FC  c8
loc_30_C3FD:
    ASL tmp_11                          ; C3FD  06 11
    BCC loc_30_C406                     ; C3FF  90 05
    LDA (tmp_12),Y                      ; C401  b1 12
    STA obj_behav,X                     ; C403  9d e0 06
loc_30_C406:
    PLA                                 ; C406  68
    STA tmp_11                          ; C407  85 11
    PLA                                 ; C409  68
    TAY                                 ; C40A  a8
    PLA                                 ; C40B  68
    TAX                                 ; C40C  aa
    RTS                                 ; C40D  60
SwitchBank16:  ; A=16KB bank n -> SetR7(2n+1) then SetR6(2n) (falls into C418 path); clobbers A
    ASL A                               ; C40E  0a
    STA cur_bank16_x2                   ; C40F  85 6e
    ORA #$01                            ; C411  09 01
    JSR SetR7                           ; C413  20 27 c4
    LDA cur_bank16_x2                   ; C416  a5 6e
SetR6:  ; A=8KB bank for $8000 window; $6E=shadow; $50=6 (also called directly e.g. A=$1A bank 26)
    STA cur_bank16_x2                   ; C418  85 6e
    LDA #$06                            ; C41A  a9 06
    STA mmc3_banksel_shadow             ; C41C  85 50
    STA $8000                           ; C41E  8d 00 80
    LDA cur_bank16_x2                   ; C421  a5 6e
    STA $8001                           ; C423  8d 01 80
    RTS                                 ; C426  60
SetR7:  ; A=8KB bank for $A000 window; $6F=shadow; $50=7 (verified)
    STA r7_shadow                       ; C427  85 6f
    LDA #$07                            ; C429  a9 07
    STA mmc3_banksel_shadow             ; C42B  85 50
    STA $8000                           ; C42D  8d 00 80
    LDA r7_shadow                       ; C430  a5 6f
    STA $8001                           ; C432  8d 01 80
    RTS                                 ; C435  60
sub_30_C436:
    LDA ppuctrl_shadow                  ; C436  a5 00
    ORA #$80                            ; C438  09 80
    STA PPUCTRL                         ; C43A  8d 00 20
    RTS                                 ; C43D  60
sub_30_C43E:
    LDA ppuctrl_shadow                  ; C43E  a5 00
    AND #$7F                            ; C440  29 7f
    STA PPUCTRL                         ; C442  8d 00 20
    RTS                                 ; C445  60
    .db $A5,$01,$29,$E7,$8D,$01,$20,$85,$01,$60,$A5,$01,$09,$18,$8D,$01; 30:C446
    .db $20,$85,$01,$60,$A5,$01,$29,$F7,$8D,$01,$20,$85,$01,$60,$A5,$01; 30:C456
    .db $09,$08,$8D,$01,$20,$85,$01,$60,$A5,$01,$29,$EF,$8D,$01,$20,$85; 30:C466
    .db $01,$60,$A5,$01,$09,$10,$8D,$01,$20,$85,$01,$60; 30:C476
PpuQueueBlock:  ; s60: A=block id (x5 record); WaitPpuQueue then queues the block
    STA jmp_trampoline                  ; C482  85 10
    JSR WaitPpuQueue                    ; C484  20 76 c6
    TXA                                 ; C487  8a
    PHA                                 ; C488  48
    LDA jmp_trampoline                  ; C489  a5 10
    PHA                                 ; C48B  48
    STA tmp_25                          ; C48C  85 25
    LDA #$00                            ; C48E  a9 00
    STA tmp_26                          ; C490  85 26
    ASL tmp_25                          ; C492  06 25
    ROL tmp_26                          ; C494  26 26
    ASL tmp_25                          ; C496  06 25
    ROL tmp_26                          ; C498  26 26
    PLA                                 ; C49A  68
    ADC tmp_25                          ; C49B  65 25
    BCC loc_30_C4A1                     ; C49D  90 02
    INC tmp_26                          ; C49F  e6 26
loc_30_C4A1:
    CLC                                 ; C4A1  18
    ADC #$18                            ; C4A2  69 18
    STA tmp_25                          ; C4A4  85 25
    LDA tmp_26                          ; C4A6  a5 26
    ADC #$C5                            ; C4A8  69 c5
    STA tmp_26                          ; C4AA  85 26
    LDY #$00                            ; C4AC  a0 00
loc_30_C4AE:
    LDA (tmp_25),Y                      ; C4AE  b1 25
    STA a:tmp_20,Y                      ; C4B0  99 20 00
    INY                                 ; C4B3  c8
    CPY #$05                            ; C4B4  c0 05
    BNE loc_30_C4AE                     ; C4B6  d0 f6
    LDA tmp_24                          ; C4B8  a5 24
    AND #$7F                            ; C4BA  29 7f
    STA tmp_25                          ; C4BC  85 25
loc_30_C4BE:
    JSR sub_30_C43E                     ; C4BE  20 3e c4
    LDX tmp_0B                          ; C4C1  a6 0b
    LDA tmp_20                          ; C4C3  a5 20
    STA ppu_queue,X                     ; C4C5  9d 00 62
    LDA tmp_21                          ; C4C8  a5 21
    STA ppu_queue_1,X                   ; C4CA  9d 01 62
    LDA tmp_22                          ; C4CD  a5 22
    STA ppu_queue_2,X                   ; C4CF  9d 02 62
    LDA tmp_23                          ; C4D2  a5 23
    STA ppu_queue_3,X                   ; C4D4  9d 03 62
    TXA                                 ; C4D7  8a
    CLC                                 ; C4D8  18
    ADC #$04                            ; C4D9  69 04
    AND #$1F                            ; C4DB  29 1f
    STA tmp_0B                          ; C4DD  85 0b
    LDA ppumask_shadow                  ; C4DF  a5 01
    AND #$18                            ; C4E1  29 18
    BNE loc_30_C4E8                     ; C4E3  d0 03
    JSR sub_30_C8B2                     ; C4E5  20 b2 c8
loc_30_C4E8:
    JSR sub_30_C436                     ; C4E8  20 36 c4
    LDA tmp_22                          ; C4EB  a5 22
    CMP #$20                            ; C4ED  c9 20
    BCS loc_30_C4F3                     ; C4EF  b0 02
    LDA #$20                            ; C4F1  a9 20
loc_30_C4F3:
    CLC                                 ; C4F3  18
    ADC tmp_21                          ; C4F4  65 21
    STA tmp_21                          ; C4F6  85 21
    BCC loc_30_C4FC                     ; C4F8  90 02
    INC tmp_20                          ; C4FA  e6 20
loc_30_C4FC:
    LDA tmp_22                          ; C4FC  a5 22
    CLC                                 ; C4FE  18
    ADC tmp_23                          ; C4FF  65 23
    STA tmp_23                          ; C501  85 23
    BIT tmp_20                          ; C503  24 20
    BMI loc_30_C50A                     ; C505  30 03
    JSR WaitPpuQueue                    ; C507  20 76 c6
loc_30_C50A:
    DEC tmp_25                          ; C50A  c6 25
    BPL loc_30_C4BE                     ; C50C  10 b0
    BIT tmp_24                          ; C50E  24 24
    BMI loc_30_C515                     ; C510  30 03
    JSR WaitPpuQueue                    ; C512  20 76 c6
loc_30_C515:
    PLA                                 ; C515  68
    TAX                                 ; C516  aa
    RTS                                 ; C517  60
PpuBlockTable:  ; s72: 70 x 5-byte PpuQueueBlock records ($00-$45): ppu hi / ppu lo / len / src ofs in $6000 / rows-1 (b7 nowait); tools/ppu_block_dump.py
    .db $2B,$00,$20,$00,$00,$2B,$80,$20,$20,$00,$2B,$A0,$20,$60,$00,$2B; 30:C518
    .db $C0,$40,$60,$00,$2B,$20,$20,$00,$02,$28,$60,$60,$00,$02,$29,$80; 30:C528
    .db $60,$00,$02,$28,$00,$60,$00,$00,$2A,$A0,$60,$60,$00,$A8,$A9,$06; 30:C538
    .db $00,$01,$A8,$84,$04,$00,$02,$AA,$08,$13,$0C,$00,$A9,$C8,$13,$0C; 30:C548
    .db $00,$AA,$48,$13,$0C,$00,$AA,$88,$13,$0C,$00,$A9,$24,$05,$0C,$00; 30:C558
    .db $A8,$B3,$0B,$11,$01,$A9,$64,$09,$2A,$00,$A9,$31,$09,$33,$00,$A9; 30:C568
    .db $71,$0B,$3C,$00,$28,$00,$40,$80,$00,$28,$40,$40,$80,$00,$28,$80; 30:C578
    .db $40,$80,$00,$28,$C0,$40,$80,$00,$29,$00,$40,$80,$00,$29,$40,$40; 30:C588
    .db $80,$00,$29,$80,$40,$80,$00,$29,$C0,$40,$80,$00,$2A,$00,$40,$80; 30:C598
    .db $00,$2A,$40,$40,$80,$00,$2A,$80,$40,$80,$00,$2A,$C0,$40,$80,$00; 30:C5A8
    .db $A8,$23,$09,$00,$01,$A8,$2D,$09,$00,$01,$A8,$36,$09,$00,$01,$A8; 30:C5B8
    .db $63,$09,$00,$01,$A8,$6D,$09,$00,$01,$A8,$76,$09,$00,$01,$A8,$A3; 30:C5C8
    .db $09,$00,$01,$A8,$AD,$09,$00,$01,$A8,$B6,$09,$00,$01,$A8,$E3,$09; 30:C5D8
    .db $00,$01,$A8,$ED,$09,$00,$01,$23,$C0,$40,$00,$00,$20,$4C,$0A,$00; 30:C5E8
    .db $0B,$23,$C0,$18,$78,$00,$21,$05,$16,$00,$09,$A0,$08,$0E,$00,$82; 30:C5F8
    .db $A0,$68,$0E,$00,$82,$A0,$C8,$0E,$00,$82,$A1,$28,$0E,$00,$82,$A1; 30:C608
    .db $88,$0E,$00,$82,$A0,$08,$0E,$00,$82,$A0,$68,$0E,$00,$82,$A0,$C8; 30:C618
    .db $0E,$00,$82,$A1,$28,$0E,$00,$82,$A1,$88,$0E,$00,$82,$20,$00,$40; 30:C628
    .db $00,$03,$21,$00,$40,$00,$03,$22,$00,$40,$00,$03,$23,$00,$40,$00; 30:C638
    .db $03,$21,$2E,$02,$00,$01,$20,$C2,$06,$00,$01,$20,$D8,$06,$0C,$01; 30:C648
    .db $21,$42,$06,$18,$03,$21,$58,$06,$30,$03,$20,$CC,$08,$48,$07,$21; 30:C658
    .db $CE,$04,$88,$02,$20,$CC,$08,$94,$07,$21,$8E,$04,$D4,$02; 30:C668
WaitPpuQueue:  ; s60: spin until $0A=$0B (PPU queue drained)
    LDA tmp_0A                          ; C676  a5 0a
    CMP tmp_0B                          ; C678  c5 0b
    BNE WaitPpuQueue                    ; C67A  d0 fa
loc_30_C67C:
    RTS                                 ; C67C  60
PpuQueueFlush:  ; s48: NMI PPU queue flush: ring $6200 from $0A to $0B; record b6 -> PPUCTRL +32 increment; PPUADDR + data
    LDX tmp_0A                          ; C67D  a6 0a
    CPX tmp_0B                          ; C67F  e4 0b
    BEQ loc_30_C67C                     ; C681  f0 f9
    LDA ppu_queue,X                     ; C683  bd 00 62
    STA tmp_0C                          ; C686  85 0c
    LDA ppuctrl_shadow                  ; C688  a5 00
    AND #$FB                            ; C68A  29 fb
    BIT tmp_0C                          ; C68C  24 0c
    BVC loc_30_C692                     ; C68E  50 02
    ORA #$04                            ; C690  09 04
loc_30_C692:
    STA PPUCTRL                         ; C692  8d 00 20
    LDA tmp_0C                          ; C695  a5 0c
    STA PPUADDR                         ; C697  8d 06 20
    LDA ppu_queue_1,X                   ; C69A  bd 01 62
    STA PPUADDR                         ; C69D  8d 06 20
    LDA ppu_queue_2,X                   ; C6A0  bd 02 62
    STA $0D                             ; C6A3  85 0d
    LDY ppu_queue_3,X                   ; C6A5  bc 03 62
    LDA #$FF                            ; C6A8  a9 ff
    STA ppu_queue_3,X                   ; C6AA  9d 03 62
    LDX $0D                             ; C6AD  a6 0d
    BMI PpuQueueAttrCopy                ; C6AF  30 36
loc_30_C6B1:
    LDA wram_buf,Y                      ; C6B1  b9 00 60
    STA PPUDATA                         ; C6B4  8d 07 20
    INY                                 ; C6B7  c8
    DEX                                 ; C6B8  ca
    BEQ loc_30_C6D9                     ; C6B9  f0 1e
    LDA wram_buf,Y                      ; C6BB  b9 00 60
    STA PPUDATA                         ; C6BE  8d 07 20
    INY                                 ; C6C1  c8
    DEX                                 ; C6C2  ca
    BEQ loc_30_C6D9                     ; C6C3  f0 14
    LDA wram_buf,Y                      ; C6C5  b9 00 60
    STA PPUDATA                         ; C6C8  8d 07 20
    INY                                 ; C6CB  c8
    DEX                                 ; C6CC  ca
    BEQ loc_30_C6D9                     ; C6CD  f0 0a
    LDA wram_buf,Y                      ; C6CF  b9 00 60
    STA PPUDATA                         ; C6D2  8d 07 20
    INY                                 ; C6D5  c8
    DEX                                 ; C6D6  ca
    BNE loc_30_C6B1                     ; C6D7  d0 d8
loc_30_C6D9:
    LDA tmp_0A                          ; C6D9  a5 0a
    CLC                                 ; C6DB  18
    ADC #$04                            ; C6DC  69 04
    AND #$1F                            ; C6DE  29 1f
    STA tmp_0A                          ; C6E0  85 0a
    LDA tmp_0C                          ; C6E2  a5 0c
    BMI PpuQueueFlush                   ; C6E4  30 97
    RTS                                 ; C6E6  60
PpuQueueAttrCopy:  ; s64: PpuQueueFlush path for len b7 set: 64 bytes from attr_shadow $6100+src to PPUDATA; never chains
    LDX #$08                            ; C6E7  a2 08
loc_30_C6E9:
    LDA attr_shadow,Y                   ; C6E9  b9 00 61
    STA PPUDATA                         ; C6EC  8d 07 20
    LDA $6101,Y                         ; C6EF  b9 01 61
    STA PPUDATA                         ; C6F2  8d 07 20
    LDA $6102,Y                         ; C6F5  b9 02 61
    STA PPUDATA                         ; C6F8  8d 07 20
    LDA $6103,Y                         ; C6FB  b9 03 61
    STA PPUDATA                         ; C6FE  8d 07 20
    LDA $6104,Y                         ; C701  b9 04 61
    STA PPUDATA                         ; C704  8d 07 20
    LDA $6105,Y                         ; C707  b9 05 61
    STA PPUDATA                         ; C70A  8d 07 20
    LDA $6106,Y                         ; C70D  b9 06 61
    STA PPUDATA                         ; C710  8d 07 20
    LDA $6107,Y                         ; C713  b9 07 61
    STA PPUDATA                         ; C716  8d 07 20
    TYA                                 ; C719  98
    CLC                                 ; C71A  18
    ADC #$08                            ; C71B  69 08
    TAY                                 ; C71D  a8
    DEX                                 ; C71E  ca
    BNE loc_30_C6E9                     ; C71F  d0 c8
    LDA tmp_0A                          ; C721  a5 0a
    CLC                                 ; C723  18
    ADC #$04                            ; C724  69 04
    AND #$1F                            ; C726  29 1f
    STA tmp_0A                          ; C728  85 0a
    RTS                                 ; C72A  60
sub_30_C72B:
    LDY #$1C                            ; C72B  a0 1c
loc_30_C72D:
    CMP ppu_queue_3,Y                   ; C72D  d9 03 62
    BEQ sub_30_C72B                     ; C730  f0 f9
    DEY                                 ; C732  88
    DEY                                 ; C733  88
    DEY                                 ; C734  88
    DEY                                 ; C735  88
    BPL loc_30_C72D                     ; C736  10 f5
    RTS                                 ; C738  60
sub_30_C739:
    INC nmi_disable                     ; C739  e6 60
    LDX tmp_0B                          ; C73B  a6 0b
    LDA #$23                            ; C73D  a9 23
    STA ppu_queue,X                     ; C73F  9d 00 62
    LDA #$C0                            ; C742  a9 c0
    STA ppu_queue_1,X                   ; C744  9d 01 62
    LDA #$BF                            ; C747  a9 bf
    STA ppu_queue_2,X                   ; C749  9d 02 62
    LDA #$00                            ; C74C  a9 00
    STA ppu_queue_3,X                   ; C74E  9d 03 62
    TXA                                 ; C751  8a
    CLC                                 ; C752  18
    ADC #$04                            ; C753  69 04
    AND #$1F                            ; C755  29 1f
    STA tmp_0B                          ; C757  85 0b
    DEC nmi_disable                     ; C759  c6 60
    RTS                                 ; C75B  60
MarkScreenVisited:  ; set $62F0[y_hi] |= BitMask[x_hi]; sub-table 4 -> set $64C0 flag
    LDY obj_x_hi,X                      ; C75C  b4 90
    LDA BitMask,Y                       ; C75E  b9 00 c0
    LDY obj_y_hi,X                      ; C761  b4 d0
    ORA visited_map,Y                   ; C763  19 f0 62
    STA visited_map,Y                   ; C766  99 f0 62
    LDA obj_y_hi,X                      ; C769  b5 d0
    ASL A                               ; C76B  0a
    ASL A                               ; C76C  0a
    ASL A                               ; C76D  0a
    ASL A                               ; C76E  0a
    ORA obj_x_hi,X                      ; C76F  15 90
    STA tmp_12                          ; C771  85 12
    LDA #$04                            ; C773  a9 04
    JSR AreaSubTable                    ; C775  20 a5 c7
loc_30_C778:
    LDA (jmp_trampoline),Y              ; C778  b1 10
    CMP #$FF                            ; C77A  c9 ff
    BEQ SwitchBank16_13                 ; C77C  f0 22
    STA tmp_13                          ; C77E  85 13
    INY                                 ; C780  c8
    LDA (jmp_trampoline),Y              ; C781  b1 10
    INY                                 ; C783  c8
    CMP tmp_12                          ; C784  c5 12
    BNE loc_30_C778                     ; C786  d0 f0
    LDA tmp_13                          ; C788  a5 13
    AND #$07                            ; C78A  29 07
    TAY                                 ; C78C  a8
    LDA BitMask,Y                       ; C78D  b9 00 c0
    STA tmp_12                          ; C790  85 12
    LDA tmp_13                          ; C792  a5 13
    LSR A                               ; C794  4a
    LSR A                               ; C795  4a
    LSR A                               ; C796  4a
    TAY                                 ; C797  a8
    LDA screen_flags,Y                  ; C798  b9 c0 64
    ORA tmp_12                          ; C79B  05 12
    STA screen_flags,Y                  ; C79D  99 c0 64
SwitchBank16_13:  ; tail-call SwitchBank16(13) -> banks 26/27
    LDA #$0D                            ; C7A0  a9 0d
    JMP SwitchBank16                    ; C7A2  4c 0e c4
AreaSubTable:  ; A=k: SwitchBank16(5); ($10)=header(10:8300[$6C])[k]; Y=0. k0 map/music k1 gfx k2 entries k3 exits k4 screen flags k5 edge links
    PHA                                 ; C7A5  48
    LDA #$05                            ; C7A6  a9 05
    JSR SwitchBank16                    ; C7A8  20 0e c4
    LDA area_id                         ; C7AB  a5 6c
    ASL A                               ; C7AD  0a
    TAY                                 ; C7AE  a8
    LDA AreaHeaderTable,Y               ; C7AF  b9 00 83
    STA jmp_trampoline                  ; C7B2  85 10
    LDA $8301,Y                         ; C7B4  b9 01 83
    STA tmp_11                          ; C7B7  85 11
    BCC loc_30_C7C5                     ; C7B9  90 0a
    LDA AreaHeaderTableHi,Y             ; C7BB  b9 00 84
    STA jmp_trampoline                  ; C7BE  85 10
    LDA $8401,Y                         ; C7C0  b9 01 84
    STA tmp_11                          ; C7C3  85 11
loc_30_C7C5:
    PLA                                 ; C7C5  68
    ASL A                               ; C7C6  0a
    TAY                                 ; C7C7  a8
    LDA (jmp_trampoline),Y              ; C7C8  b1 10
    PHA                                 ; C7CA  48
    INY                                 ; C7CB  c8
    LDA (jmp_trampoline),Y              ; C7CC  b1 10
    STA tmp_11                          ; C7CE  85 11
    PLA                                 ; C7D0  68
    STA jmp_trampoline                  ; C7D1  85 10
    LDY #$00                            ; C7D3  a0 00
    RTS                                 ; C7D5  60
FadeSaveZpToWram:  ; copy ZP $0A-$29 -> $6000; banks 26/27 $8C3D(A=4); wait 30 frames; $8C49; bank 16/17; restore (tentative fade)
    LDX #$00                            ; C7D6  a2 00
loc_30_C7D8:
    LDA tmp_0A,X                        ; C7D8  b5 0a
    STA wram_buf,X                      ; C7DA  9d 00 60
    INX                                 ; C7DD  e8
    CPX #$20                            ; C7DE  e0 20
    BNE loc_30_C7D8                     ; C7E0  d0 f6
    LDA #$0D                            ; C7E2  a9 0d
    JSR SwitchBank16                    ; C7E4  20 0e c4
    LDA #$04                            ; C7E7  a9 04
    JSR Bank26_FadeA                    ; C7E9  20 3d 8c
    LDX #$1E                            ; C7EC  a2 1e
loc_30_C7EE:
    LSR frame_ready                     ; C7EE  46 09
    BCC loc_30_C7EE                     ; C7F0  90 fc
    DEX                                 ; C7F2  ca
    BNE loc_30_C7EE                     ; C7F3  d0 f9
    LDA #$04                            ; C7F5  a9 04
    JSR Bank26_FadeB                    ; C7F7  20 49 8c
    LDA #$08                            ; C7FA  a9 08
    JSR SwitchBank16                    ; C7FC  20 0e c4
    LDX #$00                            ; C7FF  a2 00
loc_30_C801:
    LDA wram_buf,X                      ; C801  bd 00 60
    STA tmp_0A,X                        ; C804  95 0a
    INX                                 ; C806  e8
    CPX #$20                            ; C807  e0 20
    BNE loc_30_C801                     ; C809  d0 f6
    RTS                                 ; C80B  60
loc_30_C80C:
    LDA #$0D                            ; C80C  a9 0d
    JSR SwitchBank16                    ; C80E  20 0e c4
    LDA #$03                            ; C811  a9 03
    JSR Bank26_FadeA                    ; C813  20 3d 8c
loc_30_C816:
    LSR frame_ready                     ; C816  46 09
    BCC loc_30_C816                     ; C818  90 fc
    LDA #$00                            ; C81A  a9 00
    STA irq_mode                        ; C81C  85 51
    LDX #$1E                            ; C81E  a2 1e
loc_30_C820:
    LSR frame_ready                     ; C820  46 09
    BCC loc_30_C820                     ; C822  90 fc
    DEX                                 ; C824  ca
    BNE loc_30_C820                     ; C825  d0 f9
    LDA #$03                            ; C827  a9 03
    JSR Bank26_FadeB                    ; C829  20 49 8c
    LDA #$08                            ; C82C  a9 08
    JMP SwitchBank16                    ; C82E  4c 0e c4
sub_30_C831:
    PHA                                 ; C831  48
    LDA cur_bank16_x2                   ; C832  a5 6e
    PHA                                 ; C834  48
    LDA r7_shadow                       ; C835  a5 6f
    PHA                                 ; C837  48
    INC pos_y_hi                        ; C838  e6 37
    LDA #$1E                            ; C83A  a9 1e
    STA tmp_2E                          ; C83C  85 2e
loc_30_C83E:
    JSR WaitPpuQueue                    ; C83E  20 76 c6
    LDA #$1A                            ; C841  a9 1a
    JSR SetR6                           ; C843  20 18 c4
    LDA #$00                            ; C846  a9 00
    STA tmp_30                          ; C848  85 30
    LDA #$F8                            ; C84A  a9 f8
    STA tmp_31                          ; C84C  85 31
    JSR TempAddOffset                   ; C84E  20 24 98
    JSR BgRowRender                     ; C851  20 91 eb
    DEC tmp_2E                          ; C854  c6 2e
    BNE loc_30_C83E                     ; C856  d0 e6
    PLA                                 ; C858  68
    JSR SetR7                           ; C859  20 27 c4
    PLA                                 ; C85C  68
    JSR SetR6                           ; C85D  20 18 c4
    PLA                                 ; C860  68
    BNE loc_30_C864                     ; C861  d0 01
    RTS                                 ; C863  60
loc_30_C864:
    JMP sub_30_C739                     ; C864  4c 39 c7
sub_30_C867:
    LDY #$04                            ; C867  a0 04
    LDA #$00                            ; C869  a9 00
loc_30_C86B:
    STA obj_active,Y                    ; C86B  99 a0 04
    INY                                 ; C86E  c8
    CPY #$0C                            ; C86F  c0 0c
    BNE loc_30_C86B                     ; C871  d0 f8
    RTS                                 ; C873  60
sub_30_C874:
    JSR WaitPpuQueue                    ; C874  20 76 c6
    LDX #$1C                            ; C877  a2 1c
    LDA #$0F                            ; C879  a9 0f
loc_30_C87B:
    STA pal_buf,X                       ; C87B  9d 40 61
    DEX                                 ; C87E  ca
    DEX                                 ; C87F  ca
    DEX                                 ; C880  ca
    DEX                                 ; C881  ca
    BPL loc_30_C87B                     ; C882  10 f7
    LDX #$0C                            ; C884  a2 0c
loc_30_C886:
    STA pal_buf,X                       ; C886  9d 40 61
    DEX                                 ; C889  ca
    BPL loc_30_C886                     ; C88A  10 fa
    LDX #$3F                            ; C88C  a2 3f
loc_30_C88E:
    LDA #$00                            ; C88E  a9 00
    STA wram_buf,X                      ; C890  9d 00 60
    DEX                                 ; C893  ca
    BPL loc_30_C88E                     ; C894  10 f8
    LDA #$2B                            ; C896  a9 2b
    JSR PpuQueueBlock                   ; C898  20 82 c4
    JMP WaitFrame                       ; C89B  4c 69 c1
sub_30_C89E:
    LDA area_id                         ; C89E  a5 6c
    CMP #$5F                            ; C8A0  c9 5f
    BNE loc_30_C8A8                     ; C8A2  d0 04
    LDA #$00                            ; C8A4  a9 00
    STA irq_mode                        ; C8A6  85 51
loc_30_C8A8:
    RTS                                 ; C8A8  60
sub_30_C8A9:
    LDA area_id                         ; C8A9  a5 6c
    CMP #$5F                            ; C8AB  c9 5f
    BNE loc_30_C8A8                     ; C8AD  d0 f9
    JMP SubstRestoreScreen              ; C8AF  4c 4d cc
sub_30_C8B2:
    LDA ppuctrl_shadow                  ; C8B2  a5 00
    PHA                                 ; C8B4  48
    AND #$7F                            ; C8B5  29 7f
    STA ppuctrl_shadow                  ; C8B7  85 00
    JSR PpuQueueFlush                   ; C8B9  20 7d c6
    PLA                                 ; C8BC  68
    STA ppuctrl_shadow                  ; C8BD  85 00
    RTS                                 ; C8BF  60
StaleText30_C8C0:  ; dead filler to C8FF: copy of 22:A8C0 text between RTS and MainLoop
    .db $65,$76,$65,$61,$6C,$86,$BD,$21,$20,$FB,$FD,$65,$6E,$6A,$6F,$79; 30:C8C0
    .db $02,$64,$65,$73,$74,$72,$6F,$79,$AC,$F3,$21,$20,$B0,$06,$10,$03; 30:C8D0
    .db $01,$20,$B0,$05,$02,$CF,$DA,$06,$17,$27,$73,$02,$66,$69,$6E,$65; 30:C8E0
    .db $73,$74,$20,$66,$6F,$75,$72,$2E,$20,$54,$68,$AD,$FD,$62,$65,$02; 30:C8F0
MainLoop:  ; wait $09 (NMI flag) then dispatch on state $40 via MainStateTable
    LDA frame_ready                     ; C900  a5 09
    BEQ MainLoop                        ; C902  f0 fc
    LDA main_state                      ; C904  a5 40
    ASL A                               ; C906  0a
    TAX                                 ; C907  aa
    LDA #$4C                            ; C908  a9 4c
    STA jmp_trampoline                  ; C90A  85 10
    LDA MainStateTable,X                ; C90C  bd 1f c9
    STA tmp_11                          ; C90F  85 11
    LDA $C920,X                         ; C911  bd 20 c9
    STA tmp_12                          ; C914  85 12
    JSR a:jmp_trampoline                ; C916  20 10 00
    LDA #$00                            ; C919  a9 00
    STA frame_ready                     ; C91B  85 09
    BEQ MainLoop                        ; C91D  f0 e1
MainStateTable:  ; s63: ptr16[9]: C957 CAB6 C931 C939 C944 C94C 0 0 C9DA
    .dw MainStateNewGame                ; 30:C91F
    .dw MainStateGameplay               ; 30:C921
    .dw MainStateTitleInit              ; 30:C923
    .dw MainStateTitle                  ; 30:C925
    .dw MainStateEndingInit             ; 30:C927
    .dw MainStateEnding                 ; 30:C929
    .dw $0000                           ; 30:C92B
    .dw $0000                           ; 30:C92D
    .dw MainStateContinue               ; 30:C92F
MainStateTitleInit:  ; s63: state 2: SwitchBank16(9) JMP 18:9FCC MainState2Init (reset / game over)
    LDA #$09                            ; C931  a9 09
    JSR SwitchBank16                    ; C933  20 0e c4
    JMP MainState2Init                  ; C936  4c cc 9f
MainStateTitle:  ; s63: state 3: ReadPadsMenu + JMP 19:A015 MainState3
    LDA #$09                            ; C939  a9 09
    JSR SwitchBank16                    ; C93B  20 0e c4
    JSR ReadPadsMenu                    ; C93E  20 e0 fe
    JMP MainState3                      ; C941  4c 15 a0
MainStateEndingInit:  ; s63: state 4: SwitchBank16(8) JMP 17:A000 EndingInit
    LDA #$08                            ; C944  a9 08
    JSR SwitchBank16                    ; C946  20 0e c4
    JMP EndingInit                      ; C949  4c 00 a0
MainStateEnding:  ; s63: state 5: ReadPadsMenu + JMP 17:A100 EndingTick
    LDA #$08                            ; C94C  a9 08
    JSR SwitchBank16                    ; C94E  20 0e c4
    JSR ReadPadsMenu                    ; C951  20 e0 fe
    JMP EndingTick                      ; C954  4c 00 a1
MainStateNewGame:  ; s63: state 0: new-game init (inventory/flags/stats) then main_state=1 sub_state=0 (30:C977)
    LDX #$4F                            ; C957  a2 4f
    LDA #$FF                            ; C959  a9 ff
loc_30_C95B:
    STA inv_select,X                    ; C95B  9d 28 64
    DEX                                 ; C95E  ca
    BPL loc_30_C95B                     ; C95F  10 fa
    LDX #$00                            ; C961  a2 00
    TXA                                 ; C963  8a
loc_30_C964:
    STA story_flags,X                   ; C964  9d 80 64
    STA npc_state,X                     ; C967  9d 80 65
    DEX                                 ; C96A  ca
    BNE loc_30_C964                     ; C96B  d0 f7
    LDA #$00                            ; C96D  a9 00
    STA area_id                         ; C96F  85 6c
    LDA #$01                            ; C971  a9 01
    STA area_entry                      ; C973  85 6d
    LDA #$01                            ; C975  a9 01
    STA main_state                      ; C977  85 40
    LDA #$00                            ; C979  a9 00
    STA sub_state                       ; C97B  85 41
    JSR GameStartObjs                   ; C97D  20 ff c9
    LDA #$1E                            ; C980  a9 1e
    STA xp_next                         ; C982  8d 06 07
    LDA #$00                            ; C985  a9 00
    STA xp_next_hi                      ; C987  8d 07 07
    STA xp                              ; C98A  8d 04 07
    STA xp_hi                           ; C98D  8d 05 07
    STA gold                            ; C990  8d 02 07
    STA gold_hi                         ; C993  8d 03 07
    STA obj1_flash                      ; C996  8d 10 07
    STA use_item_sel                    ; C999  8d 15 07
    STA equip_accessory                 ; C99C  8d 16 07
    STA $0717                           ; C99F  8d 17 07
    STA equip_sword                     ; C9A2  8d 11 07
    STA equip_armor                     ; C9A5  8d 13 07
    STA equip_shield                    ; C9A8  8d 14 07
    STA magic_sel                       ; C9AB  8d 12 07
    STA equip_ball                      ; C9AE  8d 18 07
    STA sword_power                     ; C9B1  8d 19 07
    STA pal_nofade_mask                 ; C9B4  8d e8 07
    STA invuln_timer                    ; C9B7  8d 1a 07
    STA opel_revive                     ; C9BA  8d 43 07
    STA ram_0744                        ; C9BD  8d 44 07
    STA a:scroll_shadow0                ; C9C0  8d 02 00
    STA a:cam_x_hi                      ; C9C3  8d 03 00
    STA a:cam_y_lo                      ; C9C6  8d 04 00
    STA a:cam_y_hi                      ; C9C9  8d 05 00
    STA irq_mode                        ; C9CC  85 51
    LDA #$22                            ; C9CE  a9 22
    STA mp_cur                          ; C9D0  8d 08 07
    STA mp_max                          ; C9D3  8d 09 07
    JSR sub_30_C008                     ; C9D6  20 08 c0
    RTS                                 ; C9D9  60
MainStateContinue:  ; s63: state 8: GameStartObjs + LoadGameAndInit 23:BC06 + 19:BA65 then main_state=1 sub_state=1
    JSR GameStartObjs                   ; C9DA  20 ff c9
    LDA #$17                            ; C9DD  a9 17
    JSR SetR7                           ; C9DF  20 27 c4
    JSR sub_23_BC06                     ; C9E2  20 06 bc
    JSR WaitPpuQueue                    ; C9E5  20 76 c6
    JSR sub_30_C008                     ; C9E8  20 08 c0
    LDA #$09                            ; C9EB  a9 09
    JSR SwitchBank16                    ; C9ED  20 0e c4
    JSR sub_19_BA65                     ; C9F0  20 65 ba
    LDA #$01                            ; C9F3  a9 01
    STA main_state                      ; C9F5  85 40
    STA sub_state                       ; C9F7  85 41
    LDA #$00                            ; C9F9  a9 00
    STA pal_nofade_mask                 ; C9FB  8d e8 07
    RTS                                 ; C9FE  60
GameStartObjs:  ; s63: obj_active=0; PPU mask/irq_mode 0; WaitFrame; ObjInit fixed slots from 30:CA26 (3:$0A 0:$06 2:$05 1:$04)
    LDA #$00                            ; C9FF  a9 00
    LDX #$1F                            ; CA01  a2 1f
loc_30_CA03:
    STA obj_active,X                    ; CA03  9d a0 04
    DEX                                 ; CA06  ca
    BPL loc_30_CA03                     ; CA07  10 fa
    LDA #$00                            ; CA09  a9 00
    STA ppumask_shadow                  ; CA0B  85 01
    STA irq_mode                        ; CA0D  85 51
    JSR WaitFrame                       ; CA0F  20 69 c1
    LDY #$06                            ; CA12  a0 06
loc_30_CA14:
    LDA GameStartObjTable,Y             ; CA14  b9 26 ca
    STA jmp_trampoline                  ; CA17  85 10
    LDA $CA27,Y                         ; CA19  b9 27 ca
    STA tmp_11                          ; CA1C  85 11
    JSR ObjInit                         ; CA1E  20 5d c2
    DEY                                 ; CA21  88
    DEY                                 ; CA22  88
    BPL loc_30_CA14                     ; CA23  10 ef
    RTS                                 ; CA25  60
GameStartObjTable:  ; s63: 4 x (slot type) pairs read backwards by GameStartObjs
    .db $01,$04,$02,$05,$00,$06,$03,$0A ; 30:CA26
SubstAreaEnter:  ; s60: substate 1: AreaEnter 31:E3E9 + ObjBgProbe slot 1; sub_state=8; area_entry&$E0=$40 -> re-dispatch at once
    JSR AreaEnter                       ; CA2E  20 e9 e3
    LDA #$1A                            ; CA31  a9 1a
    JSR SetR6                           ; CA33  20 18 c4
    LDX #$01                            ; CA36  a2 01
    JSR ObjPosToTemp                    ; CA38  20 a8 98
    LDA #$40                            ; CA3B  a9 40
    JSR ObjBgProbe                      ; CA3D  20 30 9a
    LDA #$00                            ; CA40  a9 00
    STA irq_mode                        ; CA42  85 51
    STA pal_nofade_mask                 ; CA44  8d e8 07
    LDA obj_flags,X                     ; CA47  bd 80 03
    AND #$BF                            ; CA4A  29 bf
    STA obj_flags,X                     ; CA4C  9d 80 03
    LDA #$08                            ; CA4F  a9 08
    STA sub_state                       ; CA51  85 41
    LDA area_entry                      ; CA53  a5 6d
    AND #$E0                            ; CA55  29 e0
    CMP #$40                            ; CA57  c9 40
    BNE loc_30_CA5E                     ; CA59  d0 03
    JMP MainStateGameplay               ; CA5B  4c b6 ca
loc_30_CA5E:
    RTS                                 ; CA5E  60
SubstNewGameInit:  ; s38: substate 0; sets substate $13 then falls into CAA2: if any button held that frame (pad_prev) -> substate 8 (skips the wake-up cut-scene)
    LDA #$09                            ; CA5F  a9 09
    JSR SwitchBank16                    ; CA61  20 0e c4
    JSR sub_19_BA65                     ; CA64  20 65 ba
    JSR WaitPpuQueue                    ; CA67  20 76 c6
    LDA #$1A                            ; CA6A  a9 1a
    JSR SetR6                           ; CA6C  20 18 c4
    JSR HudHpBar                        ; CA6F  20 c0 8c
    JSR WaitPpuQueue                    ; CA72  20 76 c6
    LDA #$0F                            ; CA75  a9 0f
    STA pal_buf                         ; CA77  8d 40 61
    LDA #$AF                            ; CA7A  a9 af
    STA pal_ids                         ; CA7C  8d e0 07
    STA pal_id_bg1                      ; CA7F  8d e1 07
    STA pal_id_bg2                      ; CA82  8d e2 07
    STA pal_id_bg3                      ; CA85  8d e3 07
    LDA #$4F                            ; CA88  a9 4f
    STA pal_id_spr0                     ; CA8A  8d e4 07
    STA pal_id_spr1                     ; CA8D  8d e5 07
    STA pal_id_spr2                     ; CA90  8d e6 07
    STA pal_id_spr3                     ; CA93  8d e7 07
    LDA #$FF                            ; CA96  a9 ff
    STA pal_nofade_mask                 ; CA98  8d e8 07
    JSR SubstAreaEnter                  ; CA9B  20 2e ca
    LDA #$13                            ; CA9E  a9 13
    STA sub_state                       ; CAA0  85 41
    LDA #$00                            ; CAA2  a9 00
    STA pal_nofade_mask                 ; CAA4  8d e8 07
    JSR ReadPads                        ; CAA7  20 80 fe
    LDA pad_prev                        ; CAAA  a5 43
    BEQ loc_30_CAB5                     ; CAAC  f0 07
    LDA #$08                            ; CAAE  a9 08
    STA sub_state                       ; CAB0  85 41
    JMP sub_30_C008                     ; CAB2  4c 08 c0
loc_30_CAB5:
    RTS                                 ; CAB5  60
MainStateGameplay:  ; s63: state 1: gameplay; substate $41 dispatcher
    LDA sub_state                       ; CAB6  a5 41
    CMP #$08                            ; CAB8  c9 08
    BEQ loc_30_CAC3                     ; CABA  f0 07
    CMP #$07                            ; CABC  c9 07
    BEQ loc_30_CAC3                     ; CABE  f0 03
    JSR PlayPaletteRestore              ; CAC0  20 2e cc
loc_30_CAC3:
    DEC tmp_08                          ; CAC3  c6 08
    LDA sub_state                       ; CAC5  a5 41
    ASL A                               ; CAC7  0a
    TAX                                 ; CAC8  aa
    LDA SubstateHandlerTable,X          ; CAC9  bd de ca
    STA jmp_trampoline                  ; CACC  85 10
    LDA $CADF,X                         ; CACE  bd df ca
    STA tmp_11                          ; CAD1  85 11
    LDX sub_state                       ; CAD3  a6 41
    LDA SubstateBankTable,X             ; CAD5  bd 2e cb
    JSR SwitchBank16                    ; CAD8  20 0e c4
    JMP (jmp_trampoline)                ; CADB  6c 10 00
SubstateHandlerTable:  ; ptr16[40] state-1 handlers indexed by $41 (CAB6 dispatcher; JMP ($0010)); s60: all named; 10 null
    .dw SubstNewGameInit                ; 30:CADE
    .dw SubstAreaEnter                  ; 30:CAE0
    .dw $0000                           ; 30:CAE2
    .dw PlayerDeath                     ; 30:CAE4
    .dw SubstDebugPause                 ; 30:CAE6
    .dw $0000                           ; 30:CAE8
    .dw ItemUseMenu                     ; 30:CAEA
    .dw GiveItem                        ; 30:CAEC
    .dw SubstPlay                       ; 30:CAEE
    .dw $0000                           ; 30:CAF0
    .dw $0000                           ; 30:CAF2
    .dw $0000                           ; 30:CAF4
    .dw PauseScreen                     ; 30:CAF6
    .dw PauseWait                       ; 30:CAF8
    .dw $0000                           ; 30:CAFA
    .dw $0000                           ; 30:CAFC
    .dw ShowPendingMessage              ; 30:CAFE
    .dw NpcTalk                         ; 30:CB00
    .dw SubstEquipMenu                  ; 30:CB02
    .dw SubstWakeCutscene               ; 30:CB04
    .dw SubstTeleportMenu               ; 30:CB06
    .dw SubstTelepathySelect            ; 30:CB08
    .dw SubstTelepathyReply             ; 30:CB0A
    .dw SubstChangeForm                 ; 30:CB0C
    .dw SubstCureMagic                  ; 30:CB0E
    .dw SubstFormRevert                 ; 30:CB10
    .dw SwordRaisePose                  ; 30:CB12
    .dw SubstMesiaReunion               ; 30:CB14
    .dw SubstOutOfMp                    ; 30:CB16
    .dw SubstTeleportMenu               ; 30:CB18
    .dw SubstFinalSceneReset            ; 30:CB1A
    .dw SubstRestoreScreen              ; 30:CB1C
    .dw SubstArmorShop                  ; 30:CB1E
    .dw SubstToolShop                   ; 30:CB20
    .dw SubstInn                        ; 30:CB22
    .dw SubstPawnShop                   ; 30:CB24
    .dw SubstShopEmpty                  ; 30:CB26
    .dw $0000                           ; 30:CB28
    .dw $0000                           ; 30:CB2A
    .dw $0000                           ; 30:CB2C
SubstateBankTable:  ; u8[40] SwitchBank16 n per substate $41 (s60: 3 24-27 30 -> 9; 12/13 -> 7; rest 0)
    .db $00,$00,$00,$09,$00,$00,$00,$00,$00,$00,$00,$00,$07,$07,$00,$00; 30:CB2E
    .db $00,$00,$00,$00,$00,$00,$00,$00,$09,$09,$09,$09,$00,$00,$09,$00; 30:CB3E
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 30:CB4E
    .db $00,$00,$00,$00                 ; 30:CB5E
SubstPlay:  ; sub_state 8: normal gameplay frame (ReadPads + objects + sprite pass + HUD)
    JSR ReadPads                        ; CB62  20 80 fe
    JSR PlayDeathMenuCheck              ; CB65  20 84 cb
    JSR PlayerStatusTick                ; CB68  20 55 ef
    JSR ObjUpdateAll                    ; CB6B  20 cc cc
    JSR CameraTick                      ; CB6E  20 f6 e8
    LDA #$0E                            ; CB71  a9 0e
    JSR SwitchBank16                    ; CB73  20 0e c4
    JSR ObjSpriteDraw                   ; CB76  20 0e 80
    LDA #$0D                            ; CB79  a9 0d
    JSR SwitchBank16                    ; CB7B  20 0e c4
    JSR ObjCollideAll                   ; CB7E  20 6e 8f
    JMP SubstPlayTail                   ; CB81  4c b8 e0
PlayDeathMenuCheck:  ; s46: SubstPlay body: player_hp=0 -> sub_state 3 (death); then START/SELECT menus
    LDA player_hp                       ; CB84  ad c1 03
    BNE loc_30_CB90                     ; CB87  d0 07
    LDA #$03                            ; CB89  a9 03
    STA sub_state                       ; CB8B  85 41
    PLA                                 ; CB8D  68
    PLA                                 ; CB8E  68
loc_30_CB8F:
    RTS                                 ; CB8F  60
loc_30_CB90:
    LDA pad_act                         ; CB90  a5 4d
    AND #$10                            ; CB92  29 10
    BEQ loc_30_CBB4                     ; CB94  f0 1e
    JSR PlayPaletteRestore              ; CB96  20 2e cc
    LDA area_id                         ; CB99  a5 6c
    CMP #$5F                            ; CB9B  c9 5f
    BNE loc_30_CBA3                     ; CB9D  d0 04
    LDA #$00                            ; CB9F  a9 00
    STA irq_mode                        ; CBA1  85 51
loc_30_CBA3:
    LDA #$07                            ; CBA3  a9 07
    JSR SwitchBank16                    ; CBA5  20 0e c4
    JSR PauseScreen                     ; CBA8  20 40 bc
    LDA area_id                         ; CBAB  a5 6c
    CMP #$5F                            ; CBAD  c9 5f
    BNE loc_30_CB8F                     ; CBAF  d0 de
    JMP SubstRestoreScreen              ; CBB1  4c 4d cc
loc_30_CBB4:
    LDA pad_new                         ; CBB4  a5 4b
    AND #$20                            ; CBB6  29 20
    BEQ loc_30_CBC1                     ; CBB8  f0 07
    LDA #$12                            ; CBBA  a9 12
    STA sub_state                       ; CBBC  85 41
    PLA                                 ; CBBE  68
    PLA                                 ; CBBF  68
    RTS                                 ; CBC0  60
loc_30_CBC1:
    LDA zp_4C                           ; CBC1  a5 4c
    AND #$40                            ; CBC3  29 40
    BNE SubstDebugPause                 ; CBC5  d0 35
    LDA pad_prev                        ; CBC7  a5 43
    CMP #$C0                            ; CBC9  c9 c0
    BNE loc_30_CBEB                     ; CBCB  d0 1e
    LDA zp_4C                           ; CBCD  a5 4c
    AND #$80                            ; CBCF  29 80
    BEQ loc_30_CBEB                     ; CBD1  f0 18
    LDA ram_0780                        ; CBD3  ad 80 07
    AND #$0F                            ; CBD6  29 0f
    TAY                                 ; CBD8  a8
    LDA AreaByIndexTable,Y              ; CBD9  b9 ec cb
    STA area_id                         ; CBDC  85 6c
    LDA #$00                            ; CBDE  a9 00
    STA area_entry                      ; CBE0  85 6d
    INC ram_0780                        ; CBE2  ee 80 07
    LDA #$01                            ; CBE5  a9 01
    STA sub_state                       ; CBE7  85 41
    PLA                                 ; CBE9  68
    PLA                                 ; CBEA  68
loc_30_CBEB:
    RTS                                 ; CBEB  60
AreaByIndexTable:  ; 16 area ids indexed by $0780&15 (s60: debug area cycler: pad-1 A+B held + pad-2 A in play
    .db $02,$03,$04,$14,$1A,$20,$40,$42,$60,$64,$72,$78,$7C,$90,$A8,$98; 30:CBEC
SubstDebugPause:  ; s60: substate 4 (never stored): pad-2 B freeze loop until pad-2 B again; entered by branch from PlayDeathMenuCheck
    JSR WaitFrame                       ; CBFC  20 69 c1
    JSR FarCall_B14_800E                ; CBFF  20 8c ff
    JSR ReadPadsMenu                    ; CC02  20 e0 fe
    LDA zp_4C                           ; CC05  a5 4c
    AND #$40                            ; CC07  29 40
    BEQ SubstDebugPause                 ; CC09  f0 f1
    LDA #$08                            ; CC0B  a9 08
    STA sub_state                       ; CC0D  85 41
    LDY #$08                            ; CC0F  a0 08
    JSR sub_30_C176                     ; CC11  20 76 c1
    JMP ReadPads                        ; CC14  4c 80 fe
SubstOutOfMp:  ; s60: substate 28: SFX $4E + pending msg 32.14; sub_state=$10
    JSR sub_30_C89E                     ; CC17  20 9e c8
    LDA #$4E                            ; CC1A  a9 4e
    JSR PlaySound                       ; CC1C  20 25 c1
    LDA #$10                            ; CC1F  a9 10
    STA sub_state                       ; CC21  85 41
    LDA #$0E                            ; CC23  a9 0e
    STA pending_msg_id                  ; CC25  8d c3 06
    LDA #$20                            ; CC28  a9 20
    STA pending_msg_group               ; CC2A  8d e3 06
    RTS                                 ; CC2D  60
PlayPaletteRestore:  ; s60: if slot 4 obj_active=$1A: clear it + PaletteBuild; irq_mode 2 -> 0 (run before state 1 unless substate 7/8)
    LDA obj_active_s04                  ; CC2E  ad a4 04
    CMP #$1A                            ; CC31  c9 1a
    BNE loc_30_CC4C                     ; CC33  d0 17
    LDA #$00                            ; CC35  a9 00
    STA obj_active_s04                  ; CC37  8d a4 04
    LDA #$1A                            ; CC3A  a9 1a
    JSR SetR6                           ; CC3C  20 18 c4
    JSR PaletteBuild                    ; CC3F  20 0e 8c
    LDA irq_mode                        ; CC42  a5 51
    CMP #$02                            ; CC44  c9 02
    BNE loc_30_CC4C                     ; CC46  d0 04
    LDA #$00                            ; CC48  a9 00
    STA irq_mode                        ; CC4A  85 51
loc_30_CC4C:
    RTS                                 ; CC4C  60
SubstRestoreScreen:  ; s60: substate 31: restore pos from $3C-$3F; HUD window rebuild 19:BA65; queue blocks 3 and $3E-$43; BG+spr on; sub_state=8
    JSR WaitPpuQueue                    ; CC4D  20 76 c6
    LDA ppumask_shadow                  ; CC50  a5 01
    AND #$E7                            ; CC52  29 e7
    STA ppumask_shadow                  ; CC54  85 01
    JSR WaitFrame                       ; CC56  20 69 c1
    LDA zp_3C                           ; CC59  a5 3c
    STA pos_x_lo                        ; CC5B  85 34
    LDA zp_3D                           ; CC5D  a5 3d
    STA pos_x_hi                        ; CC5F  85 35
    LDA zp_3E                           ; CC61  a5 3e
    STA pos_y_lo                        ; CC63  85 36
    LDA zp_3F                           ; CC65  a5 3f
    STA pos_y_hi                        ; CC67  85 37
    LDA #$08                            ; CC69  a9 08
    STA ram_0744                        ; CC6B  8d 44 07
    LDA #$01                            ; CC6E  a9 01
    JSR sub_30_C831                     ; CC70  20 31 c8
    LDA #$00                            ; CC73  a9 00
    STA ram_0744                        ; CC75  8d 44 07
    LDA #$09                            ; CC78  a9 09
    JSR SwitchBank16                    ; CC7A  20 0e c4
    JSR sub_19_BA65                     ; CC7D  20 65 ba
    JSR WaitFrame                       ; CC80  20 69 c1
    LDY #$3F                            ; CC83  a0 3f
    LDA #$FF                            ; CC85  a9 ff
loc_30_CC87:
    STA wram_6060,Y                     ; CC87  99 60 60
    DEY                                 ; CC8A  88
    BPL loc_30_CC87                     ; CC8B  10 fa
    LDY #$2F                            ; CC8D  a0 2f
loc_30_CC8F:
    LDA attr_shadow,Y                   ; CC8F  b9 00 61
    STA wram_6060,Y                     ; CC92  99 60 60
    DEY                                 ; CC95  88
    BPL loc_30_CC8F                     ; CC96  10 f7
    LDA #$03                            ; CC98  a9 03
    JSR PpuQueueBlock                   ; CC9A  20 82 c4
    LDA #$0B                            ; CC9D  a9 0b
    JSR SetR6                           ; CC9F  20 18 c4
    LDY #$00                            ; CCA2  a0 00
loc_30_CCA4:
    LDA $9E00,Y                         ; CCA4  b9 00 9e
    STA wram_buf,Y                      ; CCA7  99 00 60
    INY                                 ; CCAA  c8
    BNE loc_30_CCA4                     ; CCAB  d0 f7
    LDA #$3E                            ; CCAD  a9 3e
    STA tmp_2F                          ; CCAF  85 2f
loc_30_CCB1:
    LDA tmp_2F                          ; CCB1  a5 2f
    JSR PpuQueueBlock                   ; CCB3  20 82 c4
    INC tmp_2F                          ; CCB6  e6 2f
    LDA tmp_2F                          ; CCB8  a5 2f
    CMP #$44                            ; CCBA  c9 44
    BNE loc_30_CCB1                     ; CCBC  d0 f3
    LDA ppumask_shadow                  ; CCBE  a5 01
    ORA #$18                            ; CCC0  09 18
    STA ppumask_shadow                  ; CCC2  85 01
    JSR WaitFrame                       ; CCC4  20 69 c1
    LDA #$08                            ; CCC7  a9 08
    STA sub_state                       ; CCC9  85 41
    RTS                                 ; CCCB  60
ObjUpdateAll:  ; banks 26/27; slots 0-30: $05A0 timer (hit-flag clear) then JSR ObjAiTable[obj_active&$7F]; ids $60-$6F map bank 15 at $A000; id $1A -> 30:CD7F
    LDX #$00                            ; CCCC  a2 00
    STX obj_active_s1F                  ; CCCE  8e bf 04
loc_30_CCD1:
    LDA #$0D                            ; CCD1  a9 0d
    JSR SwitchBank16                    ; CCD3  20 0e c4
    LDA obj_flags2,X                    ; CCD6  bd 40 03
    AND #$F0                            ; CCD9  29 f0
    BNE loc_30_CD31                     ; CCDB  d0 54
loc_30_CCDD:
    LDA obj_freeze,X                    ; CCDD  bd a0 05
    BEQ loc_30_CCFA                     ; CCE0  f0 18
    DEC obj_freeze,X                    ; CCE2  de a0 05
    BNE loc_30_CD2B                     ; CCE5  d0 44
    LDA obj_spawn_b2,X                  ; CCE7  bd 40 05
    CMP #$01                            ; CCEA  c9 01
    BNE loc_30_CCFA                     ; CCEC  d0 0c
    LDA #$00                            ; CCEE  a9 00
    STA tmp_12                          ; CCF0  85 12
    LDA obj_spawn_param,X               ; CCF2  bd 60 05
    STA tmp_13                          ; CCF5  85 13
    JSR HitSetFlag                      ; CCF7  20 11 90
loc_30_CCFA:
    LDA obj_active,X                    ; CCFA  bd a0 04
    AND #$7F                            ; CCFD  29 7f
    CMP #$1A                            ; CCFF  c9 1a
    BNE loc_30_CD06                     ; CD01  d0 03
    JMP loc_30_CD7F                     ; CD03  4c 7f cd
loc_30_CD06:
    ASL A                               ; CD06  0a
    TAY                                 ; CD07  a8
    LDA #$4C                            ; CD08  a9 4c
    STA jmp_trampoline                  ; CD0A  85 10
    LDA ObjAiTable,Y                    ; CD0C  b9 14 a3
    STA tmp_11                          ; CD0F  85 11
    LDA $A315,Y                         ; CD11  b9 15 a3
    STA tmp_12                          ; CD14  85 12
    ORA tmp_11                          ; CD16  05 11
    BEQ loc_30_CD2B                     ; CD18  f0 11
    LDA obj_active,X                    ; CD1A  bd a0 04
    AND #$70                            ; CD1D  29 70
    CMP #$60                            ; CD1F  c9 60
    BNE ObjUpdateAllCall                ; CD21  d0 05
    LDA #$0F                            ; CD23  a9 0f
    JSR SetR7                           ; CD25  20 27 c4
ObjUpdateAllCall:  ; s55: JSR $0010 (JMP trampoline $10-$12 = 4C + ObjAiTable[id]); an AI that JMPs to AiShootCheck returns from ObjUpdateAll here
    JSR a:jmp_trampoline                ; CD28  20 10 00
loc_30_CD2B:
    INX                                 ; CD2B  e8
    CPX #$1F                            ; CD2C  e0 1f
    BNE loc_30_CCD1                     ; CD2E  d0 a1
    RTS                                 ; CD30  60
loc_30_CD31:
    LDA obj_flags2,X                    ; CD31  bd 40 03
    CLC                                 ; CD34  18
    ADC #$10                            ; CD35  69 10
    STA obj_flags2,X                    ; CD37  9d 40 03
    AND #$F0                            ; CD3A  29 f0
    BEQ loc_30_CD74                     ; CD3C  f0 36
    CPX #$01                            ; CD3E  e0 01
    BNE loc_30_CD45                     ; CD40  d0 03
    JMP loc_30_CCDD                     ; CD42  4c dd cc
loc_30_CD45:
    LDA obj_active,X                    ; CD45  bd a0 04
    BMI loc_30_CD74                     ; CD48  30 2a
    LDA obj_flags2,X                    ; CD4A  bd 40 03
    PHA                                 ; CD4D  48
    LDA obj_meta_dir,X                  ; CD4E  bd 60 03
    PHA                                 ; CD51  48
    LSR A                               ; CD52  4a
    LSR A                               ; CD53  4a
    LSR A                               ; CD54  4a
    LSR A                               ; CD55  4a
    STA obj_meta_dir,X                  ; CD56  9d 60 03
    LDA #$08                            ; CD59  a9 08
    STA obj_flags2,X                    ; CD5B  9d 40 03
    LDA #$1A                            ; CD5E  a9 1a
    JSR SetR6                           ; CD60  20 18 c4
    JSR ObjMoveSlide                    ; CD63  20 d7 98
    DEC obj_timer,X                     ; CD66  de 80 04
    PLA                                 ; CD69  68
    STA obj_meta_dir,X                  ; CD6A  9d 60 03
    PLA                                 ; CD6D  68
    STA obj_flags2,X                    ; CD6E  9d 40 03
    JMP loc_30_CD2B                     ; CD71  4c 2b cd
loc_30_CD74:
    LDA obj_meta_dir,X                  ; CD74  bd 60 03
    AND #$0F                            ; CD77  29 0f
    STA obj_meta_dir,X                  ; CD79  9d 60 03
    JMP loc_30_CCDD                     ; CD7C  4c dd cc
loc_30_CD7F:
    JSR sub_30_CD8A                     ; CD7F  20 8a cd
    LDA #$0D                            ; CD82  a9 0d
    JSR SwitchBank16                    ; CD84  20 0e c4
    JMP loc_30_CD2B                     ; CD87  4c 2b cd
sub_30_CD8A:
    JSR sub_30_CDD5                     ; CD8A  20 d5 cd
    TXA                                 ; CD8D  8a
    PHA                                 ; CD8E  48
    JSR WaitPpuQueue                    ; CD8F  20 76 c6
    JSR sub_30_CE07                     ; CD92  20 07 ce
    LDA irq_mode                        ; CD95  a5 51
    PHA                                 ; CD97  48
    LDA obj_phase,X                     ; CD98  bd 00 06
    JSR sub_30_CE12                     ; CD9B  20 12 ce
    PLA                                 ; CD9E  68
    STA irq_mode                        ; CD9F  85 51
    PLA                                 ; CDA1  68
    TAX                                 ; CDA2  aa
    INC obj_phase,X                     ; CDA3  fe 00 06
    LDA obj_phase,X                     ; CDA6  bd 00 06
    SEC                                 ; CDA9  38
    SBC #$6A                            ; CDAA  e9 6a
    BNE loc_30_CDC0                     ; CDAC  d0 12
    STA obj_active,X                    ; CDAE  9d a0 04
    JSR WaitFrame                       ; CDB1  20 69 c1
    LDA #$00                            ; CDB4  a9 00
    STA irq_mode                        ; CDB6  85 51
    LDA #$1A                            ; CDB8  a9 1a
    JSR SetR6                           ; CDBA  20 18 c4
    JMP PaletteBuild                    ; CDBD  4c 0e 8c
loc_30_CDC0:
    LDA irq_mode                        ; CDC0  a5 51
    BEQ loc_30_CDD4                     ; CDC2  f0 10
    LDA obj_flags_s01                   ; CDC4  ad 81 03
    ORA #$10                            ; CDC7  09 10
    STA obj_flags_s01                   ; CDC9  8d 81 03
    LDA obj_flags                       ; CDCC  ad 80 03
    ORA #$10                            ; CDCF  09 10
    LDA obj_flags                       ; CDD1  ad 80 03
loc_30_CDD4:
    RTS                                 ; CDD4  60
sub_30_CDD5:
    LDA #$1A                            ; CDD5  a9 1a
    JSR SetR6                           ; CDD7  20 18 c4
    LDY #$00                            ; CDDA  a0 00
    LDA #$1E                            ; CDDC  a9 1e
    JSR SpawnShot                       ; CDDE  20 2d 97
    BCC loc_30_CE06                     ; CDE1  90 23
    STX tmp_11                          ; CDE3  86 11
    LDA obj_phase,X                     ; CDE5  bd 00 06
    AND #$0F                            ; CDE8  29 0f
    CLC                                 ; CDEA  18
    ADC #$0D                            ; CDEB  69 0d
    TAY                                 ; CDED  a8
    LDX jmp_trampoline                  ; CDEE  a6 10
    LDA a:obj_x_lo,Y                    ; CDF0  b9 70 00
    STA obj_x_lo,X                      ; CDF3  95 70
    LDA a:obj_x_hi,Y                    ; CDF5  b9 90 00
    STA obj_x_hi,X                      ; CDF8  95 90
    LDA a:obj_y_lo,Y                    ; CDFA  b9 b0 00
    STA obj_y_lo,X                      ; CDFD  95 b0
    LDA a:obj_y_hi,Y                    ; CDFF  b9 d0 00
    STA obj_y_hi,X                      ; CE02  95 d0
    LDX tmp_11                          ; CE04  a6 11
loc_30_CE06:
    RTS                                 ; CE06  60
sub_30_CE07:
    LDA #$7F                            ; CE07  a9 7f
    LDY #$BF                            ; CE09  a0 bf
loc_30_CE0B:
    STA wram_buf,Y                      ; CE0B  99 00 60
    DEY                                 ; CE0E  88
    BMI loc_30_CE0B                     ; CE0F  30 fa
    RTS                                 ; CE11  60
sub_30_CE12:
    STA jmp_trampoline                  ; CE12  85 10
    ASL A                               ; CE14  0a
    CLC                                 ; CE15  18
    ADC jmp_trampoline                  ; CE16  65 10
    TAY                                 ; CE18  a8
    LDA $CF47,Y                         ; CE19  b9 47 cf
    STA tmp_20                          ; CE1C  85 20
    LDA $CF48,Y                         ; CE1E  b9 48 cf
    STA tmp_21                          ; CE21  85 21
    LDA $CF49,Y                         ; CE23  b9 49 cf
    STA tmp_22                          ; CE26  85 22
    BCC loc_30_CE39                     ; CE28  90 0f
    LDA $D047,Y                         ; CE2A  b9 47 d0
    STA tmp_20                          ; CE2D  85 20
    LDA $D048,Y                         ; CE2F  b9 48 d0
    STA tmp_21                          ; CE32  85 21
    LDA $D049,Y                         ; CE34  b9 49 d0
    STA tmp_22                          ; CE37  85 22
loc_30_CE39:
    LDA #$00                            ; CE39  a9 00
    JSR SwitchBank16                    ; CE3B  20 0e c4
    LDA tmp_20                          ; CE3E  a5 20
    BPL loc_30_CEB0                     ; CE40  10 6e
    TAX                                 ; CE42  aa
    LDA tmp_21                          ; CE43  a5 21
    STA tmp_11                          ; CE45  85 11
    BEQ loc_30_CEAF                     ; CE47  f0 66
    LDA #$F0                            ; CE49  a9 f0
    STA jmp_trampoline                  ; CE4B  85 10
    LDA tmp_22                          ; CE4D  a5 22
    PHA                                 ; CE4F  48
    LDY #$00                            ; CE50  a0 00
    JSR sub_30_CE89                     ; CE52  20 89 ce
    JSR sub_30_CE89                     ; CE55  20 89 ce
    PLA                                 ; CE58  68
    JSR PpuQueueBlock                   ; CE59  20 82 c4
    LDA tmp_08                          ; CE5C  a5 08
    LSR A                               ; CE5E  4a
    BCC loc_30_CE69                     ; CE5F  90 08
    LDA #$1A                            ; CE61  a9 1a
    JSR SetR6                           ; CE63  20 18 c4
    JMP PaletteBuild                    ; CE66  4c 0e 8c
loc_30_CE69:
    LDX #$1F                            ; CE69  a2 1f
    LDA #$0F                            ; CE6B  a9 0f
loc_30_CE6D:
    STA pal_buf,X                       ; CE6D  9d 40 61
    DEX                                 ; CE70  ca
    BNE loc_30_CE6D                     ; CE71  d0 fa
    LDX #$0C                            ; CE73  a2 0c
    LDA #$30                            ; CE75  a9 30
loc_30_CE77:
    STA $6141,X                         ; CE77  9d 41 61
    STA $6142,X                         ; CE7A  9d 42 61
    STA $6143,X                         ; CE7D  9d 43 61
    DEX                                 ; CE80  ca
    DEX                                 ; CE81  ca
    DEX                                 ; CE82  ca
    DEX                                 ; CE83  ca
    BPL loc_30_CE77                     ; CE84  10 f1
    JMP WaitFrame                       ; CE86  4c 69 c1
sub_30_CE89:
    LDA #$08                            ; CE89  a9 08
    STA tmp_12                          ; CE8B  85 12
loc_30_CE8D:
    LDA (jmp_trampoline),Y              ; CE8D  b1 10
    LSR A                               ; CE8F  4a
    LSR A                               ; CE90  4a
    LSR A                               ; CE91  4a
    LSR A                               ; CE92  4a
    CLC                                 ; CE93  18
    ADC #$70                            ; CE94  69 70
    STA wram_buf,X                      ; CE96  9d 00 60
    LDA (jmp_trampoline),Y              ; CE99  b1 10
    AND #$0F                            ; CE9B  29 0f
    CLC                                 ; CE9D  18
    ADC #$70                            ; CE9E  69 70
    STA wram_6001,X                     ; CEA0  9d 01 60
    INX                                 ; CEA3  e8
    INX                                 ; CEA4  e8
    INY                                 ; CEA5  c8
    DEC tmp_12                          ; CEA6  c6 12
    BNE loc_30_CE8D                     ; CEA8  d0 e3
    TXA                                 ; CEAA  8a
    CLC                                 ; CEAB  18
    ADC #$10                            ; CEAC  69 10
    TAX                                 ; CEAE  aa
loc_30_CEAF:
    RTS                                 ; CEAF  60
loc_30_CEB0:
    CMP #$00                            ; CEB0  c9 00
    BNE loc_30_CEE0                     ; CEB2  d0 2c
    LDA #$00                            ; CEB4  a9 00
    JSR SwitchBank16                    ; CEB6  20 0e c4
    LDA tmp_22                          ; CEB9  a5 22
    BEQ loc_30_CEC0                     ; CEBB  f0 03
    JSR sub_30_CF28                     ; CEBD  20 28 cf
loc_30_CEC0:
    LDA tmp_21                          ; CEC0  a5 21
    STA tmp_11                          ; CEC2  85 11
    LDA #$F0                            ; CEC4  a9 f0
    STA jmp_trampoline                  ; CEC6  85 10
    LDY #$00                            ; CEC8  a0 00
loc_30_CECA:
    LDA (jmp_trampoline),Y              ; CECA  b1 10
    STA pal_buf,Y                       ; CECC  99 40 61
    INC tmp_11                          ; CECF  e6 11
    LDA (jmp_trampoline),Y              ; CED1  b1 10
    STA wram_6150,Y                     ; CED3  99 50 61
    DEC tmp_11                          ; CED6  c6 11
    INY                                 ; CED8  c8
    CPY #$10                            ; CED9  c0 10
    BNE loc_30_CECA                     ; CEDB  d0 ed
    JMP WaitFrame                       ; CEDD  4c 69 c1
loc_30_CEE0:
    CMP #$01                            ; CEE0  c9 01
    BNE loc_30_CF13                     ; CEE2  d0 2f
    JSR WaitFrame                       ; CEE4  20 69 c1
    LDA #$43                            ; CEE7  a9 43
    JSR PlaySound                       ; CEE9  20 25 c1
    JSR sub_30_CF28                     ; CEEC  20 28 cf
    LDX #$0F                            ; CEEF  a2 0f
loc_30_CEF1:
    LDA #$00                            ; CEF1  a9 00
    STA wram_6060,X                     ; CEF3  9d 60 60
    LDA #$55                            ; CEF6  a9 55
    STA $6070,X                         ; CEF8  9d 70 60
    LDA #$AA                            ; CEFB  a9 aa
    STA $6078,X                         ; CEFD  9d 78 60
    STA $6088,X                         ; CF00  9d 88 60
    LDA #$FF                            ; CF03  a9 ff
    STA $6090,X                         ; CF05  9d 90 60
    DEX                                 ; CF08  ca
    BPL loc_30_CEF1                     ; CF09  10 e6
    LDA #$03                            ; CF0B  a9 03
    JSR PpuQueueBlock                   ; CF0D  20 82 c4
    JMP WaitFrame                       ; CF10  4c 69 c1
loc_30_CF13:
    LDA irq_mode                        ; CF13  a5 51
    CMP #$02                            ; CF15  c9 02
    BNE loc_30_CF1D                     ; CF17  d0 04
    LDA #$00                            ; CF19  a9 00
    STA irq_mode                        ; CF1B  85 51
loc_30_CF1D:
    LDA #$1A                            ; CF1D  a9 1a
    JSR SetR6                           ; CF1F  20 18 c4
    JSR PaletteBuild                    ; CF22  20 0e 8c
    JMP WaitFrame                       ; CF25  4c 69 c1
sub_30_CF28:
    LDA #$02                            ; CF28  a9 02
    STA irq_mode                        ; CF2A  85 51
    LDA #$34                            ; CF2C  a9 34
    STA chr_r0_alt                      ; CF2E  85 58
    STA chr_r1_alt                      ; CF30  85 59
    LDA chr_r2_play                     ; CF32  ad f2 07
    STA chr_r2_alt                      ; CF35  85 5a
    LDA chr_r3_play                     ; CF37  ad f3 07
    STA chr_r3_alt                      ; CF3A  85 5b
    LDA chr_r4_play                     ; CF3C  ad f4 07
    STA chr_r4_alt                      ; CF3F  85 5c
    LDA chr_r5_play                     ; CF41  ad f5 07
    STA chr_r5_alt                      ; CF44  85 5d
    RTS                                 ; CF46  60
    .db $88,$80,$14,$88,$81,$15,$88,$82,$16,$88,$83,$17,$88,$84,$18,$88; 30:CF47
    .db $85,$19,$88,$86,$1A,$88,$87,$1B,$88,$BF,$1C,$88,$BF,$1D,$88,$BF; 30:CF57
    .db $1E,$88,$BF,$1F,$01,$00,$00,$00,$98,$01,$00,$9A,$01,$00,$9C,$01; 30:CF67
    .db $00,$9E,$01,$00,$A0,$01,$00,$A2,$01,$00,$A4,$01,$00,$A6,$01,$00; 30:CF77
    .db $A4,$01,$00,$A6,$01,$00,$A4,$01,$00,$A6,$01,$00,$A4,$01,$00,$A6; 30:CF87
    .db $01,$00,$A4,$01,$00,$A6,$01,$00,$A8,$01,$00,$AA,$01,$00,$AC,$01; 30:CF97
    .db $00,$AE,$01,$02,$00,$01,$80,$88,$14,$80,$89,$15,$80,$8A,$16,$80; 30:CFA7
    .db $8B,$17,$80,$8C,$18,$80,$8D,$19,$80,$8E,$1A,$80,$8F,$1B,$01,$00; 30:CFB7
    .db $00,$00,$98,$01,$00,$9A,$01,$00,$9C,$01,$00,$9E,$01,$00,$A0,$01; 30:CFC7
    .db $00,$A2,$01,$00,$A4,$01,$00,$A6,$01,$00,$A4,$01,$00,$A6,$01,$00; 30:CFD7
    .db $A4,$01,$00,$A6,$01,$00,$A4,$01,$00,$A6,$01,$00,$A4,$01,$00,$A6; 30:CFE7
    .db $01,$00,$A8,$01,$00,$AA,$01,$00,$AC,$01,$00,$AE,$01,$02,$00,$01; 30:CFF7
    .db $88,$BF,$14,$88,$BF,$15,$90,$90,$16,$90,$91,$17,$90,$92,$18,$90; 30:D007
    .db $93,$19,$90,$94,$1A,$90,$95,$1B,$90,$96,$1C,$90,$97,$1D,$01,$00; 30:D017
    .db $00,$00,$98,$01,$00,$9A,$01,$00,$9C,$01,$00,$9E,$01,$00,$A0,$01; 30:D027
    .db $00,$A2,$01,$00,$A4,$01,$00,$A6,$01,$00,$A4,$01,$00,$A6,$01,$00; 30:D037
    .db $A4,$01,$00,$A6,$01,$00,$A4,$01,$00,$A6,$01,$00,$A4,$01,$00,$A6; 30:D047
    .db $01,$00,$A4,$01,$00,$A6,$01,$00,$A4,$01,$00,$A6,$01,$00,$A4,$01; 30:D057
    .db $00,$A6,$01,$00,$A8,$01,$00,$A8,$01,$00,$AA,$01,$00,$AA,$01,$00; 30:D067
    .db $AC,$01,$00,$AC,$01,$00,$AE,$01,$00,$AE,$01,$02,$00,$00; 30:D077
ShowPendingMessage:  ; show msg ($06C3 $06E3) set by entity code; restores $51
    JSR sub_30_C89E                     ; D085  20 9e c8
    LDA irq_mode                        ; D088  a5 51
    PHA                                 ; D08A  48
    LDY #$20                            ; D08B  a0 20
    JSR sub_30_C176                     ; D08D  20 76 c1
    LDA pending_msg_id                  ; D090  ad c3 06
    CMP #$0D                            ; D093  c9 0d
    BNE loc_30_D0AE                     ; D095  d0 17
    LDA pending_msg_group               ; D097  ad e3 06
    CMP #$20                            ; D09A  c9 20
    BNE loc_30_D0AE                     ; D09C  d0 10
    LDA snd_cur_music                   ; D09E  ad 02 01
    ORA #$80                            ; D0A1  09 80
    STA snd_cur_music                   ; D0A3  8d 02 01
    LDA #$93                            ; D0A6  a9 93
    JSR PlaySound                       ; D0A8  20 25 c1
    JMP loc_30_D0B3                     ; D0AB  4c b3 d0
loc_30_D0AE:
    LDA #$29                            ; D0AE  a9 29
    JSR PlaySound                       ; D0B0  20 25 c1
loc_30_D0B3:
    LDA pending_msg_id                  ; D0B3  ad c3 06
    STA text_msg_id                     ; D0B6  8d de 07
    LDA pending_msg_group               ; D0B9  ad e3 06
    STA text_group                      ; D0BC  8d df 07
    JSR ShowMessage                     ; D0BF  20 54 d3
    PLA                                 ; D0C2  68
    STA irq_mode                        ; D0C3  85 51
    JMP sub_30_C8A9                     ; D0C5  4c a9 c8
NpcTalk:  ; touched spawn entry $0623 -> NpcScript -> ShowMessagePacked -> TalkActionTable
    LDA #$29                            ; D0C8  a9 29
    JSR PlaySound                       ; D0CA  20 25 c1
    LDY obj_timer2_s03                  ; D0CD  ac 23 06
    LDA obj_freeze,Y                    ; D0D0  b9 a0 05
    BEQ loc_30_D100                     ; D0D3  f0 2b
    LDA obj_spawn_b2,Y                  ; D0D5  b9 40 05
    CMP #$01                            ; D0D8  c9 01
    BNE loc_30_D0F3                     ; D0DA  d0 17
    LDA obj_spawn_param,Y               ; D0DC  b9 60 05
    CMP #$5E                            ; D0DF  c9 5e
    BEQ loc_30_D100                     ; D0E1  f0 1d
    CMP #$5F                            ; D0E3  c9 5f
    BEQ loc_30_D100                     ; D0E5  f0 19
    CMP #$62                            ; D0E7  c9 62
    BEQ loc_30_D100                     ; D0E9  f0 15
    CMP #$6D                            ; D0EB  c9 6d
    BEQ loc_30_D100                     ; D0ED  f0 11
    CMP #$6C                            ; D0EF  c9 6c
    BEQ loc_30_D100                     ; D0F1  f0 0d
loc_30_D0F3:
    LDA #$00                            ; D0F3  a9 00
    STA text_msg_id                     ; D0F5  8d de 07
    LDA #$1F                            ; D0F8  a9 1f
    STA text_group                      ; D0FA  8d df 07
    JMP ShowMessage                     ; D0FD  4c 54 d3
loc_30_D100:
    LDA obj_spawn_param,Y               ; D100  b9 60 05
    STA tmp_23                          ; D103  85 23
    LDA #$07                            ; D105  a9 07
    JSR SwitchBank16                    ; D107  20 0e c4
    JSR NpcScript                       ; D10A  20 00 80
    LDA tmp_22                          ; D10D  a5 22
    PHA                                 ; D10F  48
    JSR ShowMessagePacked               ; D110  20 47 d3
    PLA                                 ; D113  68
    ASL A                               ; D114  0a
    TAY                                 ; D115  a8
    LDA TalkActionTable,Y               ; D116  b9 23 d1
    STA jmp_trampoline                  ; D119  85 10
    LDA $D124,Y                         ; D11B  b9 24 d1
    STA tmp_11                          ; D11E  85 11
    JMP (jmp_trampoline)                ; D120  6c 10 00
TalkActionTable:  ; 32 words indexed by NPC script action $22
    .dw sub_30_D163                     ; 30:D123
    .dw sub_30_D19C                     ; 30:D125
    .dw NpcVanish                       ; 30:D127
    .dw sub_30_D225                     ; 30:D129
    .dw sub_30_D163                     ; 30:D12B
    .dw sub_30_D164                     ; 30:D12D
    .dw MakeFollower13                  ; 30:D12F
    .dw sub_30_D163                     ; 30:D131
    .dw sub_30_D63F                     ; 30:D133
    .dw TalkGiveGold100                 ; 30:D135
    .dw sub_30_D2F4                     ; 30:D137
    .dw sub_30_D163                     ; 30:D139
    .dw sub_30_D25A                     ; 30:D13B
    .dw MakeFollower13                  ; 30:D13D
    .dw sub_30_D163                     ; 30:D13F
    .dw sub_30_D163                     ; 30:D141
    .dw sub_30_D1EB                     ; 30:D143
    .dw sub_30_D21D                     ; 30:D145
    .dw sub_30_D163                     ; 30:D147
    .dw sub_30_D1B5                     ; 30:D149
    .dw sub_30_D18E                     ; 30:D14B
    .dw sub_30_D2D3                     ; 30:D14D
    .dw sub_30_D1CA                     ; 30:D14F
    .dw TalkFullRestore                 ; 30:D151
    .dw sub_30_D280                     ; 30:D153
    .dw sub_30_D2AE                     ; 30:D155
    .dw sub_30_D336                     ; 30:D157
    .dw sub_30_D341                     ; 30:D159
    .dw sub_30_D163                     ; 30:D15B
    .dw sub_30_D163                     ; 30:D15D
    .dw sub_30_D163                     ; 30:D15F
    .dw sub_30_D163                     ; 30:D161
sub_30_D163:
    RTS                                 ; D163  60
sub_30_D164:
    LDA #$00                            ; D164  a9 00
    STA obj_active_s0E                  ; D166  8d ae 04
    STA boss_cam_lock                   ; D169  8d d7 07
    RTS                                 ; D16C  60
TalkFullRestore:  ; HP $03C1=$03C0; MP $0708=$0709; HUD redraw; NpcVanish
    LDA #$00                            ; D16D  a9 00
    STA boss_cam_lock                   ; D16F  8d d7 07
    LDA player_hp_max                   ; D172  ad c0 03
    STA player_hp                       ; D175  8d c1 03
    LDA mp_max                          ; D178  ad 09 07
    STA mp_cur                          ; D17B  8d 08 07
    LDA #$1A                            ; D17E  a9 1a
    JSR SetR6                           ; D180  20 18 c4
    JSR HudHpBar                        ; D183  20 c0 8c
    LDA #$04                            ; D186  a9 04
    JSR HudRedraw                       ; D188  20 46 8e
    JMP NpcVanish                       ; D18B  4c 1c d3
sub_30_D18E:
    LDA #$48                            ; D18E  a9 48
    JSR GiveItemA                       ; D190  20 2b d2
    JMP TalkFullRestore                 ; D193  4c 6d d1
    .db $20,$8B,$D8,$4C,$48,$E1         ; 30:D196
sub_30_D19C:
    LDA obj_timer2_s03                  ; D19C  ad 23 06
    SEC                                 ; D19F  38
    SBC #$01                            ; D1A0  e9 01
    AND #$FE                            ; D1A2  29 fe
    CLC                                 ; D1A4  18
    ADC #$01                            ; D1A5  69 01
    STA jmp_trampoline                  ; D1A7  85 10
    LDA #$64                            ; D1A9  a9 64
    STA tmp_11                          ; D1AB  85 11
    JSR ObjInit                         ; D1AD  20 5d c2
    INC jmp_trampoline                  ; D1B0  e6 10
    JMP ObjInit                         ; D1B2  4c 5d c2
sub_30_D1B5:
    LDX obj_timer2_s03                  ; D1B5  ae 23 06
    JSR SpawnFxAtSlot                   ; D1B8  20 ff d1
    LDA #$A0                            ; D1BB  a9 a0
    STA obj_tile_ofs_s1E                ; D1BD  8d 3e 03
    JSR AreaLoadA                       ; D1C0  20 44 e1
    LDY obj_timer2_s03                  ; D1C3  ac 23 06
    INY                                 ; D1C6  c8
    JMP loc_30_D31F                     ; D1C7  4c 1f d3
sub_30_D1CA:
    LDA #$29                            ; D1CA  a9 29
    JSR PlaySound                       ; D1CC  20 25 c1
    LDA #$00                            ; D1CF  a9 00
    STA text_msg_id                     ; D1D1  8d de 07
    LDA #$13                            ; D1D4  a9 13
    STA text_group                      ; D1D6  8d df 07
    JSR ShowMessage                     ; D1D9  20 54 d3
    JSR AreaLoadA                       ; D1DC  20 44 e1
    LDX obj_timer2_s03                  ; D1DF  ae 23 06
    JSR SpawnFxAtSlot                   ; D1E2  20 ff d1
    LDA #$A0                            ; D1E5  a9 a0
    STA obj_tile_ofs_s1E                ; D1E7  8d 3e 03
    RTS                                 ; D1EA  60
sub_30_D1EB:
    LDA #$01                            ; D1EB  a9 01
    STA text_msg_id                     ; D1ED  8d de 07
    LDA #$0B                            ; D1F0  a9 0b
    STA text_group                      ; D1F2  8d df 07
    JSR ShowMessage                     ; D1F5  20 54 d3
    LDA #$45                            ; D1F8  a9 45
    JSR GiveItemA                       ; D1FA  20 2b d2
    LDX #$0E                            ; D1FD  a2 0e
SpawnFxAtSlot:  ; X=src slot: ObjInit type $DF in slot $1E at slot X position; AreaLoadB
    LDA #$1A                            ; D1FF  a9 1a
    JSR SetR6                           ; D201  20 18 c4
    JSR ObjPosToTemp                    ; D204  20 a8 98
    LDX #$1E                            ; D207  a2 1e
    STX jmp_trampoline                  ; D209  86 10
    JSR TempToObjPos                    ; D20B  20 97 98
    LDA #$DF                            ; D20E  a9 df
    STA tmp_11                          ; D210  85 11
    JSR ObjInit                         ; D212  20 5d c2
    LDA #$80                            ; D215  a9 80
    STA obj_tile_ofs,X                  ; D217  9d 20 03
    JMP AreaLoadB                       ; D21A  4c 48 e1
sub_30_D21D:
    LDY obj_timer2_s03                  ; D21D  ac 23 06
    LDA obj_param2,Y                    ; D220  b9 a0 06
    BNE GiveItemA                       ; D223  d0 06
sub_30_D225:
    LDY obj_timer2_s03                  ; D225  ac 23 06
    LDA obj_param1,Y                    ; D228  b9 80 06
GiveItemA:  ; A -> $057F (slot $1F spawn param) then GiveItem with $0623=$1F
    STA $057F                           ; D22B  8d 7f 05
    LDA obj_timer2_s03                  ; D22E  ad 23 06
    PHA                                 ; D231  48
    LDA #$1F                            ; D232  a9 1f
    STA obj_timer2_s03                  ; D234  8d 23 06
    JSR GiveItem                        ; D237  20 eb d3
    PLA                                 ; D23A  68
    STA obj_timer2_s03                  ; D23B  8d 23 06
    RTS                                 ; D23E  60
MakeFollower13:  ; slot 13 -> type $32 follower (talk act 6/13)
    LDA #$32                            ; D23F  a9 32
    STA obj_active_s0D                  ; D241  8d ad 04
    LDA #$00                            ; D244  a9 00
    STA obj_coll_s0D                    ; D246  8d ad 03
    STA obj_timer2_s0D                  ; D249  8d 2d 06
    STA $064D                           ; D24C  8d 4d 06
    LDA #$01                            ; D24F  a9 01
    STA obj_anim_s0D                    ; D251  8d ed 04
    LDA #$FF                            ; D254  a9 ff
    STA obj_phase_s0D                   ; D256  8d 0d 06
    RTS                                 ; D259  60
sub_30_D25A:
    LDY obj_timer2_s03                  ; D25A  ac 23 06
    LDA #$54                            ; D25D  a9 54
    STA obj_active,Y                    ; D25F  99 a0 04
    RTS                                 ; D262  60
TalkGiveGold100:  ; $0702+=100 (carry bug: INC $0702 not $0703)
    LDA #$3F                            ; D263  a9 3f
    JSR PlaySound                       ; D265  20 25 c1
    LDA #$64                            ; D268  a9 64
    CLC                                 ; D26A  18
    ADC gold                            ; D26B  6d 02 07
    STA gold                            ; D26E  8d 02 07
    BCC loc_30_D276                     ; D271  90 03
    INC gold                            ; D273  ee 02 07
loc_30_D276:
    LDA #$1A                            ; D276  a9 1a
    JSR SetR6                           ; D278  20 18 c4
    LDA #$01                            ; D27B  a9 01
    JMP HudRedraw                       ; D27D  4c 46 8e
sub_30_D280:
    LDA #$00                            ; D280  a9 00
    STA obj_timer2                      ; D282  8d 20 06
    LDA #$1A                            ; D285  a9 1a
    JSR SetR6                           ; D287  20 18 c4
    LDX #$1E                            ; D28A  a2 1e
    JSR ObjPosToTemp                    ; D28C  20 a8 98
    LDX #$00                            ; D28F  a2 00
    JSR TempToObjPos                    ; D291  20 97 98
    INX                                 ; D294  e8
    JSR TempToObjPos                    ; D295  20 97 98
    LDA obj1_flash                      ; D298  ad 10 07
    ORA #$40                            ; D29B  09 40
    STA obj1_flash                      ; D29D  8d 10 07
    LDA #$02                            ; D2A0  a9 02
    STA obj_spawn_param_s01             ; D2A2  8d 61 05
    STA player_bg_attr                  ; D2A5  8d 41 06
    LDA #$0C                            ; D2A8  a9 0c
    STA obj_active_s1E                  ; D2AA  8d be 04
    RTS                                 ; D2AD  60
sub_30_D2AE:
    LDA #$30                            ; D2AE  a9 30
    JSR GiveItemA                       ; D2B0  20 2b d2
    LDA #$32                            ; D2B3  a9 32
    STA obj_active_s0E                  ; D2B5  8d ae 04
    LDA #$00                            ; D2B8  a9 00
    STA $03AE                           ; D2BA  8d ae 03
    STA obj_timer2_s0E                  ; D2BD  8d 2e 06
    STA $064E                           ; D2C0  8d 4e 06
    LDA #$01                            ; D2C3  a9 01
    STA obj_anim_s0E                    ; D2C5  8d ee 04
    LDA #$FF                            ; D2C8  a9 ff
    STA $060E                           ; D2CA  8d 0e 06
    LDA #$0D                            ; D2CD  a9 0d
    STA obj_param1_s0E                  ; D2CF  8d 8e 06
    RTS                                 ; D2D2  60
sub_30_D2D3:
    LDA obj_active_s0E                  ; D2D3  ad ae 04
    CMP #$32                            ; D2D6  c9 32
    BEQ loc_30_D2F3                     ; D2D8  f0 19
    LDY #$0E                            ; D2DA  a0 0e
    STY obj_param1_s0D                  ; D2DC  8c 8d 06
    INY                                 ; D2DF  c8
    STY obj_param1_s0E                  ; D2E0  8c 8e 06
    LDA #$32                            ; D2E3  a9 32
    STA obj_active_s0D                  ; D2E5  8d ad 04
    STA obj_active_s0E                  ; D2E8  8d ae 04
    LDA #$01                            ; D2EB  a9 01
    STA obj_anim_s0D                    ; D2ED  8d ed 04
    STA obj_anim_s0E                    ; D2F0  8d ee 04
loc_30_D2F3:
    RTS                                 ; D2F3  60
sub_30_D2F4:
    LDA #$1A                            ; D2F4  a9 1a
    JSR SetR6                           ; D2F6  20 18 c4
    LDX obj_timer2_s03                  ; D2F9  ae 23 06
    JSR ObjPosToTemp                    ; D2FC  20 a8 98
    LDX #$1E                            ; D2FF  a2 1e
    STX jmp_trampoline                  ; D301  86 10
    LDA #$0F                            ; D303  a9 0f
    STA tmp_11                          ; D305  85 11
    JSR TempToObjPos                    ; D307  20 97 98
    JSR FarCall_C25D                    ; D30A  20 80 ff
    LDA #$39                            ; D30D  a9 39
    STA $057E                           ; D30F  8d 7e 05
    LDA #$02                            ; D312  a9 02
    STA $055E                           ; D314  8d 5e 05
    LDA #$03                            ; D317  a9 03
    STA $061E                           ; D319  8d 1e 06
NpcVanish:  ; Y=$0623: obj_active=$53 $0660=1 SFX $6D (entry D31F with Y set)
    LDY obj_timer2_s03                  ; D31C  ac 23 06
loc_30_D31F:
    LDA #$01                            ; D31F  a9 01
    STA obj_aux,Y                       ; D321  99 60 06
    LDA #$53                            ; D324  a9 53
    STA obj_active,Y                    ; D326  99 a0 04
    LDA #$00                            ; D329  a9 00
    STA obj_freeze,Y                    ; D32B  99 a0 05
    STA obj_coll,Y                      ; D32E  99 a0 03
    LDA #$6D                            ; D331  a9 6d
    JMP PlaySound                       ; D333  4c 25 c1
sub_30_D336:
    LDA #$09                            ; D336  a9 09
    JSR GiveItemA                       ; D338  20 2b d2
    LDA #$04                            ; D33B  a9 04
    STA obj_phase_s0D                   ; D33D  8d 0d 06
    RTS                                 ; D340  60
sub_30_D341:
    LDA #$06                            ; D341  a9 06
    STA obj_phase_s0D                   ; D343  8d 0d 06
    RTS                                 ; D346  60
ShowMessagePacked:  ; $21:$20 linear msg id -> $07DF/$07DE then ShowMessage
    LDA tmp_20                          ; D347  a5 20
    STA text_msg_id                     ; D349  8d de 07
    LDA tmp_21                          ; D34C  a5 21
    STA text_group                      ; D34E  8d df 07
    JSR TextIdUnpack                    ; D351  20 c2 d3
ShowMessage:  ; font CHR R0=$3C R1=$38; window open; TextPrintBox; Select=replay A/B=close
    LDA #$3C                            ; D354  a9 3c
    STA chr_r0_alt                      ; D356  85 58
    LDA #$38                            ; D358  a9 38
    STA chr_r1_alt                      ; D35A  85 59
    LDA #$14                            ; D35C  a9 14
    JSR SetR6                           ; D35E  20 18 c4
    JSR TextWindowOpen                  ; D361  20 00 85
    JSR WaitFrame                       ; D364  20 69 c1
    LDA #$01                            ; D367  a9 01
    LDY irq_mode                        ; D369  a4 51
    CPY #$07                            ; D36B  c0 07
    BNE loc_30_D371                     ; D36D  d0 02
    LDA #$09                            ; D36F  a9 09
loc_30_D371:
    STA irq_mode                        ; D371  85 51
    JSR FarCall_B14_800E                ; D373  20 8c ff
    JSR OamClipTop                      ; D376  20 7d c1
    LDA sub_state                       ; D379  a5 41
    CMP #$09                            ; D37B  c9 09
    BNE loc_30_D382                     ; D37D  d0 03
    JSR sub_30_D896                     ; D37F  20 96 d8
loc_30_D382:
    LDA #$14                            ; D382  a9 14
    JSR SetR6                           ; D384  20 18 c4
    JSR TextPrintBox                    ; D387  20 28 85
loc_30_D38A:
    LSR frame_ready                     ; D38A  46 09
    BCC loc_30_D38A                     ; D38C  90 fc
    JSR ReadPads                        ; D38E  20 80 fe
    JSR FarCall_B14_800E                ; D391  20 8c ff
    JSR OamClipTop                      ; D394  20 7d c1
    LDA pad_new                         ; D397  a5 4b
    AND #$20                            ; D399  29 20
    BEQ loc_30_D3A0                     ; D39B  f0 03
    JMP ShowMessage                     ; D39D  4c 54 d3
loc_30_D3A0:
    LDA pad_new                         ; D3A0  a5 4b
    AND #$C0                            ; D3A2  29 c0
    BEQ loc_30_D38A                     ; D3A4  f0 e4
    LDA #$08                            ; D3A6  a9 08
    STA tmp_20                          ; D3A8  85 20
loc_30_D3AA:
    JSR WaitFrame                       ; D3AA  20 69 c1
    JSR FarCall_B14_800E                ; D3AD  20 8c ff
    JSR OamClipTop                      ; D3B0  20 7d c1
    DEC tmp_20                          ; D3B3  c6 20
    BNE loc_30_D3AA                     ; D3B5  d0 f3
    LDA #$00                            ; D3B7  a9 00
    STA irq_mode                        ; D3B9  85 51
    STA pad_new                         ; D3BB  85 4b
    LDA #$08                            ; D3BD  a9 08
    STA sub_state                       ; D3BF  85 41
    RTS                                 ; D3C1  60
TextIdUnpack:  ; $07DF = $07DF*8 + ($07DE>>5); $07DE &= $1F
    LDA text_msg_id                     ; D3C2  ad de 07
    ASL A                               ; D3C5  0a
    ROL text_group                      ; D3C6  2e df 07
    ASL A                               ; D3C9  0a
    ROL text_group                      ; D3CA  2e df 07
    ASL A                               ; D3CD  0a
    ROL text_group                      ; D3CE  2e df 07
    LDA text_msg_id                     ; D3D1  ad de 07
    AND #$1F                            ; D3D4  29 1f
    STA text_msg_id                     ; D3D6  8d de 07
    RTS                                 ; D3D9  60
TrapChest:  ; item $70: ObjInit chest slot as type $73 (chest monster AI $2B 27:AC60); sub_state=8
    LDA obj_timer2_s03                  ; D3DA  ad 23 06
    STA jmp_trampoline                  ; D3DD  85 10
    LDA #$73                            ; D3DF  a9 73
    STA tmp_11                          ; D3E1  85 11
    JSR ObjInit                         ; D3E3  20 5d c2
    LDA #$08                            ; D3E6  a9 08
    STA sub_state                       ; D3E8  85 41
    RTS                                 ; D3EA  60
GiveItem:  ; item id obj_spawn_param Y -> $07DC; SFX; msg 32.12 You now have {ITEM}
    LDY obj_timer2_s03                  ; D3EB  ac 23 06
    LDA obj_spawn_param,Y               ; D3EE  b9 60 05
    STA ram_07DC                        ; D3F1  8d dc 07
    CMP #$80                            ; D3F4  c9 80
    BCC loc_30_D3FB                     ; D3F6  90 03
    JMP loc_30_D539                     ; D3F8  4c 39 d5
loc_30_D3FB:
    CMP #$70                            ; D3FB  c9 70
    BEQ TrapChest                       ; D3FD  f0 db
    LDA #$00                            ; D3FF  a9 00
    JSR ItemEventHook                   ; D401  20 34 d8
    LDA tmp_23                          ; D404  a5 23
    BMI GiveItemFull                    ; D406  30 74
    JSR SwordPowerLevel                 ; D408  20 d8 d9
    LDY obj_timer2_s03                  ; D40B  ac 23 06
    LDA obj_spawn_param,Y               ; D40E  b9 60 05
    CMP #$50                            ; D411  c9 50
    BCC loc_30_D41C                     ; D413  90 07
    TAY                                 ; D415  a8
    LDA ItemRemapTable,Y                ; D416  b9 0c d4
    STA ram_07DC                        ; D419  8d dc 07
loc_30_D41C:
    LDA ram_07DC                        ; D41C  ad dc 07
    AND #$F0                            ; D41F  29 f0
    CMP #$40                            ; D421  c9 40
    BNE loc_30_D435                     ; D423  d0 10
    LDA snd_cur_music                   ; D425  ad 02 01
    ORA #$80                            ; D428  09 80
    STA snd_cur_music                   ; D42A  8d 02 01
    LDA #$93                            ; D42D  a9 93
    JSR PlaySound                       ; D42F  20 25 c1
    JMP loc_30_D43A                     ; D432  4c 3a d4
loc_30_D435:
    LDA #$3F                            ; D435  a9 3f
    JSR PlaySound                       ; D437  20 25 c1
loc_30_D43A:
    LDA #$20                            ; D43A  a9 20
    STA text_group                      ; D43C  8d df 07
    LDA #$0C                            ; D43F  a9 0c
    STA text_msg_id                     ; D441  8d de 07
    LDA tmp_22                          ; D444  a5 22
    PHA                                 ; D446  48
    JSR ShowMessage                     ; D447  20 54 d3
    JSR PlayPaletteRestore              ; D44A  20 2e cc
    PLA                                 ; D44D  68
    STA tmp_22                          ; D44E  85 22
    LDY obj_timer2_s03                  ; D450  ac 23 06
    LDA #$00                            ; D453  a9 00
    STA obj_active,Y                    ; D455  99 a0 04
    JMP EventActionDispatch             ; D458  4c 52 d5
loc_30_D45B:
    RTS                                 ; D45B  60
    .db $1D,$12,$1D,$1D,$22,$1D,$1D,$1D,$22,$21,$21,$28,$1F,$1F,$1E,$1E; 30:D45C
    .db $1E,$21,$21,$26,$21,$22,$23,$22,$22,$22,$24,$22,$22,$26,$24,$22; 30:D46C
GiveItemFull:  ; GiveItem fail path: msg 32.15 too many items + SFX $29 if $08&$0F=0
    LDA tmp_08                          ; D47C  a5 08
    AND #$0F                            ; D47E  29 0f
    BNE loc_30_D45B                     ; D480  d0 d9
    JSR PlayPaletteRestore              ; D482  20 2e cc
    LDA #$0F                            ; D485  a9 0f
    STA text_msg_id                     ; D487  8d de 07
    LDA #$20                            ; D48A  a9 20
    STA text_group                      ; D48C  8d df 07
    LDA #$29                            ; D48F  a9 29
    JSR PlaySound                       ; D491  20 25 c1
    JMP ShowMessage                     ; D494  4c 54 d3
ItemUseMenu:  ; item menu use: $0715 checks ($08 teleport -> SaveGame) then ItemEventHook(1) -> EventActionDispatch
    LDA use_item_sel                    ; D497  ad 15 07
    CMP #$0A                            ; D49A  c9 0a
    BNE loc_30_D4AC                     ; D49C  d0 0e
    LDA opel_revive                     ; D49E  ad 43 07
    BNE loc_30_D4AC                     ; D4A1  d0 09
    INC tmp_08                          ; D4A3  e6 08
    LDA #$08                            ; D4A5  a9 08
    STA sub_state                       ; D4A7  85 41
    JMP MainStateGameplay               ; D4A9  4c b6 ca
loc_30_D4AC:
    LDA #$00                            ; D4AC  a9 00
    STA opel_revive                     ; D4AE  8d 43 07
    LDA use_item_sel                    ; D4B1  ad 15 07
    CMP #$20                            ; D4B4  c9 20
    BNE loc_30_D4C8                     ; D4B6  d0 10
    LDA obj1_flash                      ; D4B8  ad 10 07
    BPL loc_30_D4C3                     ; D4BB  10 06
    AND #$30                            ; D4BD  29 30
    CMP #$20                            ; D4BF  c9 20
    BEQ loc_30_D4C8                     ; D4C1  f0 05
loc_30_D4C3:
    LDA #$08                            ; D4C3  a9 08
    STA sub_state                       ; D4C5  85 41
    RTS                                 ; D4C7  60
loc_30_D4C8:
    LDA use_item_sel                    ; D4C8  ad 15 07
    CMP #$08                            ; D4CB  c9 08
    BNE sub_30_D4F2                     ; D4CD  d0 23
    BIT obj1_flash                      ; D4CF  2c 10 07
    BPL loc_30_D4D9                     ; D4D2  10 05
    LDA #$08                            ; D4D4  a9 08
    STA sub_state                       ; D4D6  85 41
    RTS                                 ; D4D8  60
loc_30_D4D9:
    LDA area_id                         ; D4D9  a5 6c
    PHA                                 ; D4DB  48
    JSR SubstTeleportMenu               ; D4DC  20 28 db
    PLA                                 ; D4DF  68
    CMP area_id                         ; D4E0  c5 6c
    BNE loc_30_D4E7                     ; D4E2  d0 03
    JMP loc_30_D45B                     ; D4E4  4c 5b d4
loc_30_D4E7:
    JSR sub_30_D4F2                     ; D4E7  20 f2 d4
    LDA #$17                            ; D4EA  a9 17
    JSR SetR7                           ; D4EC  20 27 c4
    JMP SaveJumpTable                   ; D4EF  4c 00 bc
sub_30_D4F2:
    LDA use_item_sel                    ; D4F2  ad 15 07
    CMP #$0B                            ; D4F5  c9 0b
    BEQ loc_30_D501                     ; D4F7  f0 08
    CMP #$0C                            ; D4F9  c9 0c
    BEQ loc_30_D501                     ; D4FB  f0 04
    CMP #$15                            ; D4FD  c9 15
    BNE loc_30_D506                     ; D4FF  d0 05
loc_30_D501:
    LDA #$48                            ; D501  a9 48
    JSR PlaySound                       ; D503  20 25 c1
loc_30_D506:
    LDA use_item_sel                    ; D506  ad 15 07
    CLC                                 ; D509  18
    ADC #$1C                            ; D50A  69 1c
    STA $0563                           ; D50C  8d 63 05
    LDA #$03                            ; D50F  a9 03
    STA obj_timer2_s03                  ; D511  8d 23 06
    INC tmp_08                          ; D514  e6 08
    LDA #$08                            ; D516  a9 08
    STA sub_state                       ; D518  85 41
    LDA #$01                            ; D51A  a9 01
    JSR ItemEventHook                   ; D51C  20 34 d8
    LDA tmp_23                          ; D51F  a5 23
    BMI ItemUseFail                     ; D521  30 03
    JMP EventActionDispatch             ; D523  4c 52 d5
ItemUseFail:  ; sub_state=$10; pending msg 32.19 Nothing happens; SFX $4E
    LDA #$10                            ; D526  a9 10
    STA sub_state                       ; D528  85 41
    LDA #$13                            ; D52A  a9 13
    STA pending_msg_id                  ; D52C  8d c3 06
    LDA #$20                            ; D52F  a9 20
    STA pending_msg_group               ; D531  8d e3 06
    LDA #$4E                            ; D534  a9 4e
    JMP PlaySound                       ; D536  4c 25 c1
loc_30_D539:
    INC sub_state                       ; D539  e6 41
    INC sub_state                       ; D53B  e6 41
    INC tmp_08                          ; D53D  e6 08
    LDA #$02                            ; D53F  a9 02
    JSR ItemEventHook                   ; D541  20 34 d8
    LDA tmp_23                          ; D544  a5 23
    BMI loc_30_D54B                     ; D546  30 03
    JSR EventActionDispatch             ; D548  20 52 d5
loc_30_D54B:
    LDA #$08                            ; D54B  a9 08
    STA sub_state                       ; D54D  85 41
    JMP MainStateGameplay               ; D54F  4c b6 ca
EventActionDispatch:  ; JMP via EventActionTable[$22]
    LDA tmp_22                          ; D552  a5 22
    ASL A                               ; D554  0a
    TAY                                 ; D555  a8
    LDA EventActionTable,Y              ; D556  b9 63 d5
    STA jmp_trampoline                  ; D559  85 10
    LDA $D564,Y                         ; D55B  b9 64 d5
    STA tmp_11                          ; D55E  85 11
    JMP (jmp_trampoline)                ; D560  6c 10 00
EventActionTable:  ; 32 words: action handlers for event triggers (ItemHook2 path)
    .dw sub_30_D5A3                     ; 30:D563
    .dw EvActThunderWarp                ; 30:D565
    .dw sub_30_D5D6                     ; 30:D567
    .dw sub_30_D5A3                     ; 30:D569
    .dw sub_30_D653                     ; 30:D56B
    .dw sub_30_D5A3                     ; 30:D56D
    .dw sub_30_D6D5                     ; 30:D56F
    .dw EvActMesiaMessage               ; 30:D571
    .dw sub_30_D654                     ; 30:D573
    .dw EvActSpawnType38                ; 30:D575
    .dw sub_30_D63A                     ; 30:D577
    .dw sub_30_D6D8                     ; 30:D579
    .dw sub_30_D6DD                     ; 30:D57B
    .dw sub_30_D6E7                     ; 30:D57D
    .dw sub_30_D6EC                     ; 30:D57F
    .dw sub_30_D70D                     ; 30:D581
    .dw sub_30_D63F                     ; 30:D583
    .dw sub_30_D6D5                     ; 30:D585
    .dw sub_30_D5A3                     ; 30:D587
    .dw EvActClearSlot13                ; 30:D589
    .dw EvActClearSlot14                ; 30:D58B
    .dw EvActSpawn9E                    ; 30:D58D
    .dw sub_30_D78C                     ; 30:D58F
    .dw EvActHistory                    ; 30:D591
    .dw EvActTwoFollowers               ; 30:D593
    .dw EvActPushBack                   ; 30:D595
    .dw sub_30_D5A3                     ; 30:D597
    .dw EvActMakeFollowers              ; 30:D599
    .dw sub_30_D7FD                     ; 30:D59B
    .dw EvActConsume                    ; 30:D59D
    .dw EvActEnding                     ; 30:D59F
    .dw sub_30_D82E                     ; 30:D5A1
sub_30_D5A3:
    RTS                                 ; D5A3  60
EvActThunderWarp:  ; act 1: msg 28.17 then warp to area $F2 entry $41
    LDA #$00                            ; D5A4  a9 00
    STA obj_phase                       ; D5A6  8d 00 06
    STA obj_timer2                      ; D5A9  8d 20 06
    STA orbit_level                     ; D5AC  8d c0 06
    LDA #$09                            ; D5AF  a9 09
    JSR SwitchBank16                    ; D5B1  20 0e c4
    JSR SwordRaisePose                  ; D5B4  20 04 bc
    LDA #$11                            ; D5B7  a9 11
    STA text_msg_id                     ; D5B9  8d de 07
    LDA #$1C                            ; D5BC  a9 1c
    STA text_group                      ; D5BE  8d df 07
    JSR ShowMessage                     ; D5C1  20 54 d3
    LDA #$23                            ; D5C4  a9 23
    JSR PlaySound                       ; D5C6  20 25 c1
    LDA #$F2                            ; D5C9  a9 f2
    STA area_id                         ; D5CB  85 6c
    LDA #$41                            ; D5CD  a9 41
    STA area_entry                      ; D5CF  85 6d
    LDA #$01                            ; D5D1  a9 01
    STA sub_state                       ; D5D3  85 41
    RTS                                 ; D5D5  60
sub_30_D5D6:
    LDA #$0D                            ; D5D6  a9 0d
    STA obj_timer2_s03                  ; D5D8  8d 23 06
    JSR sub_30_D21D                     ; D5DB  20 1d d2
    LDA #$88                            ; D5DE  a9 88
    STA obj_meta_id_s0D                 ; D5E0  8d 0d 03
    JMP MakeFollower13                  ; D5E3  4c 3f d2
EvActMesiaMessage:  ; act 7: slot 13 anim + type $DE fx + msg 14.5
    LDY obj_timer2_s03                  ; D5E6  ac 23 06
    LDA #$00                            ; D5E9  a9 00
    STA obj_active,Y                    ; D5EB  99 a0 04
    STA obj_coll_s0D                    ; D5EE  8d ad 03
    LDA #$D2                            ; D5F1  a9 d2
    STA obj_meta_id_s0D                 ; D5F3  8d 0d 03
    LDA #$20                            ; D5F6  a9 20
    STA obj_anim_s0D                    ; D5F8  8d ed 04
    JSR sub_30_D62E                     ; D5FB  20 2e d6
    LDA #$2E                            ; D5FE  a9 2e
    STA obj_meta_id_s0D                 ; D600  8d 0d 03
    STA ram_06CD                        ; D603  8d cd 06
    JSR FarCall_B14_800E                ; D606  20 8c ff
    LDA #$1A                            ; D609  a9 1a
    JSR SetR6                           ; D60B  20 18 c4
    LDX #$0D                            ; D60E  a2 0d
    JSR ObjPosToTemp                    ; D610  20 a8 98
    LDX #$1E                            ; D613  a2 1e
    STX jmp_trampoline                  ; D615  86 10
    LDA #$DE                            ; D617  a9 de
    STA tmp_11                          ; D619  85 11
    JSR FarCall_C25D                    ; D61B  20 80 ff
    JSR TempToObjPos                    ; D61E  20 97 98
    LDA #$05                            ; D621  a9 05
    STA text_msg_id                     ; D623  8d de 07
    LDA #$0E                            ; D626  a9 0e
    STA text_group                      ; D628  8d df 07
    JMP ShowMessage                     ; D62B  4c 54 d3
sub_30_D62E:
    JSR WaitFrame                       ; D62E  20 69 c1
    JSR FarCall_B14_800E                ; D631  20 8c ff
    DEC obj_anim_s0D                    ; D634  ce ed 04
    BNE sub_30_D62E                     ; D637  d0 f5
    RTS                                 ; D639  60
sub_30_D63A:
    LDA #$6E                            ; D63A  a9 6e
    JMP PlaySound                       ; D63C  4c 25 c1
sub_30_D63F:
    LDA obj_active_s0E                  ; D63F  ad ae 04
    CMP #$08                            ; D642  c9 08
    BEQ loc_30_D652                     ; D644  f0 0c
    LDA #$0E                            ; D646  a9 0e
    STA jmp_trampoline                  ; D648  85 10
    LDA $04CE                           ; D64A  ad ce 04
    STA tmp_11                          ; D64D  85 11
    JMP ObjInit                         ; D64F  4c 5d c2
loc_30_D652:
    RTS                                 ; D652  60
sub_30_D653:
    RTS                                 ; D653  60
sub_30_D654:
    LDA #$42                            ; D654  a9 42
    JMP GiveItemA                       ; D656  4c 2b d2
EvActSpawnType38:  ; act 9: ObjInit type $38 in slot $1E from EvType38Pos by area/entry
    BIT obj1_flash                      ; D659  2c 10 07
    BVS loc_30_D6A7                     ; D65C  70 49
    LDA obj_active_s1E                  ; D65E  ad be 04
    BNE loc_30_D6A7                     ; D661  d0 44
    LDY #$05                            ; D663  a0 05
    LDA area_id                         ; D665  a5 6c
    CMP #$64                            ; D667  c9 64
    BEQ loc_30_D679                     ; D669  f0 0e
    LDY #$0A                            ; D66B  a0 0a
    CMP #$68                            ; D66D  c9 68
    BEQ loc_30_D679                     ; D66F  f0 08
    LDA area_entry                      ; D671  a5 6d
    ASL A                               ; D673  0a
    ASL A                               ; D674  0a
    CLC                                 ; D675  18
    ADC area_entry                      ; D676  65 6d
    TAY                                 ; D678  a8
loc_30_D679:
    LDA EvType38Pos,Y                   ; D679  b9 a8 d6
    CMP #$FF                            ; D67C  c9 ff
    BEQ loc_30_D6A7                     ; D67E  f0 27
    STA zp_8E                           ; D680  85 8e
    LDA #$1E                            ; D682  a9 1e
    STA jmp_trampoline                  ; D684  85 10
    LDA #$38                            ; D686  a9 38
    STA tmp_11                          ; D688  85 11
    JSR ObjInit                         ; D68A  20 5d c2
    LDA $D6A9,Y                         ; D68D  b9 a9 d6
    STA $AE                             ; D690  85 ae
    LDA $D6AA,Y                         ; D692  b9 aa d6
    STA $CE                             ; D695  85 ce
    LDA $D6AB,Y                         ; D697  b9 ab d6
    STA $EE                             ; D69A  85 ee
    LDA $D6AC,Y                         ; D69C  b9 ac d6
    STA $069E                           ; D69F  8d 9e 06
    LDA #$48                            ; D6A2  a9 48
    JMP PlaySound                       ; D6A4  4c 25 c1
loc_30_D6A7:
    RTS                                 ; D6A7  60
EvType38Pos:  ; 9 x 5 bytes: x lo/hi y lo/hi $069E ($FF = none)
    .db $E8,$05,$B8,$07,$05,$A8,$01,$78,$00,$06,$A8,$01,$78,$00,$07,$FF; 30:D6A8
    .db $0F,$FF,$0F,$0F,$08,$05,$E8,$06,$08,$FF,$0F,$FF,$0F,$0F,$FF,$0F; 30:D6B8
    .db $FF,$0F,$0F,$F8,$03,$D8,$00,$09,$E8,$05,$B8,$07,$05; 30:D6C8
sub_30_D6D5:
    JMP AreaLoadB                       ; D6D5  4c 48 e1
sub_30_D6D8:
    LDA #$46                            ; D6D8  a9 46
    JMP GiveItemA                       ; D6DA  4c 2b d2
sub_30_D6DD:
    LDA #$47                            ; D6DD  a9 47
    JSR GiveItemA                       ; D6DF  20 2b d2
    LDY #$0E                            ; D6E2  a0 0e
    JMP loc_30_D31F                     ; D6E4  4c 1f d3
sub_30_D6E7:
    LDA #$3E                            ; D6E7  a9 3e
    JMP GiveItemA                       ; D6E9  4c 2b d2
sub_30_D6EC:
    JSR AreaLoadA                       ; D6EC  20 44 e1
    LDX #$0F                            ; D6EF  a2 0f
    LDA #$1A                            ; D6F1  a9 1a
    JSR SetR6                           ; D6F3  20 18 c4
    JSR ObjPosToTemp                    ; D6F6  20 a8 98
    LDX #$1E                            ; D6F9  a2 1e
    STX jmp_trampoline                  ; D6FB  86 10
    JSR TempToObjPos                    ; D6FD  20 97 98
    LDA #$DF                            ; D700  a9 df
    STA tmp_11                          ; D702  85 11
    JSR ObjInit                         ; D704  20 5d c2
    LDA #$A0                            ; D707  a9 a0
    STA obj_tile_ofs_s1E                ; D709  8d 3e 03
    RTS                                 ; D70C  60
sub_30_D70D:
    LDY obj_timer2_s03                  ; D70D  ac 23 06
    LDA #$41                            ; D710  a9 41
    STA obj_spawn_param,Y               ; D712  99 60 05
    JMP GiveItem                        ; D715  4c eb d3
EvActClearSlot13:  ; act 19: clear slot 13 then shared slot-15 spawn check
    LDA #$00                            ; D718  a9 00
    STA obj_active_s0D                  ; D71A  8d ad 04
    BEQ loc_30_D724                     ; D71D  f0 05
EvActClearSlot14:  ; act 20: clear slot 14; both empty -> $07D7=$7F + ObjInit slot 15 type $04CF
    LDA #$00                            ; D71F  a9 00
    STA obj_active_s0E                  ; D721  8d ae 04
loc_30_D724:
    LDA #$6B                            ; D724  a9 6b
    JSR PlaySound                       ; D726  20 25 c1
    JSR WaitSfxDone                     ; D729  20 8b d8
    LDA obj_active_s0D                  ; D72C  ad ad 04
    ORA obj_active_s0E                  ; D72F  0d ae 04
    BNE loc_30_D745                     ; D732  d0 11
    LDA #$7F                            ; D734  a9 7f
    STA boss_cam_lock                   ; D736  8d d7 07
    LDA $04CF                           ; D739  ad cf 04
    STA tmp_11                          ; D73C  85 11
    LDA #$0F                            ; D73E  a9 0f
    STA jmp_trampoline                  ; D740  85 10
    JMP ObjInit                         ; D742  4c 5d c2
loc_30_D745:
    RTS                                 ; D745  60
EvActSpawn9E:  ; act 21: stop music + ObjInit type $9E in slot 13
    LDA #$6B                            ; D746  a9 6b
    JSR PlaySound                       ; D748  20 25 c1
    LDA #$00                            ; D74B  a9 00
    JSR PlaySound                       ; D74D  20 25 c1
    JSR WaitSfxDone                     ; D750  20 8b d8
    LDA #$9E                            ; D753  a9 9e
    STA tmp_11                          ; D755  85 11
    LDA #$0D                            ; D757  a9 0d
    STA jmp_trampoline                  ; D759  85 10
    JMP ObjInit                         ; D75B  4c 5d c2
EvActTwoFollowers:  ; act 24: slots 13+14 -> type $32 followers
    LDY #$0B                            ; D75E  a0 0b
    STY obj_param1_s0D                  ; D760  8c 8d 06
    DEY                                 ; D763  88
    STY obj_param1_s0E                  ; D764  8c 8e 06
    LDA #$32                            ; D767  a9 32
    STA obj_active_s0D                  ; D769  8d ad 04
    STA obj_active_s0E                  ; D76C  8d ae 04
    LDA #$00                            ; D76F  a9 00
    STA obj_timer2_s0D                  ; D771  8d 2d 06
    STA obj_timer2_s0E                  ; D774  8d 2e 06
    STA $064D                           ; D777  8d 4d 06
    STA $064E                           ; D77A  8d 4e 06
    LDY obj_timer2_s03                  ; D77D  ac 23 06
    STA obj_active,Y                    ; D780  99 a0 04
    LDA #$01                            ; D783  a9 01
    STA obj_anim_s0D                    ; D785  8d ed 04
    STA obj_anim_s0E                    ; D788  8d ee 04
    RTS                                 ; D78B  60
sub_30_D78C:
    LDA #$1A                            ; D78C  a9 1a
    STA sub_state                       ; D78E  85 41
    RTS                                 ; D790  60
EvActHistory:  ; act 23: msgs 22.0/22.2/22.4/22.6
    JSR FarCall_B14_800E                ; D791  20 8c ff
    LDA obj_flags_s01                   ; D794  ad 81 03
    AND #$FB                            ; D797  29 fb
    STA obj_flags_s01                   ; D799  8d 81 03
    LDA #$00                            ; D79C  a9 00
    STA text_msg_id                     ; D79E  8d de 07
    LDA #$16                            ; D7A1  a9 16
    STA text_group                      ; D7A3  8d df 07
loc_30_D7A6:
    JSR ShowMessage                     ; D7A6  20 54 d3
    INC text_msg_id                     ; D7A9  ee de 07
    INC text_msg_id                     ; D7AC  ee de 07
    LDA text_msg_id                     ; D7AF  ad de 07
    CMP #$08                            ; D7B2  c9 08
    BNE loc_30_D7A6                     ; D7B4  d0 f0
    RTS                                 ; D7B6  60
EvActPushBack:  ; act 25: player y += 8 (carry bug: INC low byte)
    LDA obj_y_lo                        ; D7B7  a5 b0
    CLC                                 ; D7B9  18
    ADC #$08                            ; D7BA  69 08
    STA obj_y_lo                        ; D7BC  85 b0
    BCC loc_30_D7C2                     ; D7BE  90 02
    INC obj_y_lo                        ; D7C0  e6 b0
loc_30_D7C2:
    LDA obj_y_lo_s01                    ; D7C2  a5 b1
    CLC                                 ; D7C4  18
    ADC #$08                            ; D7C5  69 08
    STA obj_y_lo_s01                    ; D7C7  85 b1
    BCC loc_30_D7CD                     ; D7C9  90 02
    INC obj_y_lo_s01                    ; D7CB  e6 b1
loc_30_D7CD:
    RTS                                 ; D7CD  60
EvActMakeFollowers:  ; act 27: active slots 13-14 -> type $32 followers
    LDY obj_timer2_s03                  ; D7CE  ac 23 06
    LDA #$00                            ; D7D1  a9 00
    STA obj_active,Y                    ; D7D3  99 a0 04
    LDY #$0D                            ; D7D6  a0 0d
loc_30_D7D8:
    LDA obj_active,Y                    ; D7D8  b9 a0 04
    BEQ loc_30_D7F7                     ; D7DB  f0 1a
    CMP #$32                            ; D7DD  c9 32
    BEQ loc_30_D7F7                     ; D7DF  f0 16
    LDA obj_freeze,Y                    ; D7E1  b9 a0 05
    BNE loc_30_D7F7                     ; D7E4  d0 11
    TYA                                 ; D7E6  98
    SEC                                 ; D7E7  38
    SBC #$0B                            ; D7E8  e9 0b
    STA obj_param1,Y                    ; D7EA  99 80 06
    LDA #$32                            ; D7ED  a9 32
    STA obj_active,Y                    ; D7EF  99 a0 04
    LDA #$01                            ; D7F2  a9 01
    STA obj_anim,Y                      ; D7F4  99 e0 04
loc_30_D7F7:
    INY                                 ; D7F7  c8
    CPY #$0F                            ; D7F8  c0 0f
    BNE loc_30_D7D8                     ; D7FA  d0 dc
    RTS                                 ; D7FC  60
sub_30_D7FD:
    LDA #$29                            ; D7FD  a9 29
    JSR GiveItemA                       ; D7FF  20 2b d2
    JMP MakeFollower13                  ; D802  4c 3f d2
EvActConsume:  ; act 29: clear trigger slot; area $F2 spawns type $88 in slot 13
    LDY obj_timer2_s03                  ; D805  ac 23 06
    LDA #$00                            ; D808  a9 00
    STA obj_active,Y                    ; D80A  99 a0 04
    LDA area_id                         ; D80D  a5 6c
    CMP #$F2                            ; D80F  c9 f2
    BNE loc_30_D826                     ; D811  d0 13
    LDA #$80                            ; D813  a9 80
    STA $7D                             ; D815  85 7d
    LDA #$40                            ; D817  a9 40
    STA obj_y_lo_s0D                    ; D819  85 bd
    LDA #$0D                            ; D81B  a9 0d
    STA jmp_trampoline                  ; D81D  85 10
    LDA #$88                            ; D81F  a9 88
    STA tmp_11                          ; D821  85 11
    JMP FarCall_C25D                    ; D823  4c 80 ff
loc_30_D826:
    RTS                                 ; D826  60
EvActEnding:  ; act 30: sub_state=$1B and drop caller return
    LDA #$1B                            ; D827  a9 1b
    STA sub_state                       ; D829  85 41
    PLA                                 ; D82B  68
    PLA                                 ; D82C  68
    RTS                                 ; D82D  60
sub_30_D82E:
    LDA #$55                            ; D82E  a9 55
    STA obj_active_s0D                  ; D830  8d ad 04
    RTS                                 ; D833  60
ItemEventHook:  ; A=hook 0-2 -> bank14 word 30:D885[A] (826F/831E/80E3); shows $21:$20 msg
    PHA                                 ; D834  48
    LDY obj_timer2_s03                  ; D835  ac 23 06
    LDA obj_spawn_param,Y               ; D838  b9 60 05
    STA tmp_23                          ; D83B  85 23
    LDA #$07                            ; D83D  a9 07
    JSR SwitchBank16                    ; D83F  20 0e c4
    PLA                                 ; D842  68
    ASL A                               ; D843  0a
    TAY                                 ; D844  a8
    LDA #$4C                            ; D845  a9 4c
    STA jmp_trampoline                  ; D847  85 10
    LDA ItemEventHookTable,Y            ; D849  b9 85 d8
    STA tmp_11                          ; D84C  85 11
    LDA $D886,Y                         ; D84E  b9 86 d8
    STA tmp_12                          ; D851  85 12
    JSR a:jmp_trampoline                ; D853  20 10 00
    LDA tmp_23                          ; D856  a5 23
    BMI loc_30_D880                     ; D858  30 26
    LDA tmp_20                          ; D85A  a5 20
    ORA tmp_21                          ; D85C  05 21
    BEQ loc_30_D880                     ; D85E  f0 20
    LDA tmp_22                          ; D860  a5 22
    PHA                                 ; D862  48
    LDA tmp_23                          ; D863  a5 23
    PHA                                 ; D865  48
    JSR PlayPaletteRestore              ; D866  20 2e cc
    JSR sub_30_C89E                     ; D869  20 9e c8
    LDA #$29                            ; D86C  a9 29
    JSR PlaySound                       ; D86E  20 25 c1
    JSR ShowMessagePacked               ; D871  20 47 d3
    JSR sub_30_C008                     ; D874  20 08 c0
    JSR sub_30_C8A9                     ; D877  20 a9 c8
    PLA                                 ; D87A  68
    STA tmp_23                          ; D87B  85 23
    PLA                                 ; D87D  68
    STA tmp_22                          ; D87E  85 22
loc_30_D880:
    LDA #$08                            ; D880  a9 08
    STA sub_state                       ; D882  85 41
    RTS                                 ; D884  60
ItemEventHookTable:  ; 3 words: 14:826F 14:831E 14:80E3
    .dw ItemGetHook                     ; 30:D885
    .dw ItemUseHook                     ; 30:D887
    .dw ItemHook2                       ; 30:D889
WaitSfxDone:  ; WaitFrame until snd_sfx_priority == $FF
    JSR WaitFrame                       ; D88B  20 69 c1
    LDA snd_sfx_priority                ; D88E  ad 12 01
    CMP #$FF                            ; D891  c9 ff
    BNE WaitSfxDone                     ; D893  d0 f6
    RTS                                 ; D895  60
sub_30_D896:
    LDA #$10                            ; D896  a9 10
    STA tmp_20                          ; D898  85 20
loc_30_D89A:
    LDA #$00                            ; D89A  a9 00
    STA irq_mode                        ; D89C  85 51
    JSR WaitFrame                       ; D89E  20 69 c1
    LDA #$01                            ; D8A1  a9 01
    STA irq_mode                        ; D8A3  85 51
    JSR WaitFrame                       ; D8A5  20 69 c1
    DEC tmp_20                          ; D8A8  c6 20
    BNE loc_30_D89A                     ; D8AA  d0 ee
    RTS                                 ; D8AC  60
    .db $A9,$08,$85,$20,$20,$69,$C1,$20,$8C,$FF,$20,$7D,$C1,$C6,$20,$D0; 30:D8AD
    .db $F3                             ; 30:D8BD
loc_30_D8BE:
    LDA #$00                            ; D8BE  a9 00
    STA pad_new                         ; D8C0  85 4b
    LDA #$08                            ; D8C2  a9 08
    STA sub_state                       ; D8C4  85 41
    RTS                                 ; D8C6  60
SubstEquipMenu:  ; s60: substate 18 (SELECT): JSR 16:8000 menu; SelectedUseItem; SwordPowerLevel
    LDA area_id                         ; D8C7  a5 6c
    CMP #$5F                            ; D8C9  c9 5f
    BNE loc_30_D8D1                     ; D8CB  d0 04
    LDA #$00                            ; D8CD  a9 00
    STA irq_mode                        ; D8CF  85 51
loc_30_D8D1:
    LDA equip_sword                     ; D8D1  ad 11 07
    PHA                                 ; D8D4  48
    LDA irq_mode                        ; D8D5  a5 51
    PHA                                 ; D8D7  48
    LDA #$10                            ; D8D8  a9 10
    JSR SetR6                           ; D8DA  20 18 c4
    LDA #$17                            ; D8DD  a9 17
    JSR SetR7                           ; D8DF  20 27 c4
    JSR sub_16_8000                     ; D8E2  20 00 80
    LDY #$08                            ; D8E5  a0 08
    JSR WaitFrame                       ; D8E7  20 69 c1
    JSR ReadPads                        ; D8EA  20 80 fe
    LDA #$F0                            ; D8ED  a9 f0
    LDX #$00                            ; D8EF  a2 00
loc_30_D8F1:
    STA oam_y,X                         ; D8F1  9d 00 02
    INX                                 ; D8F4  e8
    BNE loc_30_D8F1                     ; D8F5  d0 fa
    JSR SelectedUseItem                 ; D8F7  20 0e db
    JSR SwordPowerLevel                 ; D8FA  20 d8 d9
    STY sword_power                     ; D8FD  8c 19 07
    PLA                                 ; D900  68
    STA irq_mode                        ; D901  85 51
    PLA                                 ; D903  68
    CMP equip_sword                     ; D904  cd 11 07
    BEQ loc_30_D90C                     ; D907  f0 03
    JSR sub_30_DAFC                     ; D909  20 fc da
loc_30_D90C:
    LDA area_id                         ; D90C  a5 6c
    CMP #$5F                            ; D90E  c9 5f
    BNE loc_30_D915                     ; D910  d0 03
    JSR SubstRestoreScreen              ; D912  20 4d cc
loc_30_D915:
    JSR sub_30_DADA                     ; D915  20 da da
    LDA menu_save_state                 ; D918  ad 00 70
    CMP #$FA                            ; D91B  c9 fa
    BEQ loc_30_D930                     ; D91D  f0 11
loc_30_D91F:
    JSR sub_30_C008                     ; D91F  20 08 c0
    JSR FarCall_B14_800E                ; D922  20 8c ff
    LDA #$1A                            ; D925  a9 1a
    JSR SetR6                           ; D927  20 18 c4
    JSR PaletteBuild                    ; D92A  20 0e 8c
    JMP loc_30_D8BE                     ; D92D  4c be d8
loc_30_D930:
    LDA #$00                            ; D930  a9 00
    STA menu_save_state                 ; D932  8d 00 70
    LDA ppumask_shadow                  ; D935  a5 01
    AND #$E7                            ; D937  29 e7
    STA ppumask_shadow                  ; D939  85 01
    JSR WaitFrame                       ; D93B  20 69 c1
    LDA player_level                    ; D93E  ad 21 04
    PHA                                 ; D941  48
    LDA player_hp                       ; D942  ad c1 03
    PHA                                 ; D945  48
    LDA player_hp_max                   ; D946  ad c0 03
    PHA                                 ; D949  48
    LDA #$01                            ; D94A  a9 01
    STA jmp_trampoline                  ; D94C  85 10
    LDA #$04                            ; D94E  a9 04
    STA tmp_11                          ; D950  85 11
    JSR ObjInit                         ; D952  20 5d c2
    LDA #$00                            ; D955  a9 00
    STA jmp_trampoline                  ; D957  85 10
    LDA #$06                            ; D959  a9 06
    STA tmp_11                          ; D95B  85 11
    JSR ObjInit                         ; D95D  20 5d c2
    PLA                                 ; D960  68
    STA player_hp_max                   ; D961  8d c0 03
    PLA                                 ; D964  68
    STA player_hp                       ; D965  8d c1 03
    PLA                                 ; D968  68
    STA player_level                    ; D969  8d 21 04
    LDA #$00                            ; D96C  a9 00
    STA pal_nofade_mask                 ; D96E  8d e8 07
    JSR sub_30_C008                     ; D971  20 08 c0
    LDA #$09                            ; D974  a9 09
    JSR SwitchBank16                    ; D976  20 0e c4
    JSR sub_19_BA65                     ; D979  20 65 ba
    LDA #$FF                            ; D97C  a9 ff
    STA area_entry                      ; D97E  85 6d
    LDA #$01                            ; D980  a9 01
    STA sub_state                       ; D982  85 41
    RTS                                 ; D984  60
SubstArmorShop:  ; s60: substate 32 (AI $0F types $42 $48): ShopWalkIn + 16:9500 + ShopWalkOut
    JSR ShopWalkIn                      ; D985  20 0c da
    JSR ShopEntryJumps                  ; D988  20 00 95
    JMP ShopWalkOut                     ; D98B  4c 72 da
SubstToolShop:  ; s60: substate 33 (types $41 $44 $47): 16:9503
    JSR ShopWalkIn                      ; D98E  20 0c da
    JSR sub_16_9503                     ; D991  20 03 95
    JMP ShopWalkOut                     ; D994  4c 72 da
SubstInn:  ; s60: substate 34 (types $40 $45 $46): 16:9506 then HP/MP HUD redraw
    JSR ShopWalkIn                      ; D997  20 0c da
    JSR sub_16_9506                     ; D99A  20 06 95
    LDA #$1A                            ; D99D  a9 1a
    JSR SetR6                           ; D99F  20 18 c4
    LDA #$04                            ; D9A2  a9 04
    JSR HudRedraw                       ; D9A4  20 46 8e
    JSR HudHpBar                        ; D9A7  20 c0 8c
    JMP ShopWalkOut                     ; D9AA  4c 72 da
SubstPawnShop:  ; s60: substate 35 (type $43): 16:9509
    JSR ShopWalkIn                      ; D9AD  20 0c da
    JSR sub_16_9509                     ; D9B0  20 09 95
    JMP ShopWalkOut                     ; D9B3  4c 72 da
SubstWakeCutscene:  ; s38: substate $13 handler: SwitchBank16(10) + JSR 20:9400 (self-timed cut-scene loop); then JMP 30:D91F
    LDA #$0A                            ; D9B6  a9 0a
    JSR SwitchBank16                    ; D9B8  20 0e c4
    JSR WakeCutscene                    ; D9BB  20 00 94
    JMP loc_30_D91F                     ; D9BE  4c 1f d9
SubstShopEmpty:  ; s60: substate 36 (AI $0E type $4E): msg 32.17 Nobody's here
    JSR ShopWalkIn                      ; D9C1  20 0c da
    LDA #$11                            ; D9C4  a9 11
    STA text_msg_id                     ; D9C6  8d de 07
    LDA #$20                            ; D9C9  a9 20
    STA text_group                      ; D9CB  8d df 07
    JSR ShowMessage                     ; D9CE  20 54 d3
    LDA #$24                            ; D9D1  a9 24
    STA sub_state                       ; D9D3  85 41
    JMP ShopWalkOut                     ; D9D5  4c 72 da
SwordPowerLevel:  ; Y=0/1/2 sword power from $0711 sword + $0718 ball/bracelet element match -> $0719
    LDY equip_sword                     ; D9D8  ac 11 07
    BEQ loc_30_D9FB                     ; D9DB  f0 1e
    LDA equip_ball                      ; D9DD  ad 18 07
    BEQ loc_30_D9FC                     ; D9E0  f0 1a
    DEY                                 ; D9E2  88
    STY jmp_trampoline                  ; D9E3  84 10
    SEC                                 ; D9E5  38
    SBC #$01                            ; D9E6  e9 01
    AND #$03                            ; D9E8  29 03
    CMP jmp_trampoline                  ; D9EA  c5 10
    BNE loc_30_DA03                     ; D9EC  d0 15
    LDA equip_ball                      ; D9EE  ad 18 07
    SEC                                 ; D9F1  38
    SBC #$01                            ; D9F2  e9 01
    LDY #$02                            ; D9F4  a0 02
    AND #$0C                            ; D9F6  29 0c
    BNE loc_30_D9FB                     ; D9F8  d0 01
    DEY                                 ; D9FA  88
loc_30_D9FB:
    RTS                                 ; D9FB  60
loc_30_D9FC:
    LDA $643B,Y                         ; D9FC  b9 3b 64
    CMP #$FF                            ; D9FF  c9 ff
    BNE loc_30_DA06                     ; DA01  d0 03
loc_30_DA03:
    LDY #$00                            ; DA03  a0 00
    RTS                                 ; DA05  60
loc_30_DA06:
    LSR A                               ; DA06  4a
    BCS loc_30_DA03                     ; DA07  b0 fa
    LDY #$01                            ; DA09  a0 01
    RTS                                 ; DA0B  60
ShopWalkIn:  ; s60: PaletteBuild + scripted walk down to y=$98 / right to x=$78; SwitchBank16(8)
    JSR ShopDoorOpenStub                ; DA0C  20 00 fe
    JSR sub_30_DAFC                     ; DA0F  20 fc da
    LDA #$1A                            ; DA12  a9 1a
    JSR SetR6                           ; DA14  20 18 c4
    JSR PaletteBuild                    ; DA17  20 0e 8c
    LDA #$3C                            ; DA1A  a9 3c
    STA a:chr_r0_alt                    ; DA1C  8d 58 00
    LDA #$38                            ; DA1F  a9 38
    STA a:chr_r1_alt                    ; DA21  8d 59 00
    LDA #$04                            ; DA24  a9 04
    STA obj_meta_dir                    ; DA26  8d 60 03
    LDA #$02                            ; DA29  a9 02
    STA obj_meta_id_s01                 ; DA2B  8d 01 03
    LDA #$00                            ; DA2E  a9 00
    STA obj_phase                       ; DA30  8d 00 06
    STA obj_timer2                      ; DA33  8d 20 06
    STA orbit_level                     ; DA36  8d c0 06
    STA $0BB2                           ; DA39  8d b2 0b
    LDA #$0D                            ; DA3C  a9 0d
    JSR SwitchBank16                    ; DA3E  20 0e c4
    LDX #$01                            ; DA41  a2 01
    JSR PlayerUpdate                    ; DA43  20 45 9b
loc_30_DA46:
    LDA #$00                            ; DA46  a9 00
    STA obj_phase                       ; DA48  8d 00 06
    STA obj_timer2                      ; DA4B  8d 20 06
    JSR sub_30_DABF                     ; DA4E  20 bf da
    INC obj_y_lo_s01                    ; DA51  e6 b1
    INC obj_y_lo                        ; DA53  e6 b0
    LDA obj_y_lo                        ; DA55  a5 b0
    CMP #$98                            ; DA57  c9 98
    BNE loc_30_DA46                     ; DA59  d0 eb
    LDA #$02                            ; DA5B  a9 02
    STA obj_meta_dir                    ; DA5D  8d 60 03
loc_30_DA60:
    JSR sub_30_DABF                     ; DA60  20 bf da
    INC obj_x_lo_s01                    ; DA63  e6 71
    INC obj_x_lo                        ; DA65  e6 70
    LDA obj_x_lo                        ; DA67  a5 70
    CMP #$78                            ; DA69  c9 78
    BNE loc_30_DA60                     ; DA6B  d0 f3
    LDA #$08                            ; DA6D  a9 08
    JMP SwitchBank16                    ; DA6F  4c 0e c4
ShopWalkOut:  ; s60: walk left to x=$68 / up to y=$8F; ExitLookup -> area_id/entry; sub_state=1
    JSR ShopDoorCloseStub               ; DA72  20 17 fe
    LDA #$1A                            ; DA75  a9 1a
    JSR SetR6                           ; DA77  20 18 c4
    JSR PaletteBuild                    ; DA7A  20 0e 8c
    LDA #$00                            ; DA7D  a9 00
    STA irq_mode                        ; DA7F  85 51
    LDA #$06                            ; DA81  a9 06
    STA obj_meta_dir                    ; DA83  8d 60 03
    JSR sub_30_DABF                     ; DA86  20 bf da
    DEC obj_x_lo_s01                    ; DA89  c6 71
    DEC obj_x_lo                        ; DA8B  c6 70
    LDA obj_x_lo                        ; DA8D  a5 70
    CMP #$68                            ; DA8F  c9 68
    BNE ShopWalkOut                     ; DA91  d0 df
    LDA #$00                            ; DA93  a9 00
    STA obj_meta_dir                    ; DA95  8d 60 03
loc_30_DA98:
    JSR sub_30_DABF                     ; DA98  20 bf da
    DEC obj_y_lo_s01                    ; DA9B  c6 b1
    DEC obj_y_lo                        ; DA9D  c6 b0
    LDA obj_y_lo                        ; DA9F  a5 b0
    CMP #$8F                            ; DAA1  c9 8f
    BNE loc_30_DA98                     ; DAA3  d0 f3
    JSR FarCall_B14_800E                ; DAA5  20 8c ff
    JSR ExitLookup                      ; DAA8  20 09 eb
    LDA (jmp_trampoline),Y              ; DAAB  b1 10
    STA area_id                         ; DAAD  85 6c
    INY                                 ; DAAF  c8
    LDA (jmp_trampoline),Y              ; DAB0  b1 10
    STA area_entry                      ; DAB2  85 6d
    LDA #$00                            ; DAB4  a9 00
    STA irq_mode                        ; DAB6  85 51
    STA pad_new                         ; DAB8  85 4b
    LDA #$01                            ; DABA  a9 01
    STA sub_state                       ; DABC  85 41
    RTS                                 ; DABE  60
sub_30_DABF:
    JSR WaitFrame                       ; DABF  20 69 c1
    LDA #$0D                            ; DAC2  a9 0d
    JSR SwitchBank16                    ; DAC4  20 0e c4
    LDX #$01                            ; DAC7  a2 01
    JSR PlayerUpdate                    ; DAC9  20 45 9b
    DEC obj_anim_s01                    ; DACC  ce e1 04
    LDA obj_flags,X                     ; DACF  bd 80 03
    AND #$0B                            ; DAD2  29 0b
    STA obj_flags,X                     ; DAD4  9d 80 03
    JMP FarCall_B14_800E                ; DAD7  4c 8c ff
sub_30_DADA:
    LDY #$0D                            ; DADA  a0 0d
loc_30_DADC:
    LDA obj_active,Y                    ; DADC  b9 a0 04
    BEQ loc_30_DAF6                     ; DADF  f0 15
    LDA obj_meta_id,Y                   ; DAE1  b9 00 03
    CMP #$EB                            ; DAE4  c9 eb
    BEQ loc_30_DAEC                     ; DAE6  f0 04
    CMP #$EC                            ; DAE8  c9 ec
    BNE loc_30_DAF6                     ; DAEA  d0 0a
loc_30_DAEC:
    LDA obj_tile_ofs,Y                  ; DAEC  b9 20 03
    BNE loc_30_DAF6                     ; DAEF  d0 05
    LDA #$00                            ; DAF1  a9 00
    STA obj_active,Y                    ; DAF3  99 a0 04
loc_30_DAF6:
    INY                                 ; DAF6  c8
    CPY #$1F                            ; DAF7  c0 1f
    BNE loc_30_DADC                     ; DAF9  d0 e1
    RTS                                 ; DAFB  60
sub_30_DAFC:
    LDA #$00                            ; DAFC  a9 00
    STA orbit_level                     ; DAFE  8d c0 06
    LDX #$04                            ; DB01  a2 04
    LDA #$00                            ; DB03  a9 00
loc_30_DB05:
    STA obj_active,X                    ; DB05  9d a0 04
    INX                                 ; DB08  e8
    CPX #$0C                            ; DB09  e0 0c
    BNE loc_30_DB05                     ; DB0B  d0 f8
    RTS                                 ; DB0D  60
SelectedUseItem:  ; $0715 = selected consumable ($642C/$6440) or key item ($642E/$6450) id - $1C
    LDY $642C                           ; DB0E  ac 2c 64
    BMI loc_30_DB19                     ; DB11  30 06
    LDA $6440,Y                         ; DB13  b9 40 64
    JMP loc_30_DB21                     ; DB16  4c 21 db
loc_30_DB19:
    LDY $642E                           ; DB19  ac 2e 64
    BMI loc_30_DB27                     ; DB1C  30 09
    LDA $6450,Y                         ; DB1E  b9 50 64
loc_30_DB21:
    SEC                                 ; DB21  38
    SBC #$1C                            ; DB22  e9 1c
    STA use_item_sel                    ; DB24  8d 15 07
loc_30_DB27:
    RTS                                 ; DB27  60
SubstTeleportMenu:  ; substate 20: msg 33.0 town list
    LDA player_hp                       ; DB28  ad c1 03
    BEQ loc_30_DB4A                     ; DB2B  f0 1d
    LDA boss_cam_lock                   ; DB2D  ad d7 07
    BNE loc_30_DB45                     ; DB30  d0 13
    LDA area_id                         ; DB32  a5 6c
    AND #$F8                            ; DB34  29 f8
    CMP #$58                            ; DB36  c9 58
    BEQ loc_30_DB45                     ; DB38  f0 0b
    LDA obj1_flash                      ; DB3A  ad 10 07
    AND #$0F                            ; DB3D  29 0f
    CMP #$04                            ; DB3F  c9 04
    BEQ loc_30_DB45                     ; DB41  f0 02
    BNE loc_30_DB4F                     ; DB43  d0 0a
loc_30_DB45:
    LDA #$4E                            ; DB45  a9 4e
    JSR PlaySound                       ; DB47  20 25 c1
loc_30_DB4A:
    LDA #$08                            ; DB4A  a9 08
    STA sub_state                       ; DB4C  85 41
    RTS                                 ; DB4E  60
loc_30_DB4F:
    LDA #$00                            ; DB4F  a9 00
    STA orbit_level                     ; DB51  8d c0 06
    LDA #$1A                            ; DB54  a9 1a
    JSR SetR6                           ; DB56  20 18 c4
    JSR PaletteBuild                    ; DB59  20 0e 8c
    LDA #$00                            ; DB5C  a9 00
    STA irq_mode                        ; DB5E  85 51
    LDA #$00                            ; DB60  a9 00
    STA text_msg_id                     ; DB62  8d de 07
    LDA #$21                            ; DB65  a9 21
    STA text_group                      ; DB67  8d df 07
    LDA #$14                            ; DB6A  a9 14
    JSR SetR6                           ; DB6C  20 18 c4
    JSR TextWindowOpen                  ; DB6F  20 00 85
    JSR TextPrintBox                    ; DB72  20 28 85
    JSR sub_30_DD36                     ; DB75  20 36 dd
    JSR WaitFrame                       ; DB78  20 69 c1
    LDA #$3C                            ; DB7B  a9 3c
    STA chr_r0_alt                      ; DB7D  85 58
    LDA #$38                            ; DB7F  a9 38
    STA chr_r1_alt                      ; DB81  85 59
    LDA #$5C                            ; DB83  a9 5c
    STA chr_r2_alt                      ; DB85  85 5a
    LDA #$10                            ; DB87  a9 10
    STA oam_x                           ; DB89  8d 03 02
    LDA cam_y_lo                        ; DB8C  a5 04
    AND #$07                            ; DB8E  29 07
    STA jmp_trampoline                  ; DB90  85 10
    LDA #$1C                            ; DB92  a9 1c
    SEC                                 ; DB94  38
    SBC jmp_trampoline                  ; DB95  e5 10
    STA oam_y                           ; DB97  8d 00 02
    LDA #$00                            ; DB9A  a9 00
    STA oam_attr                        ; DB9C  8d 02 02
    LDA #$00                            ; DB9F  a9 00
    STA tmp_20                          ; DBA1  85 20
    STA tmp_21                          ; DBA3  85 21
    JSR FarCall_B14_800E                ; DBA5  20 8c ff
    LDA #$01                            ; DBA8  a9 01
    STA irq_mode                        ; DBAA  85 51
loc_30_DBAC:
    LDA obj_draw_rot                    ; DBAC  ad ff 07
    LSR A                               ; DBAF  4a
    LSR A                               ; DBB0  4a
    LSR A                               ; DBB1  4a
    LSR A                               ; DBB2  4a
    LDA #$3F                            ; DBB3  a9 3f
    BCC loc_30_DBB9                     ; DBB5  90 02
    LDA #$3C                            ; DBB7  a9 3c
loc_30_DBB9:
    STA oam_tile                        ; DBB9  8d 01 02
    JSR ReadPadsMenu                    ; DBBC  20 e0 fe
    JSR FarCall_B14_800E                ; DBBF  20 8c ff
    JSR OamClipTop                      ; DBC2  20 7d c1
    JSR WaitFrame                       ; DBC5  20 69 c1
    JSR PadNewButtonIndex               ; DBC8  20 10 c2
    BCC loc_30_DBAC                     ; DBCB  90 df
    ASL A                               ; DBCD  0a
    TAX                                 ; DBCE  aa
    LDA JmpTable30_DBDC,X               ; DBCF  bd dc db
    STA jmp_trampoline                  ; DBD2  85 10
    LDA $DBDD,X                         ; DBD4  bd dd db
    STA tmp_11                          ; DBD7  85 11
    JMP (jmp_trampoline)                ; DBD9  6c 10 00
JmpTable30_DBDC:  ; s67: teleport-menu keys [PadNewButtonIndex 0-7 = A B SEL START U D L R]: DC70 / DC64 x3 / DBF4 DC0C DC26 DC3E (JUMP_TABLES)
    .dw sub_30_DC70                     ; 30:DBDC
    .dw sub_30_DC64                     ; 30:DBDE
    .dw sub_30_DC64                     ; 30:DBE0
    .dw sub_30_DC64                     ; 30:DBE2
    .dw sub_30_DBF4                     ; 30:DBE4
    .dw sub_30_DC0C                     ; 30:DBE6
    .dw sub_30_DC26                     ; 30:DBE8
    .dw sub_30_DC3E                     ; 30:DBEA
loc_30_DBEC:
    LDA #$4B                            ; DBEC  a9 4b
    JSR PlaySound                       ; DBEE  20 25 c1
    JMP loc_30_DBAC                     ; DBF1  4c ac db
sub_30_DBF4:
    LDA oam_y                           ; DBF4  ad 00 02
    SEC                                 ; DBF7  38
    SBC #$10                            ; DBF8  e9 10
    LDY tmp_21                          ; DBFA  a4 21
    DEY                                 ; DBFC  88
    BPL loc_30_DC04                     ; DBFD  10 05
    LDY #$03                            ; DBFF  a0 03
    CLC                                 ; DC01  18
    ADC #$40                            ; DC02  69 40
loc_30_DC04:
    STA oam_y                           ; DC04  8d 00 02
    STY tmp_21                          ; DC07  84 21
    JMP loc_30_DBEC                     ; DC09  4c ec db
sub_30_DC0C:
    LDA oam_y                           ; DC0C  ad 00 02
    CLC                                 ; DC0F  18
    ADC #$10                            ; DC10  69 10
    LDY tmp_21                          ; DC12  a4 21
    INY                                 ; DC14  c8
    CPY #$04                            ; DC15  c0 04
    BNE loc_30_DC1E                     ; DC17  d0 05
    LDY #$00                            ; DC19  a0 00
    SEC                                 ; DC1B  38
    SBC #$40                            ; DC1C  e9 40
loc_30_DC1E:
    STA oam_y                           ; DC1E  8d 00 02
    STY tmp_21                          ; DC21  84 21
    JMP loc_30_DBEC                     ; DC23  4c ec db
sub_30_DC26:
    LDA oam_x                           ; DC26  ad 03 02
    SEC                                 ; DC29  38
    SBC #$50                            ; DC2A  e9 50
    LDY tmp_20                          ; DC2C  a4 20
    DEY                                 ; DC2E  88
    BPL loc_30_DC36                     ; DC2F  10 05
    LDY #$02                            ; DC31  a0 02
    CLC                                 ; DC33  18
    ADC #$F0                            ; DC34  69 f0
loc_30_DC36:
    STA oam_x                           ; DC36  8d 03 02
    STY tmp_20                          ; DC39  84 20
    JMP loc_30_DBEC                     ; DC3B  4c ec db
sub_30_DC3E:
    LDA oam_x                           ; DC3E  ad 03 02
    CLC                                 ; DC41  18
    ADC #$50                            ; DC42  69 50
    LDY tmp_20                          ; DC44  a4 20
    INY                                 ; DC46  c8
    CPY #$03                            ; DC47  c0 03
    BNE loc_30_DC50                     ; DC49  d0 05
    LDY #$00                            ; DC4B  a0 00
    SEC                                 ; DC4D  38
    SBC #$F0                            ; DC4E  e9 f0
loc_30_DC50:
    STA oam_x                           ; DC50  8d 03 02
    STY tmp_20                          ; DC53  84 20
    JMP loc_30_DBEC                     ; DC55  4c ec db
    .db $02,$18,$1C,$D5,$50,$1B,$71,$72,$8C,$8E,$93,$FF; 30:DC58
sub_30_DC64:
    JSR sub_30_C19F                     ; DC64  20 9f c1
    LDA #$00                            ; DC67  a9 00
    STA irq_mode                        ; DC69  85 51
    LDA #$08                            ; DC6B  a9 08
    STA sub_state                       ; DC6D  85 41
    RTS                                 ; DC6F  60
sub_30_DC70:
    LDA tmp_21                          ; DC70  a5 21
    ASL A                               ; DC72  0a
    CLC                                 ; DC73  18
    ADC tmp_21                          ; DC74  65 21
    CLC                                 ; DC76  18
    ADC tmp_20                          ; DC77  65 20
    STA tmp_22                          ; DC79  85 22
    CMP #$0B                            ; DC7B  c9 0b
    BNE loc_30_DC82                     ; DC7D  d0 03
    JMP loc_30_DBAC                     ; DC7F  4c ac db
loc_30_DC82:
    JSR TestFlagF2F0                    ; DC82  20 58 dd
    BNE loc_30_DC8A                     ; DC85  d0 03
    JMP loc_30_DBAC                     ; DC87  4c ac db
loc_30_DC8A:
    LDY tmp_22                          ; DC8A  a4 22
    LDA $DC58,Y                         ; DC8C  b9 58 dc
    CMP area_id                         ; DC8F  c5 6c
    BEQ sub_30_DC64                     ; DC91  f0 d1
    STA area_id                         ; DC93  85 6c
    LDA #$40                            ; DC95  a9 40
    STA area_entry                      ; DC97  85 6d
    LDA sub_state                       ; DC99  a5 41
    CMP #$14                            ; DC9B  c9 14
    BNE loc_30_DCA9                     ; DC9D  d0 0a
    LDA #$1A                            ; DC9F  a9 1a
    JSR SetR6                           ; DCA1  20 18 c4
    LDA #$14                            ; DCA4  a9 14
    JSR SpendMp                         ; DCA6  20 e8 9a
loc_30_DCA9:
    LDY irq_handler_idx                 ; DCA9  a4 56
    DEY                                 ; DCAB  88
    BNE loc_30_DCA9                     ; DCAC  d0 fb
    JSR sub_30_C19F                     ; DCAE  20 9f c1
    LDA obj_meta_id_s01                 ; DCB1  ad 01 03
    PHA                                 ; DCB4  48
    LDA obj_overlay_id_s01              ; DCB5  ad 81 05
    PHA                                 ; DCB8  48
    LDA #$CE                            ; DCB9  a9 ce
    STA obj_meta_id_s01                 ; DCBB  8d 01 03
    LDA #$4C                            ; DCBE  a9 4c
    STA obj_overlay_id_s01              ; DCC0  8d 81 05
    STA obj_meta_id                     ; DCC3  8d 00 03
    LDA #$3F                            ; DCC6  a9 3f
    STA obj_anim_s01                    ; DCC8  8d e1 04
    LDA #$00                            ; DCCB  a9 00
    STA $0BB1                           ; DCCD  8d b1 0b
    LDX #$04                            ; DCD0  a2 04
loc_30_DCD2:
    STA obj_active,X                    ; DCD2  9d a0 04
    INX                                 ; DCD5  e8
    CPX #$20                            ; DCD6  e0 20
    BNE loc_30_DCD2                     ; DCD8  d0 f8
    LDA #$23                            ; DCDA  a9 23
    JSR PlaySound                       ; DCDC  20 25 c1
    LDA #$73                            ; DCDF  a9 73
    STA chr_r2_play                     ; DCE1  8d f2 07
    LDA #$00                            ; DCE4  a9 00
    STA irq_mode                        ; DCE6  85 51
loc_30_DCE8:
    JSR FarCall_B14_800E                ; DCE8  20 8c ff
    JSR WaitFrame                       ; DCEB  20 69 c1
    DEC obj_anim_s01                    ; DCEE  ce e1 04
    BPL loc_30_DCE8                     ; DCF1  10 f5
    LDA #$4C                            ; DCF3  a9 4c
    STA obj_meta_id_s01                 ; DCF5  8d 01 03
    STA obj_meta_id                     ; DCF8  8d 00 03
    JSR FarCall_B14_800E                ; DCFB  20 8c ff
    JSR SubstAreaEnter                  ; DCFE  20 2e ca
    LDA obj_flags_s01                   ; DD01  ad 81 03
    AND #$FB                            ; DD04  29 fb
    STA obj_flags_s01                   ; DD06  8d 81 03
    LDA #$4C                            ; DD09  a9 4c
    STA obj_overlay_id_s01              ; DD0B  8d 81 05
    LDA #$CE                            ; DD0E  a9 ce
    STA obj_meta_id_s01                 ; DD10  8d 01 03
    LDA #$C0                            ; DD13  a9 c0
    STA obj_anim_s01                    ; DD15  8d e1 04
    LDA #$66                            ; DD18  a9 66
    JSR PlaySound                       ; DD1A  20 25 c1
loc_30_DD1D:
    JSR FarCall_B14_800E                ; DD1D  20 8c ff
    JSR WaitFrame                       ; DD20  20 69 c1
    INC obj_anim_s01                    ; DD23  ee e1 04
    BNE loc_30_DD1D                     ; DD26  d0 f5
    PLA                                 ; DD28  68
    STA obj_overlay_id_s01              ; DD29  8d 81 05
    PLA                                 ; DD2C  68
    STA obj_meta_id_s01                 ; DD2D  8d 01 03
    JSR sub_30_C008                     ; DD30  20 08 c0
    JMP FarCall_B14_800E                ; DD33  4c 8c ff
sub_30_DD36:
    LDX #$12                            ; DD36  a2 12
    LDA #$10                            ; DD38  a9 10
loc_30_DD3A:
    STA wram_buf,X                      ; DD3A  9d 00 60
    DEX                                 ; DD3D  ca
    BPL loc_30_DD3A                     ; DD3E  10 fa
    LDA #$0A                            ; DD40  a9 0a
    STA tmp_11                          ; DD42  85 11
loc_30_DD44:
    LDA tmp_11                          ; DD44  a5 11
    JSR TestFlagF2F0                    ; DD46  20 58 dd
    BNE loc_30_DD53                     ; DD49  d0 08
    LDA tmp_11                          ; DD4B  a5 11
    CLC                                 ; DD4D  18
    ADC #$20                            ; DD4E  69 20
    JSR PpuQueueBlock                   ; DD50  20 82 c4
loc_30_DD53:
    DEC tmp_11                          ; DD53  c6 11
    BPL loc_30_DD44                     ; DD55  10 ed
    RTS                                 ; DD57  60
TestFlagF2F0:  ; A+5 -> test flag F2F0+A+5
    CLC                                 ; DD58  18
    ADC #$05                            ; DD59  69 05
    PHA                                 ; DD5B  48
    LSR A                               ; DD5C  4a
    LSR A                               ; DD5D  4a
    LSR A                               ; DD5E  4a
    TAX                                 ; DD5F  aa
    PLA                                 ; DD60  68
    AND #$07                            ; DD61  29 07
    TAY                                 ; DD63  a8
    LDA $64DE,X                         ; DD64  bd de 64
    AND BitMask,Y                       ; DD67  39 00 c0
    RTS                                 ; DD6A  60
SubstTelepathySelect:  ; substate 21: msg 33.2 pick sage
    LDA irq_mode                        ; DD6B  a5 51
    PHA                                 ; DD6D  48
    LDA #$02                            ; DD6E  a9 02
    STA text_msg_id                     ; DD70  8d de 07
    LDA #$21                            ; DD73  a9 21
    STA text_group                      ; DD75  8d df 07
    JSR sub_30_DF8F                     ; DD78  20 8f df
    LDA #$50                            ; DD7B  a9 50
    STA chr_r4_alt                      ; DD7D  85 5c
    LDA #$5C                            ; DD7F  a9 5c
    STA chr_r2_play                     ; DD81  8d f2 07
    STA chr_r2_alt                      ; DD84  85 5a
    LDX #$0F                            ; DD86  a2 0f
loc_30_DD88:
    LDA $E0A8,X                         ; DD88  bd a8 e0
    STA wram_6150,X                     ; DD8B  9d 50 61
    DEX                                 ; DD8E  ca
    BPL loc_30_DD88                     ; DD8F  10 f7
    LDA #$3F                            ; DD91  a9 3f
    STA obj_draw_rot                    ; DD93  8d ff 07
    LDA #$01                            ; DD96  a9 01
    STA tmp_1F                          ; DD98  85 1f
sub_30_DD9A:
    JSR sub_30_DE72                     ; DD9A  20 72 de
    JSR sub_30_DDCD                     ; DD9D  20 cd dd
    JSR sub_30_DFCE                     ; DDA0  20 ce df
    JSR WaitFrame                       ; DDA3  20 69 c1
    JSR ReadPadsMenu                    ; DDA6  20 e0 fe
    JSR PadNewButtonIndex               ; DDA9  20 10 c2
    BCC sub_30_DD9A                     ; DDAC  90 ec
    ASL A                               ; DDAE  0a
    TAY                                 ; DDAF  a8
    LDA JmpTable30_DDBD,Y               ; DDB0  b9 bd dd
    STA jmp_trampoline                  ; DDB3  85 10
    LDA $DDBE,Y                         ; DDB5  b9 be dd
    STA tmp_11                          ; DDB8  85 11
    JMP (jmp_trampoline)                ; DDBA  6c 10 00
JmpTable30_DDBD:  ; s67: telepathy-select keys [PadNewButtonIndex]: SubstTelepathyReply / DE51 x3 / DD9A x2 (U/D ignored) / DDE5 DDE1 (JUMP_TABLES)
    .dw SubstTelepathyReply             ; 30:DDBD
    .dw sub_30_DE51                     ; 30:DDBF
    .dw sub_30_DE51                     ; 30:DDC1
    .dw sub_30_DE51                     ; 30:DDC3
    .dw sub_30_DD9A                     ; 30:DDC5
    .dw sub_30_DD9A                     ; 30:DDC7
    .dw sub_30_DDE5                     ; 30:DDC9
    .dw sub_30_DDE1                     ; 30:DDCB
sub_30_DDCD:
    DEC obj_draw_rot                    ; DDCD  ce ff 07
    LDA obj_draw_rot                    ; DDD0  ad ff 07
    LSR A                               ; DDD3  4a
    LSR A                               ; DDD4  4a
    LSR A                               ; DDD5  4a
    LSR A                               ; DDD6  4a
    LDA #$3D                            ; DDD7  a9 3d
    BCC loc_30_DDDD                     ; DDD9  90 02
    LDA #$3F                            ; DDDB  a9 3f
loc_30_DDDD:
    STA oam_tile                        ; DDDD  8d 01 02
    RTS                                 ; DDE0  60
sub_30_DDE1:
    INC tmp_21                          ; DDE1  e6 21
    INC tmp_21                          ; DDE3  e6 21
sub_30_DDE5:
    DEC tmp_21                          ; DDE5  c6 21
    JSR sub_30_DDED                     ; DDE7  20 ed dd
    JMP sub_30_DD9A                     ; DDEA  4c 9a dd
sub_30_DDED:
    LDA tmp_21                          ; DDED  a5 21
    AND #$03                            ; DDEF  29 03
    STA tmp_21                          ; DDF1  85 21
    ASL A                               ; DDF3  0a
    ASL A                               ; DDF4  0a
    ASL A                               ; DDF5  0a
    ASL A                               ; DDF6  0a
    STA jmp_trampoline                  ; DDF7  85 10
    ASL A                               ; DDF9  0a
    CLC                                 ; DDFA  18
    ADC jmp_trampoline                  ; DDFB  65 10
    CLC                                 ; DDFD  18
    ADC #$38                            ; DDFE  69 38
    STA oam_x                           ; DE00  8d 03 02
    RTS                                 ; DE03  60
SubstTelepathyReply:  ; substate 22: 14:816F -> message
    JSR sub_30_C19F                     ; DE04  20 9f c1
    JSR OamClipTop                      ; DE07  20 7d c1
    LDA tmp_21                          ; DE0A  a5 21
    PHA                                 ; DE0C  48
    STA tmp_23                          ; DE0D  85 23
    LDA #$07                            ; DE0F  a9 07
    JSR SwitchBank16                    ; DE11  20 0e c4
    JSR Telepathy                       ; DE14  20 6f 81
    LDA tmp_20                          ; DE17  a5 20
    STA text_msg_id                     ; DE19  8d de 07
    LDA tmp_21                          ; DE1C  a5 21
    STA text_group                      ; DE1E  8d df 07
    JSR TextIdUnpack                    ; DE21  20 c2 d3
    LDA #$14                            ; DE24  a9 14
    JSR SetR6                           ; DE26  20 18 c4
    JSR TextPrintBox                    ; DE29  20 28 85
    PLA                                 ; DE2C  68
    STA tmp_21                          ; DE2D  85 21
loc_30_DE2F:
    JSR WaitFrame                       ; DE2F  20 69 c1
    JSR WaitPpuQueue                    ; DE32  20 76 c6
    JSR sub_30_DE72                     ; DE35  20 72 de
    DEC obj_draw_rot                    ; DE38  ce ff 07
    LDA #$01                            ; DE3B  a9 01
    STA tmp_20                          ; DE3D  85 20
    JSR sub_30_DFFD                     ; DE3F  20 fd df
    JSR sub_30_DE7E                     ; DE42  20 7e de
    JSR sub_30_C1AE                     ; DE45  20 ae c1
    JSR ReadPadsMenu                    ; DE48  20 e0 fe
    LDA pad_new                         ; DE4B  a5 4b
    AND #$F0                            ; DE4D  29 f0
    BEQ loc_30_DE2F                     ; DE4F  f0 de
sub_30_DE51:
    JSR sub_30_C19F                     ; DE51  20 9f c1
    LDA #$F0                            ; DE54  a9 f0
    JSR sub_30_C188                     ; DE56  20 88 c1
    LDA #$1A                            ; DE59  a9 1a
    JSR SetR6                           ; DE5B  20 18 c4
    JSR PaletteBuild                    ; DE5E  20 0e 8c
    JSR sub_30_C739                     ; DE61  20 39 c7
    JSR WaitFrame                       ; DE64  20 69 c1
    JSR sub_30_C008                     ; DE67  20 08 c0
    PLA                                 ; DE6A  68
    STA irq_mode                        ; DE6B  85 51
    LDA #$08                            ; DE6D  a9 08
    STA sub_state                       ; DE6F  85 41
loc_30_DE71:
    RTS                                 ; DE71  60
sub_30_DE72:
    LDA obj_draw_rot                    ; DE72  ad ff 07
    AND #$3F                            ; DE75  29 3f
    BNE loc_30_DE71                     ; DE77  d0 f8
    LDA #$2C                            ; DE79  a9 2c
    JMP PlaySound                       ; DE7B  4c 25 c1
sub_30_DE7E:
    LDA tmp_21                          ; DE7E  a5 21
    ASL A                               ; DE80  0a
    ASL A                               ; DE81  0a
    ASL A                               ; DE82  0a
    TAX                                 ; DE83  aa
    LDY #$00                            ; DE84  a0 00
loc_30_DE86:
    LDA $DEA1,X                         ; DE86  bd a1 de
    STA $6008,Y                         ; DE89  99 08 60
    INX                                 ; DE8C  e8
    INY                                 ; DE8D  c8
    CPY #$08                            ; DE8E  c0 08
    BNE loc_30_DE86                     ; DE90  d0 f4
    LDA cam_y_lo                        ; DE92  a5 04
    AND #$07                            ; DE94  29 07
    STA jmp_trampoline                  ; DE96  85 10
    LDA #$44                            ; DE98  a9 44
    SEC                                 ; DE9A  38
    SBC jmp_trampoline                  ; DE9B  e5 10
    STA $6009                           ; DE9D  8d 09 60
    RTS                                 ; DEA0  60
    .db $18,$40,$2E,$4C,$09,$00,$00,$00,$18,$40,$32,$4C,$09,$00,$00,$00; 30:DEA1
    .db $18,$40,$36,$4C,$09,$00,$00,$00,$18,$40,$3A,$4C,$09,$00,$00,$00; 30:DEB1
SubstChangeForm:  ; substate 23: msg 33.1 What form will you take?
    LDA irq_mode                        ; DEC1  a5 51
    PHA                                 ; DEC3  48
    LDA #$00                            ; DEC4  a9 00
    STA orbit_level                     ; DEC6  8d c0 06
    LDA #$01                            ; DEC9  a9 01
    STA text_msg_id                     ; DECB  8d de 07
    LDA #$21                            ; DECE  a9 21
    STA text_group                      ; DED0  8d df 07
    JSR sub_30_DF8F                     ; DED3  20 8f df
    LDA #$4D                            ; DED6  a9 4d
    STA chr_r4_alt                      ; DED8  85 5c
    LDA #$5C                            ; DEDA  a9 5c
    STA chr_r2_alt                      ; DEDC  85 5a
    STA chr_r3_play                     ; DEDE  8d f3 07
    JSR WaitPpuQueue                    ; DEE1  20 76 c6
sub_30_DEE4:
    JSR sub_30_DDCD                     ; DEE4  20 cd dd
    LDA #$00                            ; DEE7  a9 00
    STA tmp_1F                          ; DEE9  85 1f
    JSR sub_31_E038                     ; DEEB  20 38 e0
    JSR sub_30_DFD4                     ; DEEE  20 d4 df
    JSR sub_31_E077                     ; DEF1  20 77 e0
    JSR WaitFrame                       ; DEF4  20 69 c1
    JSR ReadPadsMenu                    ; DEF7  20 e0 fe
    JSR PadNewButtonIndex               ; DEFA  20 10 c2
    BCC sub_30_DEE4                     ; DEFD  90 e5
    ASL A                               ; DEFF  0a
    TAY                                 ; DF00  a8
    LDA JmpTable30_DF0E,Y               ; DF01  b9 0e df
    STA jmp_trampoline                  ; DF04  85 10
    LDA $DF0F,Y                         ; DF06  b9 0f df
    STA tmp_11                          ; DF09  85 11
    JMP (jmp_trampoline)                ; DF0B  6c 10 00
JmpTable30_DF0E:  ; s67: change-form keys [PadNewButtonIndex]: DF2F / DE51 x3 / DEE4 x2 (U/D ignored) / DF22 DF1E (JUMP_TABLES)
    .dw sub_30_DF2F                     ; 30:DF0E
    .dw sub_30_DE51                     ; 30:DF10
    .dw sub_30_DE51                     ; 30:DF12
    .dw sub_30_DE51                     ; 30:DF14
    .dw sub_30_DEE4                     ; 30:DF16
    .dw sub_30_DEE4                     ; 30:DF18
    .dw sub_30_DF22                     ; 30:DF1A
    .dw sub_30_DF1E                     ; 30:DF1C
sub_30_DF1E:
    INC tmp_21                          ; DF1E  e6 21
    INC tmp_21                          ; DF20  e6 21
sub_30_DF22:
    DEC tmp_21                          ; DF22  c6 21
    JSR sub_30_DDED                     ; DF24  20 ed dd
    LDA #$4B                            ; DF27  a9 4b
    JSR PlaySound                       ; DF29  20 25 c1
    JMP sub_30_DEE4                     ; DF2C  4c e4 de
sub_30_DF2F:
    LDA #$39                            ; DF2F  a9 39
    JSR PlaySound                       ; DF31  20 25 c1
    JSR sub_30_C19F                     ; DF34  20 9f c1
    LDA #$1F                            ; DF37  a9 1f
    STA obj_draw_rot                    ; DF39  8d ff 07
    STA tmp_1F                          ; DF3C  85 1f
    LDA chr_r4_play                     ; DF3E  ad f4 07
    PHA                                 ; DF41  48
    LDA #$4D                            ; DF42  a9 4d
    STA chr_r4_play                     ; DF44  8d f4 07
loc_30_DF47:
    JSR WaitFrame                       ; DF47  20 69 c1
    JSR sub_31_E038                     ; DF4A  20 38 e0
    JSR sub_30_DFD4                     ; DF4D  20 d4 df
    JSR sub_31_E077                     ; DF50  20 77 e0
    DEC obj_draw_rot                    ; DF53  ce ff 07
    BNE loc_30_DF47                     ; DF56  d0 ef
    LDA #$4C                            ; DF58  a9 4c
    STA wram_6003                       ; DF5A  8d 03 60
    JSR sub_30_DFD4                     ; DF5D  20 d4 df
    JSR sub_31_E077                     ; DF60  20 77 e0
    LDY #$20                            ; DF63  a0 20
    JSR sub_30_C176                     ; DF65  20 76 c1
    LDA #$F0                            ; DF68  a9 f0
    JSR sub_30_C188                     ; DF6A  20 88 c1
    JSR WaitFrame                       ; DF6D  20 69 c1
    PLA                                 ; DF70  68
    STA chr_r4_play                     ; DF71  8d f4 07
    LDA tmp_21                          ; DF74  a5 21
    ASL A                               ; DF76  0a
    ASL A                               ; DF77  0a
    ASL A                               ; DF78  0a
    ASL A                               ; DF79  0a
    ORA obj1_flash                      ; DF7A  0d 10 07
    ORA #$80                            ; DF7D  09 80
    STA obj1_flash                      ; DF7F  8d 10 07
    LDA #$1A                            ; DF82  a9 1a
    JSR SetR6                           ; DF84  20 18 c4
    LDA #$14                            ; DF87  a9 14
    JSR SpendMp                         ; DF89  20 e8 9a
    JMP sub_30_DE51                     ; DF8C  4c 51 de
sub_30_DF8F:
    LDA #$00                            ; DF8F  a9 00
    STA $04AC                           ; DF91  8d ac 04
    JSR WaitPpuQueue                    ; DF94  20 76 c6
    LDA #$14                            ; DF97  a9 14
    JSR SetR6                           ; DF99  20 18 c4
    JSR TextWindowOpen                  ; DF9C  20 00 85
    JSR TextPrintBox                    ; DF9F  20 28 85
    JSR sub_30_C874                     ; DFA2  20 74 c8
    LDA #$3C                            ; DFA5  a9 3c
    STA chr_r0_alt                      ; DFA7  85 58
    LDA #$38                            ; DFA9  a9 38
    STA chr_r1_alt                      ; DFAB  85 59
    LDA #$38                            ; DFAD  a9 38
    STA oam_x                           ; DFAF  8d 03 02
    LDA cam_y_lo                        ; DFB2  a5 04
    AND #$07                            ; DFB4  29 07
    STA jmp_trampoline                  ; DFB6  85 10
    LDA #$4C                            ; DFB8  a9 4c
    SEC                                 ; DFBA  38
    SBC jmp_trampoline                  ; DFBB  e5 10
    STA oam_y                           ; DFBD  8d 00 02
    LDA #$00                            ; DFC0  a9 00
    STA oam_attr                        ; DFC2  8d 02 02
    LDA #$01                            ; DFC5  a9 01
    STA irq_mode                        ; DFC7  85 51
    LDA #$00                            ; DFC9  a9 00
    STA tmp_21                          ; DFCB  85 21
    RTS                                 ; DFCD  60
sub_30_DFCE:
    JSR WaitPpuQueue                    ; DFCE  20 76 c6
    JSR sub_30_DFFD                     ; DFD1  20 fd df
sub_30_DFD4:
    LDX #$1F                            ; DFD4  a2 1f
loc_30_DFD6:
    LDA $E088,X                         ; DFD6  bd 88 e0
    STA $6008,X                         ; DFD9  9d 08 60
    DEX                                 ; DFDC  ca
    BPL loc_30_DFD6                     ; DFDD  10 f7
    LDA cam_y_lo                        ; DFDF  a5 04
    AND #$07                            ; DFE1  29 07
    STA jmp_trampoline                  ; DFE3  85 10
    LDA #$4C                            ; DFE5  a9 4c
    SEC                                 ; DFE7  38
    SBC jmp_trampoline                  ; DFE8  e5 10
    STA $6009                           ; DFEA  8d 09 60
    STA $6011                           ; DFED  8d 11 60
    STA $6019                           ; DFF0  8d 19 60
    STA wram_6021                       ; DFF3  8d 21 60
    LDA #$04                            ; DFF6  a9 04
    STA tmp_20                          ; DFF8  85 20
    JMP sub_30_C1AE                     ; DFFA  4c ae c1
sub_30_DFFD:
    LDA #$84                            ; DFFD  a9 84
    .db $8D                             ; DFFF  STA wram_buf -- continues in the next window
