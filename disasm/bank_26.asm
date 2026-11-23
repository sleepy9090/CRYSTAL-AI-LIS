; re-c bank 26  file 0x34010  CPU $8000-$9FFF  (tools/disasm_all.py)

ppumask_shadow = $01
scroll_shadow0 = $02
cam_y_lo = $04
tmp_08 = $08
tmp_0B = $0B
tmp_0E = $0E
jmp_trampoline = $10
tmp_11 = $11
tmp_12 = $12
tmp_13 = $13
tmp_14 = $14
tmp_15 = $15
tmp_16 = $16
tmp_17 = $17
tmp_18 = $18
tmp_19 = $19
tmp_1A = $1A
tmp_1B = $1B
tmp_1C = $1C
tmp_1D = $1D
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
tmp_2E = $2E
tmp_2F = $2F
tmp_30 = $30
tmp_31 = $31
pos_x_lo = $34
pos_x_hi = $35
pos_y_lo = $36
pos_y_hi = $37
sub_state = $41
pad_prev = $43
pad_dir = $49
pad_new = $4B
pad_act = $4D
mmc3_banksel_shadow = $50
zp_68 = $68
area_id = $6C
r7_shadow = $6F
obj_x_lo = $70
obj_x_hi = $90
obj_y_lo = $B0
obj_y_lo_s0D = $BD
obj_y_hi = $D0
obj_meta_id = $0300
obj_meta_id_s01 = $0301
obj_flags2 = $0340
obj_meta_dir = $0360
obj_flags = $0380
obj_flags_s01 = $0381
obj_coll = $03A0
player_hp_max = $03C0
player_hp = $03C1
obj_attack = $03E0
obj_defense = $0400
obj_hitflags = $0420
player_level = $0421
obj_bg_mask = $0460
obj_timer = $0480
obj_active = $04A0
obj_active_s04 = $04A4
obj_death_type = $04C0
obj_anim = $04E0
obj_anim_s01 = $04E1
obj_elem_drop = $0500
obj_xp_value = $0520
obj_spawn_b2 = $0540
ride_platform_slot = $0541
obj_spawn_param = $0560
obj_spawn_param_s01 = $0561
obj_overlay_id = $0580
obj_freeze = $05A0
obj_screen_x = $05C0
obj_screen_y = $05E0
obj_phase = $0600
obj_phase_s03 = $0603
obj_timer2 = $0620
obj_timer2_s03 = $0623
obj_act_timer = $0640
player_bg_attr = $0641
obj_act_timer_s03 = $0643
obj_aux = $0660
obj_aux_s01 = $0661
obj_aux_s0D = $066D
obj_param1 = $0680
obj_param2 = $06A0
orbit_level = $06C0
orbit_burst_timer = $06C1
pending_msg_id = $06C3
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
equip_armor = $0713
equip_shield = $0714
use_item_sel = $0715
equip_accessory = $0716
sword_power = $0719
invuln_timer = $071A
ram_07D6 = $07D6
boss_cam_lock = $07D7
pal_ids = $07E0
pal_id_spr0 = $07E4
pal_nofade_mask = $07E8
wram_6040 = $6040
wram_605F = $605F
wram_6060 = $6060
pal_buf = $6140
ppu_queue = $6200
ppu_queue_1 = $6201
ppu_queue_2 = $6202
ppu_queue_3 = $6203
visited_map = $62F0
screen_grid = $6300
story_flags = $6480
DiagToCardinal = $A017
PlayerMagic = $A022
PlayerFacing = $A1C9
ShotTable = $BC00
ShotOffsets = $BD80
BitMask = $C000
sub_30_C008 = $C008
PlaySound = $C125
WaitFrame = $C169
sub_30_C176 = $C176
SetR7 = $C427
sub_30_C436 = $C436
sub_30_C43E = $C43E
sub_30_C72B = $C72B
sub_30_C8B2 = $C8B2
FarCall_C25D = $FF80
FarCall_B14_800E = $FF8C

    .org $8000
DirGrid:  ; s47: 32x32 grid [dy cell+16][dx cell+16] -> hi=distance (16px cells) lo=dir 0-7; read by DirToTarget
    .db $97,$97,$97,$97,$97,$97,$87,$80,$80,$80,$80,$70,$70,$70,$70,$70; 26:8000
    .db $70,$70,$70,$70,$70,$70,$80,$80,$80,$80,$81,$91,$91,$91,$91,$91; 26:8010
    .db $97,$97,$97,$97,$97,$87,$87,$87,$70,$70,$70,$70,$70,$70,$70,$70; 26:8020
    .db $70,$70,$70,$70,$70,$70,$70,$70,$70,$81,$81,$81,$91,$91,$91,$91; 26:8030
    .db $97,$97,$97,$87,$87,$87,$77,$77,$70,$70,$70,$70,$60,$60,$60,$60; 26:8040
    .db $60,$60,$60,$60,$60,$70,$70,$70,$70,$71,$71,$81,$81,$81,$91,$91; 26:8050
    .db $97,$97,$87,$87,$87,$77,$77,$77,$77,$60,$60,$60,$60,$60,$60,$60; 26:8060
    .db $60,$60,$60,$60,$60,$60,$60,$60,$71,$71,$71,$71,$81,$81,$81,$91; 26:8070
    .db $97,$87,$87,$87,$77,$77,$77,$67,$67,$67,$60,$60,$50,$50,$50,$50; 26:8080
    .db $50,$50,$50,$50,$50,$60,$60,$61,$61,$61,$71,$71,$71,$81,$81,$81; 26:8090
    .db $97,$87,$87,$77,$77,$77,$67,$67,$67,$67,$50,$50,$50,$50,$50,$50; 26:80A0
    .db $50,$50,$50,$50,$50,$50,$50,$61,$61,$61,$61,$71,$71,$71,$81,$81; 26:80B0
    .db $97,$87,$87,$77,$77,$67,$67,$67,$57,$57,$57,$50,$50,$40,$40,$40; 26:80C0
    .db $40,$40,$40,$40,$50,$50,$51,$51,$51,$61,$61,$61,$71,$71,$81,$81; 26:80D0
    .db $87,$87,$77,$77,$77,$67,$67,$57,$57,$57,$57,$47,$40,$40,$40,$40; 26:80E0
    .db $40,$40,$40,$40,$40,$41,$51,$51,$51,$51,$61,$61,$71,$71,$71,$81; 26:80F0
    .db $86,$87,$77,$77,$67,$67,$67,$57,$57,$57,$47,$47,$40,$40,$30,$30; 26:8100
    .db $30,$30,$30,$40,$41,$41,$41,$51,$51,$51,$61,$61,$61,$71,$71,$82; 26:8110
    .db $86,$86,$76,$77,$67,$67,$57,$57,$57,$47,$47,$47,$37,$30,$30,$30; 26:8120
    .db $30,$30,$30,$30,$31,$41,$41,$41,$51,$51,$51,$61,$61,$72,$72,$82; 26:8130
    .db $86,$76,$76,$76,$66,$67,$57,$57,$47,$47,$47,$37,$37,$37,$30,$20; 26:8140
    .db $20,$20,$30,$31,$31,$31,$41,$41,$41,$51,$51,$62,$62,$72,$72,$72; 26:8150
    .db $86,$76,$76,$66,$66,$56,$56,$57,$47,$47,$37,$37,$37,$27,$20,$20; 26:8160
    .db $20,$20,$20,$21,$31,$31,$31,$41,$41,$52,$52,$52,$62,$62,$72,$72; 26:8170
    .db $86,$76,$76,$66,$66,$56,$56,$46,$46,$47,$37,$37,$27,$27,$27,$10; 26:8180
    .db $10,$10,$21,$21,$21,$31,$31,$42,$42,$42,$52,$52,$62,$62,$72,$72; 26:8190
    .db $86,$76,$76,$66,$66,$56,$56,$46,$46,$36,$36,$37,$27,$27,$17,$10; 26:81A0
    .db $10,$11,$11,$21,$21,$32,$32,$32,$42,$42,$52,$52,$62,$62,$72,$72; 26:81B0
    .db $86,$76,$76,$66,$66,$56,$56,$46,$46,$36,$36,$26,$26,$17,$17,$07; 26:81C0
    .db $00,$01,$11,$12,$22,$22,$32,$32,$42,$42,$52,$52,$62,$62,$72,$72; 26:81D0
    .db $86,$76,$76,$66,$66,$56,$56,$46,$46,$36,$36,$26,$26,$16,$16,$06; 26:81E0
    .db $02,$02,$12,$12,$22,$22,$32,$32,$42,$42,$52,$52,$62,$62,$72,$72; 26:81F0
    .db $86,$76,$76,$66,$66,$56,$56,$46,$46,$36,$36,$26,$26,$16,$15,$05; 26:8200
    .db $04,$03,$12,$12,$22,$22,$32,$32,$42,$42,$52,$52,$62,$62,$72,$72; 26:8210
    .db $86,$76,$76,$66,$66,$56,$56,$46,$46,$36,$36,$36,$25,$25,$15,$14; 26:8220
    .db $14,$13,$13,$23,$22,$32,$32,$32,$42,$42,$52,$52,$62,$62,$72,$72; 26:8230
    .db $86,$76,$76,$66,$66,$56,$56,$46,$46,$46,$35,$35,$25,$25,$25,$14; 26:8240
    .db $14,$14,$23,$23,$23,$33,$32,$42,$42,$42,$52,$52,$62,$62,$72,$72; 26:8250
    .db $86,$76,$76,$66,$66,$56,$56,$56,$45,$45,$35,$35,$35,$25,$24,$24; 26:8260
    .db $24,$24,$23,$23,$33,$33,$33,$42,$42,$52,$52,$52,$62,$62,$72,$72; 26:8270
    .db $86,$76,$76,$76,$66,$66,$55,$55,$45,$45,$45,$35,$35,$35,$34,$24; 26:8280
    .db $24,$24,$34,$33,$33,$33,$43,$43,$43,$52,$52,$62,$62,$72,$72,$72; 26:8290
    .db $86,$86,$76,$76,$65,$65,$55,$55,$55,$45,$45,$45,$35,$34,$34,$34; 26:82A0
    .db $34,$34,$34,$33,$33,$43,$43,$43,$53,$53,$53,$62,$62,$72,$72,$82; 26:82B0
    .db $86,$86,$75,$75,$65,$65,$65,$55,$55,$55,$45,$45,$45,$44,$34,$34; 26:82C0
    .db $34,$34,$34,$44,$43,$43,$43,$53,$53,$53,$63,$63,$63,$72,$72,$82; 26:82D0
    .db $86,$85,$75,$75,$75,$65,$65,$55,$55,$55,$55,$45,$44,$44,$44,$44; 26:82E0
    .db $44,$44,$44,$44,$44,$43,$53,$53,$53,$53,$63,$63,$73,$73,$73,$82; 26:82F0
    .db $85,$85,$85,$75,$75,$65,$65,$65,$55,$55,$55,$54,$54,$44,$44,$44; 26:8300
    .db $44,$44,$44,$44,$54,$53,$53,$53,$53,$63,$63,$63,$73,$73,$83,$82; 26:8310
    .db $95,$85,$85,$75,$75,$75,$65,$65,$65,$65,$55,$54,$54,$54,$54,$54; 26:8320
    .db $54,$54,$54,$54,$54,$54,$53,$63,$63,$63,$63,$73,$73,$73,$83,$83; 26:8330
    .db $95,$85,$85,$85,$75,$75,$75,$65,$65,$65,$64,$64,$54,$54,$54,$54; 26:8340
    .db $54,$54,$54,$54,$54,$64,$64,$63,$63,$63,$73,$73,$73,$83,$83,$83; 26:8350
    .db $95,$95,$85,$85,$85,$75,$75,$75,$75,$64,$64,$64,$64,$64,$64,$64; 26:8360
    .db $64,$64,$64,$64,$64,$64,$64,$63,$73,$73,$73,$73,$83,$83,$83,$93; 26:8370
    .db $95,$95,$95,$85,$85,$85,$75,$75,$75,$74,$74,$74,$64,$64,$64,$64; 26:8380
    .db $64,$64,$64,$64,$64,$74,$74,$74,$73,$73,$73,$83,$83,$83,$93,$93; 26:8390
    .db $95,$95,$95,$95,$85,$85,$85,$85,$84,$74,$74,$74,$74,$74,$74,$74; 26:83A0
    .db $74,$74,$74,$74,$74,$74,$74,$74,$84,$83,$83,$83,$83,$93,$93,$93; 26:83B0
    .db $95,$95,$95,$95,$95,$95,$85,$84,$84,$84,$84,$84,$74,$74,$74,$74; 26:83C0
    .db $74,$74,$74,$74,$74,$84,$84,$84,$84,$83,$83,$93,$93,$93,$93,$93; 26:83D0
    .db $95,$95,$95,$95,$95,$95,$95,$94,$84,$84,$84,$84,$84,$84,$84,$84; 26:83E0
    .db $84,$84,$84,$84,$84,$84,$84,$84,$84,$94,$93,$93,$93,$93,$93,$93; 26:83F0
    .db $A9,$00,$85,$20,$A9,$0F,$85,$21,$60; 26:8400
DirToTarget:  ; s47: X=self Y=target slot -> $20=dir 0-7 $21=distance in 16px cells (DirGrid 26:8000)
    SEC                                 ; 8409  38
    LDA a:obj_x_lo,Y                    ; 840A  b9 70 00
    SBC obj_x_lo,X                      ; 840D  f5 70
    STA jmp_trampoline                  ; 840F  85 10
    LDA a:obj_x_hi,Y                    ; 8411  b9 90 00
    SBC obj_x_hi,X                      ; 8414  f5 90
    STA tmp_11                          ; 8416  85 11
    BPL loc_26_841C                     ; 8418  10 02
    EOR #$FF                            ; 841A  49 ff
loc_26_841C:
    BEQ loc_26_8422                     ; 841C  f0 04
    LDA tmp_11                          ; 841E  a5 11
    BNE loc_26_8430                     ; 8420  d0 0e
loc_26_8422:
    LDA jmp_trampoline                  ; 8422  a5 10
    LSR tmp_11                          ; 8424  46 11
    ROR A                               ; 8426  6a
    LSR tmp_11                          ; 8427  46 11
    ROR A                               ; 8429  6a
    LSR tmp_11                          ; 842A  46 11
    ROR A                               ; 842C  6a
    LSR tmp_11                          ; 842D  46 11
    ROR A                               ; 842F  6a
loc_26_8430:
    CLC                                 ; 8430  18
    ADC #$10                            ; 8431  69 10
    AND #$1F                            ; 8433  29 1f
    STA jmp_trampoline                  ; 8435  85 10
    SEC                                 ; 8437  38
    LDA a:obj_y_lo,Y                    ; 8438  b9 b0 00
    SBC obj_y_lo,X                      ; 843B  f5 b0
    STA tmp_11                          ; 843D  85 11
    LDA a:obj_y_hi,Y                    ; 843F  b9 d0 00
    SBC obj_y_hi,X                      ; 8442  f5 d0
    TAY                                 ; 8444  a8
    BPL loc_26_8449                     ; 8445  10 02
    EOR #$FF                            ; 8447  49 ff
loc_26_8449:
    BEQ loc_26_8458                     ; 8449  f0 0d
    TYA                                 ; 844B  98
    ASL A                               ; 844C  0a
    ASL A                               ; 844D  0a
    ASL A                               ; 844E  0a
    ASL A                               ; 844F  0a
    STA tmp_11                          ; 8450  85 11
    TYA                                 ; 8452  98
    LSR A                               ; 8453  4a
    LSR A                               ; 8454  4a
    LSR A                               ; 8455  4a
    LSR A                               ; 8456  4a
    TAY                                 ; 8457  a8
loc_26_8458:
    LDA tmp_11                          ; 8458  a5 11
    AND #$F0                            ; 845A  29 f0
    ASL A                               ; 845C  0a
    ORA jmp_trampoline                  ; 845D  05 10
    STA jmp_trampoline                  ; 845F  85 10
    TYA                                 ; 8461  98
    ROL A                               ; 8462  2a
    CLC                                 ; 8463  18
    ADC #$02                            ; 8464  69 02
    AND #$03                            ; 8466  29 03
    CLC                                 ; 8468  18
    ADC #$80                            ; 8469  69 80
    STA tmp_11                          ; 846B  85 11
    LDY #$00                            ; 846D  a0 00
    LDA (jmp_trampoline),Y              ; 846F  b1 10
    AND #$0F                            ; 8471  29 0f
    STA tmp_20                          ; 8473  85 20
    LDA (jmp_trampoline),Y              ; 8475  b1 10
    LSR A                               ; 8477  4a
    LSR A                               ; 8478  4a
    LSR A                               ; 8479  4a
    LSR A                               ; 847A  4a
    STA tmp_21                          ; 847B  85 21
    RTS                                 ; 847D  60
    .db $EE,$61                         ; 26:847E
ObjStepVector:  ; A=dir: $31/$30 = dy/dx from speed tables 84BC/84CC/84DC; 8-frame patterns
    ASL A                               ; 8480  0a
    ASL A                               ; 8481  0a
    ASL A                               ; 8482  0a
    STA tmp_12                          ; 8483  85 12
    LDA obj_flags2,X                    ; 8485  bd 40 03
    AND #$0F                            ; 8488  29 0f
    TAY                                 ; 848A  a8
    LDA obj_flags,X                     ; 848B  bd 80 03
    ASL A                               ; 848E  0a
    BPL loc_26_8497                     ; 848F  10 06
    DEY                                 ; 8491  88
    DEY                                 ; 8492  88
    BPL loc_26_8497                     ; 8493  10 02
    LDY #$00                            ; 8495  a0 00
loc_26_8497:
    LDA ObjStepSpeedTables,Y            ; 8497  b9 bc 84
    STA jmp_trampoline                  ; 849A  85 10
    LDA $84CC,Y                         ; 849C  b9 cc 84
    STA tmp_11                          ; 849F  85 11
    LDA $84DC,Y                         ; 84A1  b9 dc 84
    STA tmp_13                          ; 84A4  85 13
    LDA obj_timer,X                     ; 84A6  bd 80 04
    AND #$07                            ; 84A9  29 07
    ORA tmp_12                          ; 84AB  05 12
    TAY                                 ; 84AD  a8
    LDA (jmp_trampoline),Y              ; 84AE  b1 10
    STA tmp_31                          ; 84B0  85 31
    TYA                                 ; 84B2  98
    CLC                                 ; 84B3  18
    ADC tmp_13                          ; 84B4  65 13
    TAY                                 ; 84B6  a8
    LDA (jmp_trampoline),Y              ; 84B7  b1 10
    STA tmp_30                          ; 84B9  85 30
    RTS                                 ; 84BB  60
ObjStepSpeedTables:  ; s49: u8[3x16] speed tables 84BC/84CC/84DC read by ObjStepVector 26:8480 (exported to web/data/tables.json)
    .db $EC,$3C,$8C,$DC,$2C,$7C,$CC,$1C,$6C,$BC,$0C,$5C,$FC,$9C,$3C,$DC; 26:84BC
    .db $84,$85,$85,$85,$86,$86,$86,$87,$87,$87,$88,$88,$88,$89,$8A,$8A; 26:84CC
    .db $10,$10,$10,$10,$10,$10,$10,$10,$10,$10,$10,$20,$20,$20,$20,$20; 26:84DC
    .db $FF,$00,$FF,$00,$FF,$00,$FF,$00,$FF,$00,$FF,$00,$00,$FF,$00,$00; 26:84EC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$01,$00,$01,$00,$00,$01,$00,$00; 26:84FC
    .db $01,$00,$01,$00,$01,$00,$01,$00,$01,$00,$01,$00,$00,$01,$00,$00; 26:850C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FF,$00,$FF,$00,$00,$FF,$00,$00; 26:851C
    .db $FF,$00,$FF,$00,$FF,$00,$FF,$00,$FF,$00,$FF,$00,$00,$FF,$00,$00; 26:852C
    .db $FF,$FF,$FF,$00,$FF,$FF,$FF,$00,$FF,$00,$FF,$00,$FF,$00,$FF,$00; 26:853C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$01,$00,$01,$00,$01,$00,$01; 26:854C
    .db $01,$01,$01,$00,$01,$01,$01,$00,$00,$01,$00,$01,$00,$01,$00,$01; 26:855C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FF,$00,$FF,$00,$FF,$00,$FF,$00; 26:856C
    .db $FF,$FF,$FF,$00,$FF,$FF,$FF,$00,$FF,$00,$FF,$00,$FF,$00,$FF,$00; 26:857C
    .db $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$00,$FF,$FF,$FF,$00; 26:858C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$01,$01,$01,$00,$01,$01,$01,$00; 26:859C
    .db $01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$00,$01,$01,$01,$00; 26:85AC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FF,$FF,$FF,$00,$FF,$FF,$FF,$00; 26:85BC
    .db $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$00,$FF,$FF,$FF,$00; 26:85CC
    .db $FE,$FF,$FF,$FF,$FE,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$00; 26:85DC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$01,$01,$01,$01,$01,$01,$01,$00; 26:85EC
    .db $01,$01,$01,$02,$01,$01,$01,$02,$01,$01,$01,$01,$01,$01,$01,$00; 26:85FC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$00; 26:860C
    .db $FE,$FF,$FF,$FF,$FE,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$00; 26:861C
    .db $FE,$FF,$FE,$FF,$FE,$FF,$FE,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FE; 26:862C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$01,$01,$01,$01,$01,$01,$01,$02; 26:863C
    .db $02,$01,$02,$01,$02,$01,$02,$01,$01,$01,$01,$01,$01,$01,$01,$02; 26:864C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FE,$FF,$FE,$FF,$FE,$FF,$FE,$FF; 26:865C
    .db $FE,$FF,$FE,$FF,$FE,$FF,$FE,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FE; 26:866C
    .db $FE,$FE,$FE,$FF,$FE,$FE,$FE,$FF,$FE,$FF,$FF,$FF,$FE,$FF,$FF,$FF; 26:867C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$02,$01,$01,$01,$02,$01,$01,$01; 26:868C
    .db $02,$02,$02,$01,$02,$02,$02,$01,$02,$01,$01,$01,$02,$01,$01,$01; 26:869C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FE,$FF,$FF,$FF,$FE,$FF,$FF,$FF; 26:86AC
    .db $FE,$FE,$FE,$FF,$FE,$FE,$FE,$FF,$FE,$FF,$FF,$FF,$FE,$FF,$FF,$FF; 26:86BC
    .db $FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FF,$FE,$FF,$FE,$FF,$FE,$FF; 26:86CC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$02,$01,$02,$01,$02,$01,$02,$01; 26:86DC
    .db $02,$02,$02,$02,$02,$02,$02,$02,$02,$01,$02,$01,$02,$01,$02,$01; 26:86EC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FE,$FF,$FE,$FF,$FE,$FF,$FE,$FF; 26:86FC
    .db $FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FF,$FE,$FF,$FE,$FF,$FE,$FF; 26:870C
    .db $FE,$FD,$FE,$FD,$FE,$FD,$FE,$FD,$FE,$FE,$FE,$FF,$FE,$FE,$FE,$FF; 26:871C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$02,$02,$02,$01,$02,$02,$02,$01; 26:872C
    .db $02,$03,$02,$03,$02,$03,$02,$03,$02,$02,$02,$01,$02,$02,$02,$01; 26:873C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FE,$FE,$FE,$FF,$FE,$FE,$FE,$FF; 26:874C
    .db $FE,$FD,$FE,$FD,$FE,$FD,$FE,$FD,$FE,$FE,$FE,$FF,$FE,$FE,$FE,$FF; 26:875C
    .db $FB,$FB,$FB,$FB,$FB,$FB,$FB,$FB,$FB,$FC,$FB,$FC,$FB,$FC,$FB,$FC; 26:876C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$05,$04,$05,$04,$05,$04,$05,$04; 26:877C
    .db $05,$05,$05,$05,$05,$05,$05,$05,$05,$04,$05,$04,$05,$04,$05,$04; 26:878C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FB,$FC,$FB,$FC,$FB,$FC,$FB,$FC; 26:879C
    .db $FB,$FB,$FB,$FB,$FB,$FB,$FB,$FB,$FB,$FC,$FB,$FC,$FB,$FC,$FB,$FC; 26:87AC
    .db $F9,$FA,$F9,$FA,$F9,$FA,$F9,$FA,$FB,$FB,$FC,$FB,$FB,$FC,$FB,$FC; 26:87BC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$05,$05,$04,$05,$05,$04,$05,$04; 26:87CC
    .db $07,$06,$07,$06,$07,$06,$07,$06,$05,$05,$04,$05,$05,$04,$05,$04; 26:87DC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$F9,$F9,$FA,$F9,$F9,$FA,$F9,$FA; 26:87EC
    .db $F9,$FA,$F9,$FA,$F9,$FA,$F9,$FA,$FB,$FB,$FC,$FB,$FB,$FC,$FB,$FC; 26:87FC
    .db $F8,$F8,$F8,$F8,$F8,$F8,$F8,$F8,$FA,$FA,$FB,$FA,$FA,$FB,$FA,$FB; 26:880C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$06,$06,$05,$06,$06,$05,$06,$05; 26:881C
    .db $08,$08,$08,$08,$08,$08,$08,$08,$06,$06,$05,$06,$06,$05,$06,$05; 26:882C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FA,$FA,$FB,$FA,$FA,$FB,$FA,$FB; 26:883C
    .db $F8,$F8,$F8,$F8,$F8,$F8,$F8,$F8,$FA,$FA,$FB,$FA,$FA,$FB,$FA,$FB; 26:884C
    .db $F9,$F9,$F9,$F9,$F9,$F9,$F9,$F9,$FA,$F9,$FA,$F9,$FA,$F9,$FA,$F9; 26:885C
    .db $FB,$FB,$FB,$FB,$FB,$FB,$FB,$FB,$FD,$FE,$FD,$FD,$FE,$FD,$FD,$FE; 26:886C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$03,$02,$03,$03,$02,$03,$03,$02; 26:887C
    .db $05,$05,$05,$05,$05,$05,$05,$05,$06,$07,$06,$07,$06,$07,$06,$07; 26:888C
    .db $07,$07,$07,$07,$07,$07,$07,$07,$06,$07,$06,$07,$06,$07,$06,$07; 26:889C
    .db $05,$05,$05,$05,$05,$05,$05,$05,$03,$02,$03,$03,$02,$03,$03,$02; 26:88AC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FD,$FE,$FD,$FD,$FE,$FD,$FD,$FE; 26:88BC
    .db $FB,$FB,$FB,$FB,$FB,$FB,$FB,$FB,$FA,$F9,$FA,$F9,$FA,$F9,$FA,$F9; 26:88CC
    .db $F9,$F9,$F9,$F9,$F9,$F9,$F9,$F9,$FA,$F9,$FA,$F9,$FA,$F9,$FA,$F9; 26:88DC
    .db $FB,$FB,$FB,$FB,$FB,$FB,$FB,$FB,$FD,$FE,$FD,$FD,$FE,$FD,$FD,$FE; 26:88EC
    .db $FE,$FE,$FE,$FF,$FE,$FE,$FE,$FF,$FE,$FE,$FF,$FE,$FE,$FF,$FE,$FF; 26:88FC
    .db $FF,$FF,$FF,$FE,$FF,$FF,$FF,$FE,$FF,$FF,$00,$FF,$FF,$00,$FF,$00; 26:890C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$01,$01,$00,$01,$01,$00,$01,$00; 26:891C
    .db $01,$01,$01,$02,$01,$01,$01,$02,$02,$02,$01,$02,$02,$01,$02,$01; 26:892C
    .db $02,$02,$02,$01,$02,$02,$02,$01,$02,$02,$01,$02,$02,$01,$02,$01; 26:893C
    .db $01,$01,$01,$02,$01,$01,$01,$02,$01,$01,$00,$01,$01,$00,$01,$00; 26:894C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FF,$FF,$00,$FF,$FF,$00,$FF,$00; 26:895C
    .db $FF,$FF,$FF,$FE,$FF,$FF,$FF,$FE,$FE,$FE,$FF,$FE,$FE,$FF,$FE,$FF; 26:896C
    .db $FE,$FE,$FE,$FF,$FE,$FE,$FE,$FF,$FE,$FE,$FF,$FE,$FE,$FF,$FE,$FF; 26:897C
    .db $FF,$FF,$FF,$FE,$FF,$FF,$FF,$FE,$FF,$FF,$00,$FF,$FF,$00,$FF,$00; 26:898C
    .db $FE,$FD,$FE,$FD,$FE,$FD,$FE,$FD,$FE,$FE,$FE,$FD,$FE,$FE,$FE,$FD; 26:899C
    .db $FE,$FE,$FE,$FF,$FE,$FE,$FE,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF; 26:89AC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$01,$01,$01,$01,$01,$01,$01,$01; 26:89BC
    .db $02,$02,$02,$01,$02,$02,$02,$01,$02,$02,$02,$03,$02,$02,$02,$03; 26:89CC
    .db $02,$03,$02,$03,$02,$03,$02,$03,$02,$02,$02,$03,$02,$02,$02,$03; 26:89DC
    .db $02,$02,$02,$01,$02,$02,$02,$01,$01,$01,$01,$01,$01,$01,$01,$01; 26:89EC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF; 26:89FC
    .db $FE,$FE,$FE,$FF,$FE,$FE,$FE,$FF,$FE,$FE,$FE,$FD,$FE,$FE,$FE,$FD; 26:8A0C
    .db $FE,$FD,$FE,$FD,$FE,$FD,$FE,$FD,$FE,$FE,$FE,$FD,$FE,$FE,$FE,$FD; 26:8A1C
    .db $FE,$FE,$FE,$FF,$FE,$FE,$FE,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF; 26:8A2C
    .db $FD,$FD,$FD,$FD,$FD,$FD,$FD,$FD,$FD,$FD,$FD,$FE,$FD,$FD,$FD,$FE; 26:8A3C
    .db $FE,$FE,$FE,$FE,$FE,$FE,$FE,$FD,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FE; 26:8A4C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$01,$01,$01,$01,$01,$01,$01,$02; 26:8A5C
    .db $02,$02,$02,$02,$02,$02,$02,$03,$03,$03,$03,$02,$03,$03,$03,$02; 26:8A6C
    .db $03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$02,$03,$03,$03,$02; 26:8A7C
    .db $02,$02,$02,$02,$02,$02,$02,$03,$01,$01,$01,$01,$01,$01,$01,$02; 26:8A8C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FE; 26:8A9C
    .db $FE,$FE,$FE,$FE,$FE,$FE,$FE,$FD,$FD,$FD,$FD,$FE,$FD,$FD,$FD,$FE; 26:8AAC
    .db $FD,$FD,$FD,$FD,$FD,$FD,$FD,$FD,$FD,$FD,$FD,$FE,$FD,$FD,$FD,$FE; 26:8ABC
    .db $FE,$FE,$FE,$FE,$FE,$FE,$FE,$FD,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FE; 26:8ACC
    .db $FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FF; 26:8ADC
    .db $FF,$FF,$FE,$FF,$FF,$FE,$FF,$FE,$FF,$FF,$FF,$00,$FF,$FF,$FF,$00; 26:8AEC
    .db $00,$00,$00,$00,$00,$00,$00,$00,$01,$01,$01,$00,$01,$01,$01,$00; 26:8AFC
    .db $01,$01,$02,$01,$01,$02,$01,$02,$02,$02,$02,$02,$02,$02,$02,$01; 26:8B0C
    .db $02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$01; 26:8B1C
    .db $01,$01,$02,$01,$01,$02,$01,$02,$01,$01,$01,$00,$01,$01,$01,$00; 26:8B2C
    .db $00,$00,$00,$00,$00,$00,$00,$00,$FF,$FF,$FF,$00,$FF,$FF,$FF,$00; 26:8B3C
    .db $FF,$FF,$FE,$FF,$FE,$FF,$FF,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FF; 26:8B4C
    .db $FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$FF; 26:8B5C
    .db $FF,$FF,$FE,$FF,$FF,$FE,$FF,$FE,$FF,$FF,$FF,$00,$FF,$FF,$FF,$00; 26:8B6C
    .db $69,$6E,$64                     ; 26:8B7C
LevelHpTable:  ; u8[16] max HP per level (index player_level)
    .db $00,$30,$40,$50,$60,$70,$80,$90,$A0,$B0,$C0,$D0,$E0,$F0,$FF,$FF; 26:8B7F
LevelMpTable:  ; u8[16] max MP per level
    .db $FF,$22,$22,$33,$44,$55,$66,$77,$88,$99,$AA,$BB,$CC,$DD,$EE; 26:8B8F
LevelXpTable:  ; u16[16] XP needed for next level -> $0706/$0707
    .db $FF,$FF,$1E,$00,$3C,$00,$96,$00,$2C,$01,$BC,$02,$B0,$04,$40,$06; 26:8B9E
    .db $AC,$0D,$A8,$16,$40,$1F,$10,$27,$20,$4E,$30,$75,$40,$9C,$50,$C3; 26:8BAE
    .db $00,$00,$00,$02,$06,$0A,$0E,$12,$18,$14,$20,$00,$02,$06,$08,$0C; 26:8BBE
    .db $12,$10,$18,$20                 ; 26:8BCE
SwordAttackTable:  ; s44: u8[6] 00 01 02 04 08 10 by equip_sword -> slash shot $03E2
    .db $00,$01,$02,$04,$08,$10         ; 26:8BD2
SwordMpCost:  ; s44: u8[6] 00 08 10 10 28 00 level-3 charge MP cost by equip_sword
    .db $00,$08,$10,$10,$28,$00,$00,$00,$01,$00,$02,$00,$04,$00,$08,$00; 26:8BD8
    .db $10,$00,$1E,$00,$32,$00,$64,$00,$C8,$00,$90,$01,$32,$00,$64,$00; 26:8BE8
    .db $C8,$00,$90,$01,$F4,$01,$A8,$A8,$A8,$A8,$A8,$A8,$A8,$A8,$A8,$A8; 26:8BF8
    .db $A8,$A9,$A9,$A9,$A9,$AA         ; 26:8C08
PaletteBuild:  ; $11=0 (no fade) then builds $6140-$615F from ids $07E0-$07E7; 8C10 = entry with A=fade step
    LDA #$00                            ; 8C0E  a9 00
    STA tmp_11                          ; 8C10  85 11
sub_26_8C12:
    LDA r7_shadow                       ; 8C12  a5 6f
    PHA                                 ; 8C14  48
    TXA                                 ; 8C15  8a
    PHA                                 ; 8C16  48
    LDA pal_nofade_mask                 ; 8C17  ad e8 07
    STA tmp_1D                          ; 8C1A  85 1d
    LDX #$00                            ; 8C1C  a2 00
loc_26_8C1E:
    TXA                                 ; 8C1E  8a
    LSR A                               ; 8C1F  4a
    LSR A                               ; 8C20  4a
    TAY                                 ; 8C21  a8
    LDA pal_ids,Y                       ; 8C22  b9 e0 07
    JSR PaletteLoadOne                  ; 8C25  20 6e 8c
    CPX #$20                            ; 8C28  e0 20
    BNE loc_26_8C1E                     ; 8C2A  d0 f2
    PLA                                 ; 8C2C  68
    TAX                                 ; 8C2D  aa
    PLA                                 ; 8C2E  68
    JMP SetR7                           ; 8C2F  4c 27 c4
PaletteBlackFill:  ; $6040-$605F <- $0F
    LDX #$1F                            ; 8C32  a2 1f
    LDA #$0F                            ; 8C34  a9 0f
loc_26_8C36:
    STA wram_6040,X                     ; 8C36  9d 40 60
    DEX                                 ; 8C39  ca
    BPL loc_26_8C36                     ; 8C3A  10 fa
    RTS                                 ; 8C3C  60
Bank26_FadeA:  ; A->$18; $11=0 $19=$10 (fade step +; tentative)
    STA tmp_18                          ; 8C3D  85 18
    LDA #$00                            ; 8C3F  a9 00
    STA tmp_11                          ; 8C41  85 11
    LDA #$10                            ; 8C43  a9 10
    STA tmp_19                          ; 8C45  85 19
    BNE loc_26_8C53                     ; 8C47  d0 0a
Bank26_FadeB:  ; A->$18; $11=$40 $19=$F0 (fade step -; tentative)
    STA tmp_18                          ; 8C49  85 18
    LDA #$40                            ; 8C4B  a9 40
    STA tmp_11                          ; 8C4D  85 11
    LDA #$F0                            ; 8C4F  a9 f0
    STA tmp_19                          ; 8C51  85 19
loc_26_8C53:
    LDA #$05                            ; 8C53  a9 05
    STA tmp_17                          ; 8C55  85 17
loc_26_8C57:
    LDY tmp_18                          ; 8C57  a4 18
    INY                                 ; 8C59  c8
    JSR sub_30_C176                     ; 8C5A  20 76 c1
    JSR sub_26_8C12                     ; 8C5D  20 12 8c
    LDA tmp_11                          ; 8C60  a5 11
    CLC                                 ; 8C62  18
    ADC tmp_19                          ; 8C63  65 19
    STA tmp_11                          ; 8C65  85 11
    DEC tmp_17                          ; 8C67  c6 17
    BNE loc_26_8C57                     ; 8C69  d0 ec
    JMP WaitFrame                       ; 8C6B  4c 69 c1
PaletteLoadOne:  ; A=id (+$B0 sprite): 4 colours from bank 2/3 $A000+(id>>2&1F)*$100+$F0+(id&3)*4
    CPX #$10                            ; 8C6E  e0 10
    BMI loc_26_8C75                     ; 8C70  30 03
    CLC                                 ; 8C72  18
    ADC #$B0                            ; 8C73  69 b0
loc_26_8C75:
    STA tmp_1C                          ; 8C75  85 1c
    AND #$03                            ; 8C77  29 03
    ASL A                               ; 8C79  0a
    ASL A                               ; 8C7A  0a
    ORA #$F0                            ; 8C7B  09 f0
    STA tmp_1A                          ; 8C7D  85 1a
    LDA tmp_1C                          ; 8C7F  a5 1c
    LSR A                               ; 8C81  4a
    LSR A                               ; 8C82  4a
    AND #$1F                            ; 8C83  29 1f
    ORA #$A0                            ; 8C85  09 a0
    STA tmp_1B                          ; 8C87  85 1b
    LDY #$02                            ; 8C89  a0 02
    LDA tmp_1C                          ; 8C8B  a5 1c
    AND #$80                            ; 8C8D  29 80
    BEQ loc_26_8C92                     ; 8C8F  f0 01
    INY                                 ; 8C91  c8
loc_26_8C92:
    TYA                                 ; 8C92  98
    JSR SetR7                           ; 8C93  20 27 c4
    LDA tmp_11                          ; 8C96  a5 11
    PHA                                 ; 8C98  48
    LSR tmp_1D                          ; 8C99  46 1d
    BCC loc_26_8CA1                     ; 8C9B  90 04
    LDA #$00                            ; 8C9D  a9 00
    STA tmp_11                          ; 8C9F  85 11
loc_26_8CA1:
    LDY #$00                            ; 8CA1  a0 00
loc_26_8CA3:
    LDA (tmp_1A),Y                      ; 8CA3  b1 1a
    CMP #$FF                            ; 8CA5  c9 ff
    BNE loc_26_8CAC                     ; 8CA7  d0 03
    LDA pal_buf                         ; 8CA9  ad 40 61
loc_26_8CAC:
    SEC                                 ; 8CAC  38
    SBC tmp_11                          ; 8CAD  e5 11
    BPL loc_26_8CB3                     ; 8CAF  10 02
    LDA #$0F                            ; 8CB1  a9 0f
loc_26_8CB3:
    STA pal_buf,X                       ; 8CB3  9d 40 61
    INX                                 ; 8CB6  e8
    INY                                 ; 8CB7  c8
    CPY #$04                            ; 8CB8  c0 04
    BNE loc_26_8CA3                     ; 8CBA  d0 e7
    PLA                                 ; 8CBC  68
    STA tmp_11                          ; 8CBD  85 11
    RTS                                 ; 8CBF  60
HudHpBar:  ; s46: redraw HP bar ($6060-$6070 from player_hp_max/player_hp); was Bank26_8CC0 (called via FarCall_B26_8CC0)
    TXA                                 ; 8CC0  8a
    PHA                                 ; 8CC1  48
    LDA #$60                            ; 8CC2  a9 60
    JSR sub_30_C72B                     ; 8CC4  20 2b c7
    LDX #$10                            ; 8CC7  a2 10
    LDA #$20                            ; 8CC9  a9 20
loc_26_8CCB:
    STA wram_6060,X                     ; 8CCB  9d 60 60
    DEX                                 ; 8CCE  ca
    BPL loc_26_8CCB                     ; 8CCF  10 fa
    LDA player_hp_max                   ; 8CD1  ad c0 03
    SEC                                 ; 8CD4  38
    SBC #$01                            ; 8CD5  e9 01
    BCS loc_26_8CDB                     ; 8CD7  b0 02
    LDA #$00                            ; 8CD9  a9 00
loc_26_8CDB:
    LSR A                               ; 8CDB  4a
    LSR A                               ; 8CDC  4a
    LSR A                               ; 8CDD  4a
    LSR A                               ; 8CDE  4a
    TAX                                 ; 8CDF  aa
    LDA #$8D                            ; 8CE0  a9 8d
    STA $6061,X                         ; 8CE2  9d 61 60
    LDA #$8C                            ; 8CE5  a9 8c
loc_26_8CE7:
    STA wram_6060,X                     ; 8CE7  9d 60 60
    DEX                                 ; 8CEA  ca
    BPL loc_26_8CE7                     ; 8CEB  10 fa
    LDA player_hp                       ; 8CED  ad c1 03
    SEC                                 ; 8CF0  38
    SBC #$01                            ; 8CF1  e9 01
    BCS loc_26_8CF7                     ; 8CF3  b0 02
    LDA #$00                            ; 8CF5  a9 00
loc_26_8CF7:
    LSR A                               ; 8CF7  4a
    LSR A                               ; 8CF8  4a
    LSR A                               ; 8CF9  4a
    LSR A                               ; 8CFA  4a
    TAX                                 ; 8CFB  aa
    TAY                                 ; 8CFC  a8
    BEQ loc_26_8D07                     ; 8CFD  f0 08
    LDA #$88                            ; 8CFF  a9 88
loc_26_8D01:
    STA wram_605F,X                     ; 8D01  9d 5f 60
    DEX                                 ; 8D04  ca
    BNE loc_26_8D01                     ; 8D05  d0 fa
loc_26_8D07:
    LDA player_hp                       ; 8D07  ad c1 03
    SEC                                 ; 8D0A  38
    SBC #$01                            ; 8D0B  e9 01
    BCS loc_26_8D11                     ; 8D0D  b0 02
    LDA #$00                            ; 8D0F  a9 00
loc_26_8D11:
    AND #$0F                            ; 8D11  29 0f
    TAX                                 ; 8D13  aa
    LDA $8D49,X                         ; 8D14  bd 49 8d
    STA wram_6060,Y                     ; 8D17  99 60 60
    JSR sub_30_C43E                     ; 8D1A  20 3e c4
    LDX tmp_0B                          ; 8D1D  a6 0b
    LDA #$26                            ; 8D1F  a9 26
    STA ppu_queue_1,X                   ; 8D21  9d 01 62
    LDA #$2B                            ; 8D24  a9 2b
    STA ppu_queue,X                     ; 8D26  9d 00 62
    LDA #$11                            ; 8D29  a9 11
    STA ppu_queue_2,X                   ; 8D2B  9d 02 62
    LDA #$60                            ; 8D2E  a9 60
    STA ppu_queue_3,X                   ; 8D30  9d 03 62
    TXA                                 ; 8D33  8a
    CLC                                 ; 8D34  18
    ADC #$04                            ; 8D35  69 04
    AND #$1F                            ; 8D37  29 1f
    STA tmp_0B                          ; 8D39  85 0b
    LDA ppumask_shadow                  ; 8D3B  a5 01
    AND #$18                            ; 8D3D  29 18
    BNE loc_26_8D44                     ; 8D3F  d0 03
    JSR sub_30_C8B2                     ; 8D41  20 b2 c8
loc_26_8D44:
    PLA                                 ; 8D44  68
    TAX                                 ; 8D45  aa
    JMP sub_30_C436                     ; 8D46  4c 36 c4
    .db $8C,$8B,$8B,$8B,$8B,$8A,$8A,$8A,$8A,$89,$89,$89,$89,$88,$88,$88; 26:8D49
sub_26_8D59:
    TXA                                 ; 8D59  8a
    PHA                                 ; 8D5A  48
    LDA #$60                            ; 8D5B  a9 60
    JSR sub_30_C72B                     ; 8D5D  20 2b c7
    LDX #$06                            ; 8D60  a2 06
    LDA #$8F                            ; 8D62  a9 8f
loc_26_8D64:
    STA wram_6060,X                     ; 8D64  9d 60 60
    DEX                                 ; 8D67  ca
    BPL loc_26_8D64                     ; 8D68  10 fa
    LDA orbit_level                     ; 8D6A  ad c0 06
    AND #$07                            ; 8D6D  29 07
    TAX                                 ; 8D6F  aa
    BEQ loc_26_8D7A                     ; 8D70  f0 08
    LDA #$8E                            ; 8D72  a9 8e
loc_26_8D74:
    STA wram_605F,X                     ; 8D74  9d 5f 60
    DEX                                 ; 8D77  ca
    BNE loc_26_8D74                     ; 8D78  d0 fa
loc_26_8D7A:
    LDA sword_power                     ; 8D7A  ad 19 07
    ASL A                               ; 8D7D  0a
    ASL A                               ; 8D7E  0a
    ASL A                               ; 8D7F  0a
    STA jmp_trampoline                  ; 8D80  85 10
    LDA orbit_level                     ; 8D82  ad c0 06
    BEQ loc_26_8D91                     ; 8D85  f0 0a
    LDA tmp_08                          ; 8D87  a5 08
    AND #$04                            ; 8D89  29 04
    LSR A                               ; 8D8B  4a
    LSR A                               ; 8D8C  4a
    ORA jmp_trampoline                  ; 8D8D  05 10
    STA jmp_trampoline                  ; 8D8F  85 10
loc_26_8D91:
    LDA orbit_level                     ; 8D91  ad c0 06
    AND #$18                            ; 8D94  29 18
    LSR A                               ; 8D96  4a
    LSR A                               ; 8D97  4a
    ORA jmp_trampoline                  ; 8D98  05 10
    TAX                                 ; 8D9A  aa
    LDA ChargeMeterIndex,X              ; 8D9B  bd dd 8d
    TAX                                 ; 8D9E  aa
    LDY #$00                            ; 8D9F  a0 00
loc_26_8DA1:
    LDA ChargeMeterTiles,X              ; 8DA1  bd f5 8d
    STA $6067,Y                         ; 8DA4  99 67 60
    INX                                 ; 8DA7  e8
    INY                                 ; 8DA8  c8
    CPY #$09                            ; 8DA9  c0 09
    BNE loc_26_8DA1                     ; 8DAB  d0 f4
    JSR sub_30_C43E                     ; 8DAD  20 3e c4
    LDX tmp_0B                          ; 8DB0  a6 0b
    LDA #$46                            ; 8DB2  a9 46
    STA ppu_queue_1,X                   ; 8DB4  9d 01 62
    LDA #$2B                            ; 8DB7  a9 2b
    STA ppu_queue,X                     ; 8DB9  9d 00 62
    LDA #$10                            ; 8DBC  a9 10
    STA ppu_queue_2,X                   ; 8DBE  9d 02 62
    LDA #$60                            ; 8DC1  a9 60
    STA ppu_queue_3,X                   ; 8DC3  9d 03 62
    TXA                                 ; 8DC6  8a
    CLC                                 ; 8DC7  18
    ADC #$04                            ; 8DC8  69 04
    AND #$1F                            ; 8DCA  29 1f
    STA tmp_0B                          ; 8DCC  85 0b
    LDA ppumask_shadow                  ; 8DCE  a5 01
    AND #$18                            ; 8DD0  29 18
    BNE loc_26_8DD7                     ; 8DD2  d0 03
    JSR sub_30_C8B2                     ; 8DD4  20 b2 c8
loc_26_8DD7:
    JSR sub_30_C436                     ; 8DD7  20 36 c4
    PLA                                 ; 8DDA  68
    TAX                                 ; 8DDB  aa
    RTS                                 ; 8DDC  60
ChargeMeterIndex:  ; u8[] index ($0719*8 | blink | stage*2) -> offset into ChargeMeterTiles
    .db $00,$00,$00,$09,$00,$00,$00,$00,$12,$12,$12,$1B,$12,$24,$12,$12; 26:8DDD
    .db $2D,$2D,$2D,$36,$2D,$3F,$2D,$48 ; 26:8DED
ChargeMeterTiles:  ; 9-byte HUD tile rows copied to $6067-$606F
    .db $90,$91,$92,$20,$20,$20,$20,$20,$20,$93,$95,$94,$20,$20,$20,$20; 26:8DF5
    .db $20,$20,$90,$91,$92,$90,$91,$92,$20,$20,$20,$93,$95,$94,$90,$91; 26:8E05
    .db $92,$20,$20,$20,$90,$91,$92,$93,$96,$94,$20,$20,$20,$90,$91,$92; 26:8E15
    .db $90,$91,$92,$90,$91,$92,$93,$95,$94,$90,$91,$92,$90,$91,$92,$90; 26:8E25
    .db $91,$92,$93,$96,$94,$90,$91,$92,$90,$91,$92,$90,$91,$92,$93,$97; 26:8E35
    .db $94                             ; 26:8E45
HudRedraw:  ; A=element 0-6; records 26:8EC5 (value ptr; PPU addr; digits; flags)
    STX tmp_14                          ; 8E46  86 14
    PHA                                 ; 8E48  48
    LDA #$7B                            ; 8E49  a9 7b
    JSR sub_30_C72B                     ; 8E4B  20 2b c7
    PLA                                 ; 8E4E  68
    ASL A                               ; 8E4F  0a
    STA jmp_trampoline                  ; 8E50  85 10
    ASL A                               ; 8E52  0a
    CLC                                 ; 8E53  18
    ADC jmp_trampoline                  ; 8E54  65 10
    TAX                                 ; 8E56  aa
    STX tmp_18                          ; 8E57  86 18
    LDA HudElemTable,X                  ; 8E59  bd c5 8e
    STA tmp_12                          ; 8E5C  85 12
    LDA $8EC6,X                         ; 8E5E  bd c6 8e
    STA tmp_13                          ; 8E61  85 13
    LDY #$00                            ; 8E63  a0 00
    LDA (tmp_12),Y                      ; 8E65  b1 12
    STA jmp_trampoline                  ; 8E67  85 10
    INY                                 ; 8E69  c8
    LDA (tmp_12),Y                      ; 8E6A  b1 12
    STA tmp_11                          ; 8E6C  85 11
    LDA $8ECA,X                         ; 8E6E  bd ca 8e
    BNE loc_26_8E77                     ; 8E71  d0 04
    LDA #$00                            ; 8E73  a9 00
    STA tmp_11                          ; 8E75  85 11
loc_26_8E77:
    JSR sub_26_8F2B                     ; 8E77  20 2b 8f
    LDY tmp_18                          ; 8E7A  a4 18
    LDA $8EC9,Y                         ; 8E7C  b9 c9 8e
    CLC                                 ; 8E7F  18
    ADC #$04                            ; 8E80  69 04
    TAX                                 ; 8E82  aa
    LDY #$04                            ; 8E83  a0 04
loc_26_8E85:
    LDA tmp_20,X                        ; 8E85  b5 20
    STA $607B,Y                         ; 8E87  99 7b 60
    DEX                                 ; 8E8A  ca
    DEY                                 ; 8E8B  88
    BPL loc_26_8E85                     ; 8E8C  10 f7
    LDY tmp_18                          ; 8E8E  a4 18
    JSR sub_30_C43E                     ; 8E90  20 3e c4
    LDX tmp_0B                          ; 8E93  a6 0b
    LDA $8EC7,Y                         ; 8E95  b9 c7 8e
    STA ppu_queue_1,X                   ; 8E98  9d 01 62
    LDA $8EC8,Y                         ; 8E9B  b9 c8 8e
    STA ppu_queue,X                     ; 8E9E  9d 00 62
    SEC                                 ; 8EA1  38
    LDA #$05                            ; 8EA2  a9 05
    SBC $8EC9,Y                         ; 8EA4  f9 c9 8e
    STA ppu_queue_2,X                   ; 8EA7  9d 02 62
    LDA #$7B                            ; 8EAA  a9 7b
    STA ppu_queue_3,X                   ; 8EAC  9d 03 62
    TXA                                 ; 8EAF  8a
    CLC                                 ; 8EB0  18
    ADC #$04                            ; 8EB1  69 04
    AND #$1F                            ; 8EB3  29 1f
    STA tmp_0B                          ; 8EB5  85 0b
    LDA ppumask_shadow                  ; 8EB7  a5 01
    AND #$18                            ; 8EB9  29 18
    BNE loc_26_8EC0                     ; 8EBB  d0 03
    JSR sub_30_C8B2                     ; 8EBD  20 b2 c8
loc_26_8EC0:
    LDX tmp_14                          ; 8EC0  a6 14
    JMP sub_30_C436                     ; 8EC2  4c 36 c4
HudElemTable:  ; 7 x 6 bytes
    .db $21,$04,$39,$2B,$03,$00,$02,$07,$59,$2B,$00,$40,$04,$07,$68,$2B; 26:8EC5
    .db $00,$40,$06,$07,$6E,$2B,$00,$40,$08,$07,$77,$2B,$02,$00,$09,$07; 26:8ED5
    .db $7B,$2B,$02,$00,$21,$04,$29,$29,$03,$00,$C1,$03,$69,$29,$02,$00; 26:8EE5
    .db $C0,$03,$6D,$29,$02,$00,$E1,$03,$39,$29,$03,$00,$01,$04,$79,$29; 26:8EF5
    .db $03,$00,$74,$64,$F9,$28,$00,$40,$74,$64,$F7,$28,$02,$40,$74,$64; 26:8F05
    .db $99,$2A,$00,$40,$DE,$07,$03,$29,$03,$00,$DF,$07,$D9,$28,$03,$00; 26:8F15
    .db $00,$04,$7C,$29,$03,$00         ; 26:8F25
sub_26_8F2B:
    LDA #$00                            ; 8F2B  a9 00
    STA tmp_20                          ; 8F2D  85 20
    STA tmp_21                          ; 8F2F  85 21
    STA tmp_22                          ; 8F31  85 22
    STA tmp_23                          ; 8F33  85 23
    STA tmp_24                          ; 8F35  85 24
    LDA #$08                            ; 8F37  a9 08
    STA tmp_12                          ; 8F39  85 12
loc_26_8F3B:
    LDX tmp_12                          ; 8F3B  a6 12
    LDA jmp_trampoline                  ; 8F3D  a5 10
    SEC                                 ; 8F3F  38
    SBC $8F64,X                         ; 8F40  fd 64 8f
    TAY                                 ; 8F43  a8
    LDA tmp_11                          ; 8F44  a5 11
    SBC $8F65,X                         ; 8F46  fd 65 8f
    BCC loc_26_8F5D                     ; 8F49  90 12
    STY jmp_trampoline                  ; 8F4B  84 10
    STA tmp_11                          ; 8F4D  85 11
    STX tmp_13                          ; 8F4F  86 13
    LDA #$08                            ; 8F51  a9 08
    SEC                                 ; 8F53  38
    SBC tmp_13                          ; 8F54  e5 13
    LSR A                               ; 8F56  4a
    TAX                                 ; 8F57  aa
    INC tmp_20,X                        ; 8F58  f6 20
    JMP loc_26_8F3B                     ; 8F5A  4c 3b 8f
loc_26_8F5D:
    DEC tmp_12                          ; 8F5D  c6 12
    DEC tmp_12                          ; 8F5F  c6 12
    BPL loc_26_8F3B                     ; 8F61  10 d8
    RTS                                 ; 8F63  60
    .db $01,$00,$0A,$00,$64,$00,$E8,$03,$10,$27; 26:8F64
ObjCollideAll:  ; s30: per-frame obj-vs-obj collision (from 30:CB7E); CollPairTable records 7..0 on even frames / 14..8 on odd ($08 b0)
    LDA #$00                            ; 8F6E  a9 00
    STA ride_platform_slot              ; 8F70  8d 41 05
    STA obj_act_timer_s03               ; 8F73  8d 43 06
    LDX #$07                            ; 8F76  a2 07
    LDY #$08                            ; 8F78  a0 08
    LDA tmp_08                          ; 8F7A  a5 08
    LSR A                               ; 8F7C  4a
    BCC loc_26_8F83                     ; 8F7D  90 04
    LDX #$0E                            ; 8F7F  a2 0e
    LDY #$07                            ; 8F81  a0 07
loc_26_8F83:
    STX tmp_2F                          ; 8F83  86 2f
    STY tmp_2E                          ; 8F85  84 2e
loc_26_8F87:
    LDA tmp_2F                          ; 8F87  a5 2f
    ASL A                               ; 8F89  0a
    ASL A                               ; 8F8A  0a
    TAY                                 ; 8F8B  a8
    LDA CollPairTable,Y                 ; 8F8C  b9 f1 96
    TAX                                 ; 8F8F  aa
    LDA $96F2,Y                         ; 8F90  b9 f2 96
    STA jmp_trampoline                  ; 8F93  85 10
    LDA $96F3,Y                         ; 8F95  b9 f3 96
    STA tmp_11                          ; 8F98  85 11
    LDA $96F4,Y                         ; 8F9A  b9 f4 96
    TAY                                 ; 8F9D  a8
    LDA #$4C                            ; 8F9E  a9 4c
    STA tmp_20                          ; 8FA0  85 20
    LDA HitHandlerTable,Y               ; 8FA2  b9 bb 8f
    STA tmp_21                          ; 8FA5  85 21
    LDA $8FBC,Y                         ; 8FA7  b9 bc 8f
    STA tmp_22                          ; 8FAA  85 22
    JSR ObjOverlapScan                  ; 8FAC  20 f8 95
    BCC loc_26_8FB4                     ; 8FAF  90 03
    JSR a:tmp_20                        ; 8FB1  20 20 00
loc_26_8FB4:
    DEC tmp_2F                          ; 8FB4  c6 2f
    DEC tmp_2E                          ; 8FB6  c6 2e
    BNE loc_26_8F87                     ; 8FB8  d0 cd
    RTS                                 ; 8FBA  60
HitHandlerTable:  ; 4 words: object hit handlers ($90AA $9357 $94B0 $8FC4)
    .dw sub_26_90AA                     ; 26:8FBB
    .dw HitPlayer                       ; 26:8FBD
    .dw sub_26_94B0                     ; 26:8FBF
    .dw HitHandler3                     ; 26:8FC1
loc_26_8FC3:
    RTS                                 ; 8FC3  60
HitHandler3:  ; object Y hit: kill + HitSetFlag
    LDA obj_phase,X                     ; 8FC4  bd 00 06
    CMP #$01                            ; 8FC7  c9 01
    BNE loc_26_8FCE                     ; 8FC9  d0 03
    JMP loc_26_906C                     ; 8FCB  4c 6c 90
loc_26_8FCE:
    JSR sub_26_907C                     ; 8FCE  20 7c 90
    BEQ loc_26_8FC3                     ; 8FD1  f0 f0
    LDA obj_active,Y                    ; 8FD3  b9 a0 04
    CMP #$78                            ; 8FD6  c9 78
    BEQ loc_26_8FC3                     ; 8FD8  f0 e9
    LDA #$00                            ; 8FDA  a9 00
    STA obj_active,X                    ; 8FDC  9d a0 04
    LDA obj_hitflags,Y                  ; 8FDF  b9 20 04
    AND #$20                            ; 8FE2  29 20
    BNE loc_26_8FC3                     ; 8FE4  d0 dd
    LDA obj_spawn_param,Y               ; 8FE6  b9 60 05
    STA tmp_13                          ; 8FE9  85 13
    LDA #$FF                            ; 8FEB  a9 ff
    STA tmp_12                          ; 8FED  85 12
    LDA #$FF                            ; 8FEF  a9 ff
    STA obj_freeze,Y                    ; 8FF1  99 a0 05
    STY jmp_trampoline                  ; 8FF4  84 10
    LDA obj_spawn_b2,Y                  ; 8FF6  b9 40 05
    CMP #$01                            ; 8FF9  c9 01
    BNE loc_26_9044                     ; 8FFB  d0 47
    LDY #$13                            ; 8FFD  a0 13
loc_26_8FFF:
    LDA HitImmuneParams,Y               ; 8FFF  b9 58 90
    CMP tmp_13                          ; 9002  c5 13
    BNE loc_26_900E                     ; 9004  d0 08
    LDY jmp_trampoline                  ; 9006  a4 10
    LDA #$00                            ; 9008  a9 00
    STA obj_freeze,Y                    ; 900A  99 a0 05
    RTS                                 ; 900D  60
loc_26_900E:
    DEY                                 ; 900E  88
    BPL loc_26_8FFF                     ; 900F  10 ee
HitSetFlag:  ; param $13 in 26:9045 list -> set flag 26:904F[i]
    LDY #$00                            ; 9011  a0 00
loc_26_9013:
    LDA HitFlagParams,Y                 ; 9013  b9 45 90
    BEQ loc_26_9044                     ; 9016  f0 2c
    CMP tmp_13                          ; 9018  c5 13
    BEQ loc_26_9020                     ; 901A  f0 04
    INY                                 ; 901C  c8
    JMP loc_26_9013                     ; 901D  4c 13 90
loc_26_9020:
    LDA HitFlagIds,Y                    ; 9020  b9 4f 90
    PHA                                 ; 9023  48
    AND #$07                            ; 9024  29 07
    TAY                                 ; 9026  a8
    LDA BitMask,Y                       ; 9027  b9 00 c0
    EOR #$FF                            ; 902A  49 ff
    STA tmp_11                          ; 902C  85 11
    LDA BitMask,Y                       ; 902E  b9 00 c0
    AND tmp_12                          ; 9031  25 12
    STA jmp_trampoline                  ; 9033  85 10
    PLA                                 ; 9035  68
    LSR A                               ; 9036  4a
    LSR A                               ; 9037  4a
    LSR A                               ; 9038  4a
    TAY                                 ; 9039  a8
    LDA story_flags,Y                   ; 903A  b9 80 64
    AND tmp_11                          ; 903D  25 11
    ORA jmp_trampoline                  ; 903F  05 10
    STA story_flags,Y                   ; 9041  99 80 64
loc_26_9044:
    RTS                                 ; 9044  60
HitFlagParams:  ; 0-terminated spawn params: 26 27 5E 5F 62 6D 6C 34 25
    .db $26,$27,$5E,$5F,$62,$6D,$6C,$34,$25,$00; 26:9045
HitFlagIds:  ; flag ids (F0xx) for HitFlagParams
    .db $58,$59,$6D,$6E,$6F,$70,$71,$A1,$99; 26:904F
HitImmuneParams:  ; 20 params immune when obj_spawn_b2 == 1
    .db $7E,$74,$38,$39,$73,$83,$6E,$76,$4E,$63,$69,$70,$71,$72,$68,$6B; 26:9058
    .db $75,$8E,$27,$26                 ; 26:9068
loc_26_906C:
    JSR sub_26_907C                     ; 906C  20 7c 90
    BNE loc_26_907B                     ; 906F  d0 0a
    LDA #$00                            ; 9071  a9 00
    STA obj_active,Y                    ; 9073  99 a0 04
    LDA #$3A                            ; 9076  a9 3a
    JMP PlaySound                       ; 9078  4c 25 c1
loc_26_907B:
    RTS                                 ; 907B  60
sub_26_907C:
    LDA obj_spawn_b2,Y                  ; 907C  b9 40 05
    CMP #$FF                            ; 907F  c9 ff
    RTS                                 ; 9081  60
loc_26_9082:
    LDA obj_hitflags,X                  ; 9082  bd 20 04
    CMP #$03                            ; 9085  c9 03
    BNE loc_26_9093                     ; 9087  d0 0a
    LDA obj_death_type,Y                ; 9089  b9 c0 04
    STA tmp_11                          ; 908C  85 11
    STY jmp_trampoline                  ; 908E  84 10
    JMP FarCall_C25D                    ; 9090  4c 80 ff
loc_26_9093:
    RTS                                 ; 9093  60
loc_26_9094:
    LDA obj_elem_drop,Y                 ; 9094  b9 00 05
    AND obj_elem_drop,X                 ; 9097  3d 00 05
    AND #$0F                            ; 909A  29 0f
    BNE loc_26_9093                     ; 909C  d0 f5
    LDA obj_hitflags,X                  ; 909E  bd 20 04
    CMP #$01                            ; 90A1  c9 01
    BEQ loc_26_9093                     ; 90A3  f0 ee
    BEQ sub_26_90AA                     ; 90A5  f0 03
    JMP EnemyKilled                     ; 90A7  4c 52 91
sub_26_90AA:
    LDA area_id                         ; 90AA  a5 6c
    CMP #$5F                            ; 90AC  c9 5f
    BNE loc_26_90BB                     ; 90AE  d0 0b
    LDA #$00                            ; 90B0  a9 00
    STA obj_active_s04                  ; 90B2  8d a4 04
    STA $04A5                           ; 90B5  8d a5 04
    STA $04A6                           ; 90B8  8d a6 04
loc_26_90BB:
    JSR sub_26_9229                     ; 90BB  20 29 92
    LDA obj_active,Y                    ; 90BE  b9 a0 04
    CMP #$52                            ; 90C1  c9 52
    BNE loc_26_90E4                     ; 90C3  d0 1f
    DEC obj_aux_s0D                     ; 90C5  ce 6d 06
    LDA obj_aux_s0D                     ; 90C8  ad 6d 06
    AND #$03                            ; 90CB  29 03
    BNE loc_26_90DF                     ; 90CD  d0 10
    LDA obj_y_lo                        ; 90CF  a5 b0
    SEC                                 ; 90D1  38
    SBC #$08                            ; 90D2  e9 08
    STA obj_y_lo                        ; 90D4  85 b0
    LDA a:obj_y_lo,Y                    ; 90D6  b9 b0 00
    SEC                                 ; 90D9  38
    SBC #$08                            ; 90DA  e9 08
    STA a:obj_y_lo,Y                    ; 90DC  99 b0 00
loc_26_90DF:
    LDA #$21                            ; 90DF  a9 21
    JMP PlaySound                       ; 90E1  4c 25 c1
loc_26_90E4:
    LDA obj_active,Y                    ; 90E4  b9 a0 04
    CMP #$10                            ; 90E7  c9 10
    BNE loc_26_90EC                     ; 90E9  d0 01
    RTS                                 ; 90EB  60
loc_26_90EC:
    LDA obj_active,Y                    ; 90EC  b9 a0 04
    CMP #$87                            ; 90EF  c9 87
    BEQ loc_26_9094                     ; 90F1  f0 a1
    LDA obj_meta_id,Y                   ; 90F3  b9 00 03
    CMP #$A7                            ; 90F6  c9 a7
    BEQ loc_26_9082                     ; 90F8  f0 88
    LDA obj_hitflags,Y                  ; 90FA  b9 20 04
    AND #$1F                            ; 90FD  29 1f
    STA jmp_trampoline                  ; 90FF  85 10
    LDA player_level                    ; 9101  ad 21 04
    CMP jmp_trampoline                  ; 9104  c5 10
    BCC loc_26_9125                     ; 9106  90 1d
    LDA $03E1                           ; 9108  ad e1 03
    CLC                                 ; 910B  18
    ADC obj_attack,X                    ; 910C  7d e0 03
    STA jmp_trampoline                  ; 910F  85 10
    LDA obj_elem_drop,Y                 ; 9111  b9 00 05
    AND obj_elem_drop,X                 ; 9114  3d 00 05
    AND #$0F                            ; 9117  29 0f
    BNE loc_26_9125                     ; 9119  d0 0a
    LDA jmp_trampoline                  ; 911B  a5 10
    SEC                                 ; 911D  38
    SBC obj_defense,Y                   ; 911E  f9 00 04
    STA jmp_trampoline                  ; 9121  85 10
    BCS loc_26_913B                     ; 9123  b0 16
loc_26_9125:
    LDA #$00                            ; 9125  a9 00
    STA obj_active,X                    ; 9127  9d a0 04
    STX jmp_trampoline                  ; 912A  86 10
    STY tmp_11                          ; 912C  84 11
    LDA obj_active,Y                    ; 912E  b9 a0 04
    BMI loc_26_9136                     ; 9131  30 03
    JSR HitKnockbackSet                 ; 9133  20 c0 95
loc_26_9136:
    LDA #$3A                            ; 9136  a9 3a
    JMP PlaySound                       ; 9138  4c 25 c1
loc_26_913B:
    LDA player_hp_max,Y                 ; 913B  b9 c0 03
    SEC                                 ; 913E  38
    SBC jmp_trampoline                  ; 913F  e5 10
    STA player_hp_max,Y                 ; 9141  99 c0 03
    BCC EnemyKilled                     ; 9144  90 0c
    STX jmp_trampoline                  ; 9146  86 10
    STY tmp_11                          ; 9148  84 11
    JSR HitKnockbackSet                 ; 914A  20 c0 95
    LDA #$21                            ; 914D  a9 21
    JMP PlaySound                       ; 914F  4c 25 c1
EnemyKilled:  ; HP underflow: AddXp; $04C0=0 -> EnemyDeathFx else ObjInit(slot;$04C0); level-up check (XP $0704 >= $0706)
    JSR AddXp                           ; 9152  20 4b 92
    LDA obj_death_type,Y                ; 9155  b9 c0 04
    BNE loc_26_9160                     ; 9158  d0 06
    JSR EnemyDeathFx                    ; 915A  20 fa 91
    JMP loc_26_916A                     ; 915D  4c 6a 91
loc_26_9160:
    STY jmp_trampoline                  ; 9160  84 10
    LDA obj_death_type,Y                ; 9162  b9 c0 04
    STA tmp_11                          ; 9165  85 11
    JSR FarCall_C25D                    ; 9167  20 80 ff
loc_26_916A:
    LDA xp                              ; 916A  ad 04 07
    SEC                                 ; 916D  38
    SBC xp_next                         ; 916E  ed 06 07
    LDA xp_hi                           ; 9171  ad 05 07
    SBC xp_next_hi                      ; 9174  ed 07 07
    BCC loc_26_91EF                     ; 9177  90 76
    LDA #$00                            ; 9179  a9 00
    STA xp                              ; 917B  8d 04 07
    STA xp_hi                           ; 917E  8d 05 07
    LDA player_level                    ; 9181  ad 21 04
    AND #$F0                            ; 9184  29 f0
    BNE loc_26_91EF                     ; 9186  d0 67
    INC player_level                    ; 9188  ee 21 04
    LDY player_level                    ; 918B  ac 21 04
    LDA LevelHpTable,Y                  ; 918E  b9 7f 8b
    STA player_hp_max                   ; 9191  8d c0 03
    LDA LevelMpTable,Y                  ; 9194  b9 8f 8b
    STA mp_max                          ; 9197  8d 09 07
    SEC                                 ; 919A  38
    LDA LevelHpTable,Y                  ; 919B  b9 7f 8b
    SBC $8B7E,Y                         ; 919E  f9 7e 8b
    CLC                                 ; 91A1  18
    ADC player_hp                       ; 91A2  6d c1 03
    STA player_hp                       ; 91A5  8d c1 03
    SEC                                 ; 91A8  38
    LDA LevelMpTable,Y                  ; 91A9  b9 8f 8b
    SBC $8B8E,Y                         ; 91AC  f9 8e 8b
    CLC                                 ; 91AF  18
    ADC mp_cur                          ; 91B0  6d 08 07
    STA mp_cur                          ; 91B3  8d 08 07
    LDA player_level                    ; 91B6  ad 21 04
    ASL A                               ; 91B9  0a
    TAY                                 ; 91BA  a8
    LDA LevelXpTable,Y                  ; 91BB  b9 9e 8b
    STA xp_next                         ; 91BE  8d 06 07
    LDA $8B9F,Y                         ; 91C1  b9 9f 8b
    STA xp_next_hi                      ; 91C4  8d 07 07
    JSR HudHpBar                        ; 91C7  20 c0 8c
    LDA #$00                            ; 91CA  a9 00
    JSR HudRedraw                       ; 91CC  20 46 8e
    LDA #$03                            ; 91CF  a9 03
    JSR HudRedraw                       ; 91D1  20 46 8e
    LDA #$04                            ; 91D4  a9 04
    JSR HudRedraw                       ; 91D6  20 46 8e
    LDA #$05                            ; 91D9  a9 05
    JSR HudRedraw                       ; 91DB  20 46 8e
    LDA #$10                            ; 91DE  a9 10
    STA sub_state                       ; 91E0  85 41
    LDA #$0D                            ; 91E2  a9 0d
    STA pending_msg_id                  ; 91E4  8d c3 06
    LDA #$20                            ; 91E7  a9 20
    STA pending_msg_group               ; 91E9  8d e3 06
    JSR sub_30_C008                     ; 91EC  20 08 c0
loc_26_91EF:
    LDA #$02                            ; 91EF  a9 02
    JSR HudRedraw                       ; 91F1  20 46 8e
    LDA #$21                            ; 91F4  a9 21
    JSR PlaySound                       ; 91F6  20 25 c1
    RTS                                 ; 91F9  60
EnemyDeathFx:  ; obj Y -> AI $7D death puff (overlay $AB; anim $1F; $04C0=$0B)
    LDA #$AB                            ; 91FA  a9 ab
    STA obj_overlay_id,Y                ; 91FC  99 80 05
    LDA obj_flags,Y                     ; 91FF  b9 80 03
    ORA #$02                            ; 9202  09 02
    AND #$EF                            ; 9204  29 ef
    STA obj_flags,Y                     ; 9206  99 80 03
    LDA #$08                            ; 9209  a9 08
    STA obj_flags2,Y                    ; 920B  99 40 03
    LDA #$7D                            ; 920E  a9 7d
    STA obj_active,Y                    ; 9210  99 a0 04
    LDA #$1F                            ; 9213  a9 1f
    STA obj_anim,Y                      ; 9215  99 e0 04
    LDA #$00                            ; 9218  a9 00
    STA obj_phase,Y                     ; 921A  99 00 06
    STA obj_coll,Y                      ; 921D  99 a0 03
    STA obj_freeze,Y                    ; 9220  99 a0 05
    LDA #$0B                            ; 9223  a9 0b
    STA obj_death_type,Y                ; 9225  99 c0 04
    RTS                                 ; 9228  60
sub_26_9229:
    LDA equip_sword                     ; 9229  ad 11 07
    CMP #$04                            ; 922C  c9 04
    BNE loc_26_924A                     ; 922E  d0 1a
    LDA obj_spawn_b2,Y                  ; 9230  b9 40 05
    BNE loc_26_924A                     ; 9233  d0 15
    LDA obj_spawn_param,Y               ; 9235  b9 60 05
    BEQ loc_26_923E                     ; 9238  f0 04
    CMP #$03                            ; 923A  c9 03
    BNE loc_26_924A                     ; 923C  d0 0c
loc_26_923E:
    LDA obj_behav,Y                     ; 923E  b9 e0 06
    STA tmp_11                          ; 9241  85 11
    STY jmp_trampoline                  ; 9243  84 10
    JSR FarCall_C25D                    ; 9245  20 80 ff
    PLA                                 ; 9248  68
    PLA                                 ; 9249  68
loc_26_924A:
    RTS                                 ; 924A  60
AddXp:  ; $0520;Y xp value: bit7 clear -> +value; set -> +(value&$7F)*16 (to $0704/$0705)
    LDA obj_xp_value,Y                  ; 924B  b9 20 05
    BMI loc_26_925D                     ; 924E  30 0d
    CLC                                 ; 9250  18
    ADC xp                              ; 9251  6d 04 07
    STA xp                              ; 9254  8d 04 07
    BCC loc_26_925C                     ; 9257  90 03
    INC xp_hi                           ; 9259  ee 05 07
loc_26_925C:
    RTS                                 ; 925C  60
loc_26_925D:
    LDA #$00                            ; 925D  a9 00
    STA tmp_11                          ; 925F  85 11
    LDA obj_xp_value,Y                  ; 9261  b9 20 05
    ASL A                               ; 9264  0a
    ASL A                               ; 9265  0a
    ROL tmp_11                          ; 9266  26 11
    ASL A                               ; 9268  0a
    ROL tmp_11                          ; 9269  26 11
    ASL A                               ; 926B  0a
    ROL tmp_11                          ; 926C  26 11
    CLC                                 ; 926E  18
    ADC xp                              ; 926F  6d 04 07
    STA xp                              ; 9272  8d 04 07
    LDA tmp_11                          ; 9275  a5 11
    ADC xp_hi                           ; 9277  6d 05 07
    STA xp_hi                           ; 927A  8d 05 07
    RTS                                 ; 927D  60
PlayerPoisonRoll:  ; s46: toucher obj_hitflags b7; armor !=7; Rand64<4 -> status 3 poison; msg $20/$19; SFX $36
    LDA obj_flags,X                     ; 927E  bd 80 03
    AND #$04                            ; 9281  29 04
    BNE loc_26_92BE                     ; 9283  d0 39
    LDA equip_armor                     ; 9285  ad 13 07
    CMP #$07                            ; 9288  c9 07
    BEQ loc_26_92BE                     ; 928A  f0 32
    LDA obj_hitflags,Y                  ; 928C  b9 20 04
    BPL loc_26_92BE                     ; 928F  10 2d
    STY jmp_trampoline                  ; 9291  84 10
    JSR Rand64                          ; 9293  20 d7 97
    LDY jmp_trampoline                  ; 9296  a4 10
    CMP #$04                            ; 9298  c9 04
    BCS loc_26_92BE                     ; 929A  b0 22
    LDA obj1_flash                      ; 929C  ad 10 07
    AND #$F0                            ; 929F  29 f0
    ORA #$03                            ; 92A1  09 03
    STA obj1_flash                      ; 92A3  8d 10 07
    LDA #$10                            ; 92A6  a9 10
    STA sub_state                       ; 92A8  85 41
    LDA #$19                            ; 92AA  a9 19
    STA pending_msg_id                  ; 92AC  8d c3 06
    LDA #$20                            ; 92AF  a9 20
    STA pending_msg_group               ; 92B1  8d e3 06
    JSR PlayerInvuln                    ; 92B4  20 31 94
    LDA #$36                            ; 92B7  a9 36
    JSR PlaySound                       ; 92B9  20 25 c1
    LDY jmp_trampoline                  ; 92BC  a4 10
loc_26_92BE:
    RTS                                 ; 92BE  60
HitPlayerShot:  ; s46: enemy shot (spawn_b2 $FF) by spawn_param: 0 damage; 1 paralysis; 2 stone; 3 -> type $0B; >=4 status 4
    LDA obj_spawn_param,Y               ; 92BF  b9 60 05
    BNE loc_26_92C7                     ; 92C2  d0 03
    JMP loc_26_9361                     ; 92C4  4c 61 93
loc_26_92C7:
    CMP #$01                            ; 92C7  c9 01
    BNE loc_26_92FB                     ; 92C9  d0 30
    LDA equip_shield                    ; 92CB  ad 14 07
    CMP #$06                            ; 92CE  c9 06
    BEQ sub_26_92F5                     ; 92D0  f0 23
    CMP #$08                            ; 92D2  c9 08
    BEQ sub_26_92F5                     ; 92D4  f0 1f
    LDA #$44                            ; 92D6  a9 44
    JSR PlaySound                       ; 92D8  20 25 c1
    LDA #$10                            ; 92DB  a9 10
    STA sub_state                       ; 92DD  85 41
    LDA #$1A                            ; 92DF  a9 1a
    STA pending_msg_id                  ; 92E1  8d c3 06
    LDA #$20                            ; 92E4  a9 20
    STA pending_msg_group               ; 92E6  8d e3 06
    LDA obj1_flash                      ; 92E9  ad 10 07
    AND #$F0                            ; 92EC  29 f0
    ORA #$01                            ; 92EE  09 01
    STA obj1_flash                      ; 92F0  8d 10 07
    BNE sub_26_92F5                     ; 92F3  d0 00
sub_26_92F5:
    LDA #$00                            ; 92F5  a9 00
    STA obj_active,Y                    ; 92F7  99 a0 04
    RTS                                 ; 92FA  60
loc_26_92FB:
    CMP #$02                            ; 92FB  c9 02
    BNE loc_26_933F                     ; 92FD  d0 40
    LDA obj1_flash                      ; 92FF  ad 10 07
    AND #$0F                            ; 9302  29 0f
    CMP #$02                            ; 9304  c9 02
    BEQ sub_26_92F5                     ; 9306  f0 ed
    LDA equip_shield                    ; 9308  ad 14 07
    CMP #$04                            ; 930B  c9 04
    BEQ sub_26_92F5                     ; 930D  f0 e6
    CMP #$08                            ; 930F  c9 08
    BEQ sub_26_92F5                     ; 9311  f0 e2
    LDA #$10                            ; 9313  a9 10
    STA sub_state                       ; 9315  85 41
    LDA #$1B                            ; 9317  a9 1b
    STA pending_msg_id                  ; 9319  8d c3 06
    LDA #$20                            ; 931C  a9 20
    STA pending_msg_group               ; 931E  8d e3 06
    LDA #$45                            ; 9321  a9 45
    JSR PlaySound                       ; 9323  20 25 c1
    LDA #$FF                            ; 9326  a9 ff
    STA obj_freeze                      ; 9328  8d a0 05
    LDA #$4D                            ; 932B  a9 4d
    STA pal_id_spr0                     ; 932D  8d e4 07
    JSR PaletteBuild                    ; 9330  20 0e 8c
    LDA obj1_flash                      ; 9333  ad 10 07
    AND #$F0                            ; 9336  29 f0
    ORA #$02                            ; 9338  09 02
    STA obj1_flash                      ; 933A  8d 10 07
    BNE sub_26_92F5                     ; 933D  d0 b6
loc_26_933F:
    CMP #$03                            ; 933F  c9 03
    BNE loc_26_934C                     ; 9341  d0 09
    STY jmp_trampoline                  ; 9343  84 10
    LDA #$0B                            ; 9345  a9 0b
    STA tmp_11                          ; 9347  85 11
    JMP FarCall_C25D                    ; 9349  4c 80 ff
loc_26_934C:
    JSR sub_26_92F5                     ; 934C  20 f5 92
    LDA #$04                            ; 934F  a9 04
    STA obj1_flash                      ; 9351  8d 10 07
    JMP sub_30_C008                     ; 9354  4c 08 c0
HitPlayer:  ; s46: HitHandlerTable 2 (player touched): shots; poison; AI $0B hold; gold $7B; platforms $38/$3C; damage attack-defence; 30f invuln
    LDA obj_spawn_b2,Y                  ; 9357  b9 40 05
    CMP #$FF                            ; 935A  c9 ff
    BNE loc_26_9361                     ; 935C  d0 03
    JMP HitPlayerShot                   ; 935E  4c bf 92
loc_26_9361:
    JSR PlayerPoisonRoll                ; 9361  20 7e 92
    LDA obj_active                      ; 9364  ad a0 04
    CMP #$0B                            ; 9367  c9 0b
    BNE loc_26_9388                     ; 9369  d0 1d
    DEC obj_aux_s01                     ; 936B  ce 61 06
    LDA obj_aux_s01                     ; 936E  ad 61 06
    AND #$03                            ; 9371  29 03
    BNE loc_26_9383                     ; 9373  d0 0e
    LDA obj_y_lo                        ; 9375  a5 b0
    CLC                                 ; 9377  18
    ADC #$08                            ; 9378  69 08
    STA obj_y_lo                        ; 937A  85 b0
    LDA obj_y_lo_s0D                    ; 937C  a5 bd
    CLC                                 ; 937E  18
    ADC #$08                            ; 937F  69 08
    STA obj_y_lo_s0D                    ; 9381  85 bd
loc_26_9383:
    LDA #$5F                            ; 9383  a9 5f
    JMP PlaySound                       ; 9385  4c 25 c1
loc_26_9388:
    LDA obj_active,Y                    ; 9388  b9 a0 04
    AND #$7F                            ; 938B  29 7f
    CMP #$06                            ; 938D  c9 06
    BEQ loc_26_93FD                     ; 938F  f0 6c
    CMP #$07                            ; 9391  c9 07
    BEQ loc_26_93FD                     ; 9393  f0 68
    CMP #$7B                            ; 9395  c9 7b
    BNE loc_26_939C                     ; 9397  d0 03
    JMP PickupGold                      ; 9399  4c 6d 94
loc_26_939C:
    CMP #$38                            ; 939C  c9 38
    BEQ loc_26_93FE                     ; 939E  f0 5e
    CMP #$3C                            ; 93A0  c9 3c
    BEQ loc_26_93FE                     ; 93A2  f0 5a
    LDA obj_flags_s01                   ; 93A4  ad 81 03
    AND #$04                            ; 93A7  29 04
    BEQ loc_26_93AC                     ; 93A9  f0 01
    RTS                                 ; 93AB  60
loc_26_93AC:
    STX tmp_11                          ; 93AC  86 11
    STY jmp_trampoline                  ; 93AE  84 10
    JSR HitKnockbackSet                 ; 93B0  20 c0 95
    LDA area_id                         ; 93B3  a5 6c
    CMP #$1A                            ; 93B5  c9 1a
    BNE loc_26_93C6                     ; 93B7  d0 0d
    LDA boss_cam_lock                   ; 93B9  ad d7 07
    BPL loc_26_93C6                     ; 93BC  10 08
    LDA obj_meta_dir,X                  ; 93BE  bd 60 03
    AND #$0F                            ; 93C1  29 0f
    STA obj_meta_dir,X                  ; 93C3  9d 60 03
loc_26_93C6:
    LDA obj_meta_dir,X                  ; 93C6  bd 60 03
    STA obj_meta_dir                    ; 93C9  8d 60 03
    LDA obj_flags2,X                    ; 93CC  bd 40 03
    STA obj_flags2                      ; 93CF  8d 40 03
    LDA #$FF                            ; 93D2  a9 ff
    STA obj_act_timer                   ; 93D4  8d 40 06
    LDA obj_defense,X                   ; 93D7  bd 00 04
    STA jmp_trampoline                  ; 93DA  85 10
    LDA obj_spawn_b2,Y                  ; 93DC  b9 40 05
    CMP #$FF                            ; 93DF  c9 ff
    BNE loc_26_93E8                     ; 93E1  d0 05
    LDA obj_defense                     ; 93E3  ad 00 04
    STA jmp_trampoline                  ; 93E6  85 10
loc_26_93E8:
    LDA jmp_trampoline                  ; 93E8  a5 10
    EOR #$FF                            ; 93EA  49 ff
    CLC                                 ; 93EC  18
    ADC #$01                            ; 93ED  69 01
    CLC                                 ; 93EF  18
    ADC obj_attack,Y                    ; 93F0  79 e0 03
    BPL loc_26_9401                     ; 93F3  10 0c
    LDA #$3A                            ; 93F5  a9 3a
    JSR PlaySound                       ; 93F7  20 25 c1
    JMP loc_26_9422                     ; 93FA  4c 22 94
loc_26_93FD:
    RTS                                 ; 93FD  60
loc_26_93FE:
    JMP RidePlatform                    ; 93FE  4c 3f 94
loc_26_9401:
    STA jmp_trampoline                  ; 9401  85 10
    LDA player_level                    ; 9403  ad 21 04
    CMP #$11                            ; 9406  c9 11
    BEQ loc_26_9422                     ; 9408  f0 18
    LDA player_hp_max,X                 ; 940A  bd c0 03
    SEC                                 ; 940D  38
    SBC jmp_trampoline                  ; 940E  e5 10
    STA player_hp_max,X                 ; 9410  9d c0 03
    BCS loc_26_941A                     ; 9413  b0 05
    LDA #$00                            ; 9415  a9 00
    STA player_hp_max,X                 ; 9417  9d c0 03
loc_26_941A:
    LDA #$5F                            ; 941A  a9 5f
    JSR PlaySound                       ; 941C  20 25 c1
    JSR PlayerInvuln                    ; 941F  20 31 94
loc_26_9422:
    LDA obj_spawn_b2,Y                  ; 9422  b9 40 05
    CMP #$FF                            ; 9425  c9 ff
    BNE loc_26_942E                     ; 9427  d0 05
    LDA #$00                            ; 9429  a9 00
    STA obj_active,Y                    ; 942B  99 a0 04
loc_26_942E:
    JMP HudHpBar                        ; 942E  4c c0 8c
PlayerInvuln:  ; s46: invuln_timer $071A=$1E; obj_flags_s01 b2
    LDA #$1E                            ; 9431  a9 1e
    STA invuln_timer                    ; 9433  8d 1a 07
    LDA obj_flags_s01                   ; 9436  ad 81 03
    ORA #$04                            ; 9439  09 04
    STA obj_flags_s01                   ; 943B  8d 81 03
    RTS                                 ; 943E  60
RidePlatform:  ; s46: platform AI $38/$3C: $0541=slot; move slot 0 one step in its dir at speed 2
    LDA obj_timer2                      ; 943F  ad 20 06
    BNE loc_26_946C                     ; 9442  d0 28
    STY ride_platform_slot              ; 9444  8c 41 05
    LDA obj_freeze,Y                    ; 9447  b9 a0 05
    BNE loc_26_946C                     ; 944A  d0 20
    LDX #$00                            ; 944C  a2 00
    LDA obj_meta_dir,X                  ; 944E  bd 60 03
    PHA                                 ; 9451  48
    LDA obj_flags2,X                    ; 9452  bd 40 03
    PHA                                 ; 9455  48
    LDA #$02                            ; 9456  a9 02
    STA obj_flags2,X                    ; 9458  9d 40 03
    LDA obj_meta_dir,Y                  ; 945B  b9 60 03
    STA obj_meta_dir,X                  ; 945E  9d 60 03
    JSR ObjMoveSlide                    ; 9461  20 d7 98
    PLA                                 ; 9464  68
    STA obj_flags2,X                    ; 9465  9d 40 03
    PLA                                 ; 9468  68
    STA obj_meta_dir,X                  ; 9469  9d 60 03
loc_26_946C:
    RTS                                 ; 946C  60
PickupGold:  ; gold += 26:8BDE[($0500>>4)*2]; saturate $FFFF; SFX $24
    LDA obj_elem_drop,Y                 ; 946D  b9 00 05
    LSR A                               ; 9470  4a
    LSR A                               ; 9471  4a
    LSR A                               ; 9472  4a
    LSR A                               ; 9473  4a
    ASL A                               ; 9474  0a
    TAX                                 ; 9475  aa
    LDA $8BDE,X                         ; 9476  bd de 8b
    CLC                                 ; 9479  18
    ADC gold                            ; 947A  6d 02 07
    STA gold                            ; 947D  8d 02 07
    LDA $8BDF,X                         ; 9480  bd df 8b
    ADC gold_hi                         ; 9483  6d 03 07
    STA gold_hi                         ; 9486  8d 03 07
    BCC loc_26_9493                     ; 9489  90 08
    LDA #$FF                            ; 948B  a9 ff
    STA gold                            ; 948D  8d 02 07
    STA gold_hi                         ; 9490  8d 03 07
loc_26_9493:
    LDA #$00                            ; 9493  a9 00
    STA obj_active,Y                    ; 9495  99 a0 04
    LDA #$24                            ; 9498  a9 24
    JSR PlaySound                       ; 949A  20 25 c1
    LDA #$01                            ; 949D  a9 01
    JMP HudRedraw                       ; 949F  4c 46 8e
loc_26_94A2:
    LDA sub_state                       ; 94A2  a5 41
    CMP #$08                            ; 94A4  c9 08
    BNE loc_26_94AF                     ; 94A6  d0 07
    STY obj_timer2_s03                  ; 94A8  8c 23 06
    LDA #$07                            ; 94AB  a9 07
    STA sub_state                       ; 94AD  85 41
loc_26_94AF:
    RTS                                 ; 94AF  60
sub_26_94B0:
    STY obj_act_timer_s03               ; 94B0  8c 43 06
    LDA obj_active,Y                    ; 94B3  b9 a0 04
    AND #$7F                            ; 94B6  29 7f
    CMP #$78                            ; 94B8  c9 78
    BEQ loc_26_94CB                     ; 94BA  f0 0f
    CMP #$6E                            ; 94BC  c9 6e
    BEQ loc_26_94CB                     ; 94BE  f0 0b
    TYA                                 ; 94C0  98
    TAX                                 ; 94C1  aa
    LDA obj_active,X                    ; 94C2  bd a0 04
    ASL A                               ; 94C5  0a
    CMP #$60                            ; 94C6  c9 60
    BEQ loc_26_94CE                     ; 94C8  f0 04
    RTS                                 ; 94CA  60
loc_26_94CB:
    JMP loc_26_94A2                     ; 94CB  4c a2 94
loc_26_94CE:
    LDA pad_dir                         ; 94CE  a5 49
    BMI loc_26_9534                     ; 94D0  30 62
    LDA obj_behav,X                     ; 94D2  bd e0 06
    AND #$04                            ; 94D5  29 04
    BEQ loc_26_94DF                     ; 94D7  f0 06
    JSR sub_26_9535                     ; 94D9  20 35 95
    JMP loc_26_9508                     ; 94DC  4c 08 95
loc_26_94DF:
    LDA obj_timer2                      ; 94DF  ad 20 06
    BNE loc_26_9534                     ; 94E2  d0 50
    LDA obj_phase_s03                   ; 94E4  ad 03 06
    BEQ loc_26_94EE                     ; 94E7  f0 05
    CPX obj_timer2_s03                  ; 94E9  ec 23 06
    BEQ loc_26_9534                     ; 94EC  f0 46
loc_26_94EE:
    LDA obj_coll,X                      ; 94EE  bd a0 03
    PHA                                 ; 94F1  48
    ORA #$10                            ; 94F2  09 10
    STA obj_coll,X                      ; 94F4  9d a0 03
    LDA #$00                            ; 94F7  a9 00
    STA jmp_trampoline                  ; 94F9  85 10
    LDA #$02                            ; 94FB  a9 02
    STA tmp_11                          ; 94FD  85 11
    JSR ObjOverlapScan                  ; 94FF  20 f8 95
    PLA                                 ; 9502  68
    STA obj_coll,X                      ; 9503  9d a0 03
    BCS loc_26_9534                     ; 9506  b0 2c
loc_26_9508:
    LDA #$10                            ; 9508  a9 10
    STA obj_phase_s03                   ; 950A  8d 03 06
    STX obj_timer2_s03                  ; 950D  8e 23 06
    LDA #$11                            ; 9510  a9 11
    STA sub_state                       ; 9512  85 41
    LDA obj_freeze,X                    ; 9514  bd a0 05
    BNE loc_26_9534                     ; 9517  d0 1b
    LDA obj_behav,X                     ; 9519  bd e0 06
    AND #$02                            ; 951C  29 02
    BEQ loc_26_9534                     ; 951E  f0 14
    LDA obj_meta_id_s01                 ; 9520  ad 01 03
    CLC                                 ; 9523  18
    ADC #$02                            ; 9524  69 02
    AND #$03                            ; 9526  29 03
    STA jmp_trampoline                  ; 9528  85 10
    LDA obj_meta_id,X                   ; 952A  bd 00 03
    AND #$FC                            ; 952D  29 fc
    ORA jmp_trampoline                  ; 952F  05 10
    STA obj_meta_id,X                   ; 9531  9d 00 03
loc_26_9534:
    RTS                                 ; 9534  60
sub_26_9535:
    LDA #$04                            ; 9535  a9 04
    STA tmp_28                          ; 9537  85 28
loc_26_9539:
    LDA obj_coll,X                      ; 9539  bd a0 03
    PHA                                 ; 953C  48
    ORA #$F0                            ; 953D  09 f0
    STA obj_coll,X                      ; 953F  9d a0 03
    TXA                                 ; 9542  8a
    PHA                                 ; 9543  48
    DEX                                 ; 9544  ca
    STX jmp_trampoline                  ; 9545  86 10
    INX                                 ; 9547  e8
    INX                                 ; 9548  e8
    STX tmp_11                          ; 9549  86 11
    LDX #$01                            ; 954B  a2 01
    JSR ObjOverlapScan                  ; 954D  20 f8 95
    PLA                                 ; 9550  68
    TAX                                 ; 9551  aa
    PLA                                 ; 9552  68
    STA obj_coll,X                      ; 9553  9d a0 03
    BCS loc_26_9568                     ; 9556  b0 10
    LDA #$00                            ; 9558  a9 00
    STA obj_phase_s03                   ; 955A  8d 03 06
    LDA obj_timer2                      ; 955D  ad 20 06
    BEQ loc_26_95B6                     ; 9560  f0 54
    LDA #$FF                            ; 9562  a9 ff
    STA obj_act_timer                   ; 9564  8d 40 06
    RTS                                 ; 9567  60
loc_26_9568:
    LDA obj_spawn_b2,Y                  ; 9568  b9 40 05
    CMP #$01                            ; 956B  c9 01
    BNE loc_26_9585                     ; 956D  d0 16
    LDA obj_spawn_param,Y               ; 956F  b9 60 05
    CMP #$3A                            ; 9572  c9 3a
    BNE loc_26_9585                     ; 9574  d0 0f
    LDA obj_meta_id_s01                 ; 9576  ad 01 03
    AND #$03                            ; 9579  29 03
    BNE loc_26_9585                     ; 957B  d0 08
    LDA obj_meta_id_s01                 ; 957D  ad 01 03
    AND #$FC                            ; 9580  29 fc
    STA obj_meta_id_s01                 ; 9582  8d 01 03
loc_26_9585:
    TXA                                 ; 9585  8a
    PHA                                 ; 9586  48
    LDX #$03                            ; 9587  a2 03
loc_26_9589:
    LDA obj_flags2,X                    ; 9589  bd 40 03
    PHA                                 ; 958C  48
    LDA obj_meta_dir,X                  ; 958D  bd 60 03
    PHA                                 ; 9590  48
    LDA obj_meta_id_s01                 ; 9591  ad 01 03
    AND #$03                            ; 9594  29 03
    ASL A                               ; 9596  0a
    CLC                                 ; 9597  18
    ADC #$04                            ; 9598  69 04
    AND #$06                            ; 959A  29 06
    STA obj_meta_dir,X                  ; 959C  9d 60 03
    JSR ObjMoveSlide                    ; 959F  20 d7 98
    PLA                                 ; 95A2  68
    STA obj_meta_dir,X                  ; 95A3  9d 60 03
    PLA                                 ; 95A6  68
    STA obj_flags2,X                    ; 95A7  9d 40 03
    DEX                                 ; 95AA  ca
    BPL loc_26_9589                     ; 95AB  10 dc
    JSR FarCall_B14_800E                ; 95AD  20 8c ff
    PLA                                 ; 95B0  68
    TAX                                 ; 95B1  aa
    DEC tmp_28                          ; 95B2  c6 28
    BNE loc_26_9539                     ; 95B4  d0 83
loc_26_95B6:
    RTS                                 ; 95B6  60
    .db $01,$01,$00,$00,$00,$00,$00,$01,$60; 26:95B7
HitKnockbackSet:  ; s30: target obj_flags2 |= $80 (knockback counter) and attacker dir -> target obj_meta_dir hi nibble
    STX tmp_12                          ; 95C0  86 12
    STY tmp_13                          ; 95C2  84 13
    LDX jmp_trampoline                  ; 95C4  a6 10
    LDY tmp_11                          ; 95C6  a4 11
    LDA obj_flags2,Y                    ; 95C8  b9 40 03
    BMI loc_26_95D4                     ; 95CB  30 07
    AND #$0F                            ; 95CD  29 0f
    ORA #$80                            ; 95CF  09 80
    STA obj_flags2,Y                    ; 95D1  99 40 03
loc_26_95D4:
    LDA obj_flags2,X                    ; 95D4  bd 40 03
    CMP #$0B                            ; 95D7  c9 0b
    LDA obj_meta_dir,X                  ; 95D9  bd 60 03
    BCC loc_26_95DF                     ; 95DC  90 01
    LSR A                               ; 95DE  4a
loc_26_95DF:
    ASL A                               ; 95DF  0a
    ASL A                               ; 95E0  0a
    ASL A                               ; 95E1  0a
    ASL A                               ; 95E2  0a
    STA jmp_trampoline                  ; 95E3  85 10
    LDA obj_meta_dir,Y                  ; 95E5  b9 60 03
    AND #$0F                            ; 95E8  29 0f
    ORA jmp_trampoline                  ; 95EA  05 10
    STA obj_meta_dir,Y                  ; 95EC  99 60 03
    LDX tmp_12                          ; 95EF  a6 12
    LDY tmp_13                          ; 95F1  a4 13
    RTS                                 ; 95F3  60
loc_26_95F4:
    LDX tmp_1B                          ; 95F4  a6 1b
    CLC                                 ; 95F6  18
    RTS                                 ; 95F7  60
ObjOverlapScan:  ; s30: X=attacker; scan Y=$10+1..$11-1; skip obj_flags/obj_flags2 b7; group mask obj_coll&$F0; AABB from HitboxTable + $05C0/$05E0; C=1 hit (Y=target)
    STX tmp_1B                          ; 95F8  86 1b
    LDA obj_flags,X                     ; 95FA  bd 80 03
    BMI loc_26_95F4                     ; 95FD  30 f5
    LDA obj_flags2,X                    ; 95FF  bd 40 03
    BMI loc_26_95F4                     ; 9602  30 f0
    LDA obj_coll,X                      ; 9604  bd a0 03
    AND #$F0                            ; 9607  29 f0
    BEQ loc_26_95F4                     ; 9609  f0 e9
    STA tmp_1F                          ; 960B  85 1f
    LDA obj_coll,X                      ; 960D  bd a0 03
    AND #$0F                            ; 9610  29 0f
    ASL A                               ; 9612  0a
    ASL A                               ; 9613  0a
    STA tmp_15                          ; 9614  85 15
    LDA obj_hitflags,X                  ; 9616  bd 20 04
    AND #$40                            ; 9619  29 40
    ORA tmp_15                          ; 961B  05 15
    TAY                                 ; 961D  a8
    LDA obj_screen_y,X                  ; 961E  bd e0 05
    CLC                                 ; 9621  18
    ADC $9693,Y                         ; 9622  79 93 96
    STA tmp_15                          ; 9625  85 15
    CLC                                 ; 9627  18
    ADC $9694,Y                         ; 9628  79 94 96
    STA tmp_16                          ; 962B  85 16
    LDA obj_screen_x,X                  ; 962D  bd c0 05
    CLC                                 ; 9630  18
    ADC HitboxTable,Y                   ; 9631  79 91 96
    STA tmp_13                          ; 9634  85 13
    CLC                                 ; 9636  18
    ADC $9692,Y                         ; 9637  79 92 96
    STA tmp_14                          ; 963A  85 14
    LDY jmp_trampoline                  ; 963C  a4 10
loc_26_963E:
    INY                                 ; 963E  c8
    CPY tmp_11                          ; 963F  c4 11
    BEQ loc_26_95F4                     ; 9641  f0 b1
    LDA obj_flags,Y                     ; 9643  b9 80 03
    BMI loc_26_963E                     ; 9646  30 f6
    LDA obj_coll,Y                      ; 9648  b9 a0 03
    AND tmp_1F                          ; 964B  25 1f
    BEQ loc_26_963E                     ; 964D  f0 ef
    LDA obj_flags2,Y                    ; 964F  b9 40 03
    BMI loc_26_963E                     ; 9652  30 ea
    CPY tmp_1B                          ; 9654  c4 1b
    BEQ loc_26_963E                     ; 9656  f0 e6
    LDA obj_coll,Y                      ; 9658  b9 a0 03
    AND #$0F                            ; 965B  29 0f
    ASL A                               ; 965D  0a
    ASL A                               ; 965E  0a
    STA jmp_trampoline                  ; 965F  85 10
    LDA obj_hitflags,Y                  ; 9661  b9 20 04
    AND #$40                            ; 9664  29 40
    ORA jmp_trampoline                  ; 9666  05 10
    TAX                                 ; 9668  aa
    LDA obj_screen_x,Y                  ; 9669  b9 c0 05
    CLC                                 ; 966C  18
    ADC HitboxTable,X                   ; 966D  7d 91 96
    CMP tmp_14                          ; 9670  c5 14
    BCS loc_26_963E                     ; 9672  b0 ca
    ADC $9692,X                         ; 9674  7d 92 96
    CMP tmp_13                          ; 9677  c5 13
    BCC loc_26_963E                     ; 9679  90 c3
    LDA obj_screen_y,Y                  ; 967B  b9 e0 05
    CLC                                 ; 967E  18
    ADC $9693,X                         ; 967F  7d 93 96
    CMP tmp_16                          ; 9682  c5 16
    BCS loc_26_963E                     ; 9684  b0 b8
    ADC $9694,X                         ; 9686  7d 94 96
    CMP tmp_15                          ; 9689  c5 15
    BCC loc_26_963E                     ; 968B  90 b1
    LDX tmp_1B                          ; 968D  a6 1b
    SEC                                 ; 968F  38
    RTS                                 ; 9690  60
HitboxTable:  ; s30: 24 x {dx w dy h} signed; index (obj_coll&$0F)*4 | (obj_hitflags&$40)
    .db $FA,$0C,$EB,$14,$FA,$0C,$F3,$0B,$FA,$0C,$F5,$06,$F6,$14,$EC,$14; 26:9691
    .db $F6,$14,$F3,$0B,$F3,$1A,$E3,$12,$FA,$0C,$F5,$03,$FE,$04,$F0,$10; 26:96A1
    .db $F8,$20,$E0,$30,$F8,$20,$F0,$20,$EC,$20,$E2,$10,$EA,$2C,$E2,$2C; 26:96B1
    .db $FB,$0A,$F3,$0A,$FD,$06,$F5,$06,$F9,$0E,$F1,$0E,$F2,$1C,$EA,$1C; 26:96C1
    .db $F6,$14,$E0,$20,$F0,$20,$E0,$30,$00,$28,$E8,$28,$D8,$28,$E8,$28; 26:96D1
    .db $01,$06,$F5,$06,$00,$00,$00,$00,$FD,$06,$E2,$3C,$E2,$3C,$FD,$06; 26:96E1
CollPairTable:  ; s30: 15 x {slot first last handler}: attacker slot vs targets first+1..last-1 ($0D-$1E); handler -> HitHandlerTable
    .db $04,$0C,$1F,$00,$05,$0C,$1F,$00,$06,$0C,$1F,$00,$07,$0C,$1F,$00; 26:96F1
    .db $08,$0C,$1F,$00,$0C,$0C,$1F,$06,$01,$0C,$1F,$02,$03,$0C,$1F,$04; 26:9701
    .db $09,$0C,$1F,$00,$0A,$0C,$1F,$00,$0B,$0C,$1F,$00,$0C,$0C,$1F,$06; 26:9711
    .db $02,$0C,$1F,$00,$01,$0C,$1F,$02,$03,$0C,$1F,$04; 26:9721
SpawnShot:  ; s30: A=shot id; Y=dir; record ShotTable 20:BC00[A*4] (ids >=$40 at $BD00) with bank 20 at $A000 via SetR7($14); C=0 = none spawned
    STY tmp_14                          ; 972D  84 14
    ASL A                               ; 972F  0a
    ASL A                               ; 9730  0a
    TAY                                 ; 9731  a8
    LDA r7_shadow                       ; 9732  a5 6f
    PHA                                 ; 9734  48
    LDA #$14                            ; 9735  a9 14
    JSR SetR7                           ; 9737  20 27 c4
    LDA #$00                            ; 973A  a9 00
    STA tmp_23                          ; 973C  85 23
    LDA ShotTable,Y                     ; 973E  b9 00 bc
    STA tmp_21                          ; 9741  85 21
    LDA $BC01,Y                         ; 9743  b9 01 bc
    STA tmp_22                          ; 9746  85 22
    LDA $BC02,Y                         ; 9748  b9 02 bc
    STA tmp_11                          ; 974B  85 11
    LDA $BC03,Y                         ; 974D  b9 03 bc
    STA tmp_20                          ; 9750  85 20
    BCC loc_26_9768                     ; 9752  90 14
    LDA $BD00,Y                         ; 9754  b9 00 bd
    STA tmp_21                          ; 9757  85 21
    LDA $BD01,Y                         ; 9759  b9 01 bd
    STA tmp_22                          ; 975C  85 22
    LDA $BD02,Y                         ; 975E  b9 02 bd
    STA tmp_11                          ; 9761  85 11
    LDA $BD03,Y                         ; 9763  b9 03 bd
    STA tmp_20                          ; 9766  85 20
loc_26_9768:
    LDA tmp_21                          ; 9768  a5 21
    ASL A                               ; 976A  0a
    BCC loc_26_9773                     ; 976B  90 06
    PLA                                 ; 976D  68
    JSR SetR7                           ; 976E  20 27 c4
    CLC                                 ; 9771  18
    RTS                                 ; 9772  60
loc_26_9773:
    LDY tmp_21                          ; 9773  a4 21
    LDA tmp_22                          ; 9775  a5 22
    STA jmp_trampoline                  ; 9777  85 10
    JSR ObjFindFree                     ; 9779  20 b9 98
    BCC loc_26_97D0                     ; 977C  90 52
    STY jmp_trampoline                  ; 977E  84 10
    JSR FarCall_C25D                    ; 9780  20 80 ff
    TXA                                 ; 9783  8a
    PHA                                 ; 9784  48
    JSR ObjPosToTemp                    ; 9785  20 a8 98
    LDA obj_flags,X                     ; 9788  bd 80 03
    STA tmp_12                          ; 978B  85 12
    AND #$80                            ; 978D  29 80
    LDX jmp_trampoline                  ; 978F  a6 10
    ORA obj_flags,X                     ; 9791  1d 80 03
    STA obj_flags,X                     ; 9794  9d 80 03
    BIT tmp_12                          ; 9797  24 12
    LDA #$00                            ; 9799  a9 00
    BVC loc_26_979F                     ; 979B  50 02
    LDA #$02                            ; 979D  a9 02
loc_26_979F:
    STA tmp_31                          ; 979F  85 31
    LDA tmp_14                          ; 97A1  a5 14
    STA obj_meta_dir,X                  ; 97A3  9d 60 03
    ASL A                               ; 97A6  0a
    AND #$0F                            ; 97A7  29 0f
    STA tmp_13                          ; 97A9  85 13
    LDA obj_behav,X                     ; 97AB  bd e0 06
    ASL A                               ; 97AE  0a
    ASL A                               ; 97AF  0a
    ASL A                               ; 97B0  0a
    ASL A                               ; 97B1  0a
    ORA tmp_13                          ; 97B2  05 13
    TAY                                 ; 97B4  a8
    LDA ShotOffsets,Y                   ; 97B5  b9 80 bd
    STA tmp_30                          ; 97B8  85 30
    LDA $BD81,Y                         ; 97BA  b9 81 bd
    CLC                                 ; 97BD  18
    ADC tmp_31                          ; 97BE  65 31
    STA tmp_31                          ; 97C0  85 31
    JSR TempAddOffset                   ; 97C2  20 24 98
    JSR TempToObjPos                    ; 97C5  20 97 98
    DEC tmp_23                          ; 97C8  c6 23
    PLA                                 ; 97CA  68
    TAX                                 ; 97CB  aa
    DEC tmp_20                          ; 97CC  c6 20
    BNE loc_26_9773                     ; 97CE  d0 a3
loc_26_97D0:
    PLA                                 ; 97D0  68
    JSR SetR7                           ; 97D1  20 27 c4
    ASL tmp_23                          ; 97D4  06 23
    RTS                                 ; 97D6  60
Rand64:  ; A = 26:97E4[++$0E & $3F]
    INC tmp_0E                          ; 97D7  e6 0e
    LDA tmp_0E                          ; 97D9  a5 0e
    AND #$3F                            ; 97DB  29 3f
    STA tmp_0E                          ; 97DD  85 0e
    TAY                                 ; 97DF  a8
    LDA Rand64Table,Y                   ; 97E0  b9 e4 97
    RTS                                 ; 97E3  60
Rand64Table:  ; s49: u8[64] read by Rand64 26:97D7 as [++$0E & $3F]
    .db $06,$02,$02,$01,$05,$03,$04,$07,$05,$07,$00,$05,$03,$04,$06,$06; 26:97E4
    .db $01,$07,$05,$05,$07,$07,$01,$06,$00,$06,$03,$03,$07,$03,$00,$01; 26:97F4
    .db $04,$04,$03,$03,$00,$06,$04,$01,$04,$02,$03,$01,$02,$07,$00,$02; 26:9804
    .db $04,$02,$02,$04,$02,$05,$06,$01,$07,$05,$05,$00,$00,$00,$01,$06; 26:9814
TempAddOffset:  ; s55: add signed $30/$31 to $34-$37; y wraps at 240 testing the ADC carry first (SpawnShot offsets); TempAddStep 9861 ignores that carry (web: obj/util.js)
    LDA tmp_30                          ; 9824  a5 30
    BMI loc_26_9833                     ; 9826  30 0b
    CLC                                 ; 9828  18
    ADC pos_x_lo                        ; 9829  65 34
    STA pos_x_lo                        ; 982B  85 34
    BCC loc_26_983C                     ; 982D  90 0d
    INC pos_x_hi                        ; 982F  e6 35
    BCS loc_26_983C                     ; 9831  b0 09
loc_26_9833:
    CLC                                 ; 9833  18
    ADC pos_x_lo                        ; 9834  65 34
    STA pos_x_lo                        ; 9836  85 34
    BCS loc_26_983C                     ; 9838  b0 02
    DEC pos_x_hi                        ; 983A  c6 35
loc_26_983C:
    LDA tmp_31                          ; 983C  a5 31
    BMI loc_26_9850                     ; 983E  30 10
    CLC                                 ; 9840  18
    ADC pos_y_lo                        ; 9841  65 36
    BCS loc_26_9849                     ; 9843  b0 04
    CMP #$F0                            ; 9845  c9 f0
    BCC loc_26_984D                     ; 9847  90 04
loc_26_9849:
    INC pos_y_hi                        ; 9849  e6 37
    ADC #$0F                            ; 984B  69 0f
loc_26_984D:
    STA pos_y_lo                        ; 984D  85 36
    RTS                                 ; 984F  60
loc_26_9850:
    CLC                                 ; 9850  18
    ADC pos_y_lo                        ; 9851  65 36
    BCC loc_26_985A                     ; 9853  90 05
    CMP #$F0                            ; 9855  c9 f0
    BCC loc_26_985E                     ; 9857  90 05
    CLC                                 ; 9859  18
loc_26_985A:
    ADC #$F0                            ; 985A  69 f0
    DEC pos_y_hi                        ; 985C  c6 37
loc_26_985E:
    STA pos_y_lo                        ; 985E  85 36
    RTS                                 ; 9860  60
TempAddStep:  ; add signed $30/$31 to $34-$37 with 240-line y wrap
    LDA tmp_30                          ; 9861  a5 30
    BMI loc_26_9870                     ; 9863  30 0b
    CLC                                 ; 9865  18
    ADC pos_x_lo                        ; 9866  65 34
    STA pos_x_lo                        ; 9868  85 34
    BCC loc_26_9879                     ; 986A  90 0d
    INC pos_x_hi                        ; 986C  e6 35
    BCS loc_26_9879                     ; 986E  b0 09
loc_26_9870:
    CLC                                 ; 9870  18
    ADC pos_x_lo                        ; 9871  65 34
    STA pos_x_lo                        ; 9873  85 34
    BCS loc_26_9879                     ; 9875  b0 02
    DEC pos_x_hi                        ; 9877  c6 35
loc_26_9879:
    LDA tmp_31                          ; 9879  a5 31
    CLC                                 ; 987B  18
    ADC pos_y_lo                        ; 987C  65 36
    CMP #$F0                            ; 987E  c9 f0
    BCS loc_26_9885                     ; 9880  b0 03
    STA pos_y_lo                        ; 9882  85 36
    RTS                                 ; 9884  60
loc_26_9885:
    BIT tmp_31                          ; 9885  24 31
    BMI loc_26_9890                     ; 9887  30 07
    ADC #$0F                            ; 9889  69 0f
    STA pos_y_lo                        ; 988B  85 36
    INC pos_y_hi                        ; 988D  e6 37
    RTS                                 ; 988F  60
loc_26_9890:
    ADC #$EF                            ; 9890  69 ef
    STA pos_y_lo                        ; 9892  85 36
    DEC pos_y_hi                        ; 9894  c6 37
    RTS                                 ; 9896  60
TempToObjPos:  ; $34-$37 -> slot X x/y
    LDA pos_x_lo                        ; 9897  a5 34
    STA obj_x_lo,X                      ; 9899  95 70
    LDA pos_x_hi                        ; 989B  a5 35
    STA obj_x_hi,X                      ; 989D  95 90
    LDA pos_y_lo                        ; 989F  a5 36
    STA obj_y_lo,X                      ; 98A1  95 b0
    LDA pos_y_hi                        ; 98A3  a5 37
    STA obj_y_hi,X                      ; 98A5  95 d0
    RTS                                 ; 98A7  60
ObjPosToTemp:  ; slot X x/y -> $34-$37
    LDA obj_x_lo,X                      ; 98A8  b5 70
    STA pos_x_lo                        ; 98AA  85 34
    LDA obj_x_hi,X                      ; 98AC  b5 90
    STA pos_x_hi                        ; 98AE  85 35
    LDA obj_y_lo,X                      ; 98B0  b5 b0
    STA pos_y_lo                        ; 98B2  85 36
    LDA obj_y_hi,X                      ; 98B4  b5 d0
    STA pos_y_hi                        ; 98B6  85 37
    RTS                                 ; 98B8  60
ObjFindFree:  ; s47: Y=first slot; $10=end slot (excl.); C=1 + Y=free slot (obj_active=0)
    LDA obj_active,Y                    ; 98B9  b9 a0 04
    BEQ loc_26_98C5                     ; 98BC  f0 07
    INY                                 ; 98BE  c8
    CPY jmp_trampoline                  ; 98BF  c4 10
    BNE ObjFindFree                     ; 98C1  d0 f6
    CLC                                 ; 98C3  18
    RTS                                 ; 98C4  60
loc_26_98C5:
    SEC                                 ; 98C5  38
    RTS                                 ; 98C6  60
ObjDespawn:  ; obj_active;X = 0
    LDA #$00                            ; 98C7  a9 00
    STA obj_active,X                    ; 98C9  9d a0 04
    RTS                                 ; 98CC  60
ObjBecomeNext:  ; ObjInit(slot X; type $04C0;X) (death-type chain)
    LDA obj_death_type,X                ; 98CD  bd c0 04
    STA tmp_11                          ; 98D0  85 11
    STX jmp_trampoline                  ; 98D2  86 10
    JMP FarCall_C25D                    ; 98D4  4c 80 ff
ObjMoveSlide:  ; move in obj_meta_dir via 26:9947; diagonals also try dir-1/dir+1; C=1 blocked
    LDA #$00                            ; 98D7  a9 00
    STA tmp_25                          ; 98D9  85 25
    LDA obj_meta_dir,X                  ; 98DB  bd 60 03
    STA tmp_23                          ; 98DE  85 23
    JSR ObjMoveStep                     ; 98E0  20 47 99
    LDA obj_meta_dir,X                  ; 98E3  bd 60 03
    LSR A                               ; 98E6  4a
    BCC loc_26_9900                     ; 98E7  90 17
    LDA #$00                            ; 98E9  a9 00
    STA tmp_25                          ; 98EB  85 25
    DEC obj_meta_dir,X                  ; 98ED  de 60 03
    JSR ObjMoveStep                     ; 98F0  20 47 99
    LDA #$00                            ; 98F3  a9 00
    STA tmp_25                          ; 98F5  85 25
    INC obj_meta_dir,X                  ; 98F7  fe 60 03
    INC obj_meta_dir,X                  ; 98FA  fe 60 03
    JSR ObjMoveStep                     ; 98FD  20 47 99
loc_26_9900:
    LDA tmp_23                          ; 9900  a5 23
    STA obj_meta_dir,X                  ; 9902  9d 60 03
    SEC                                 ; 9905  38
    RTS                                 ; 9906  60
BgProbePoints:  ; s46: 8 dirs x 8 B: cumulative (dx dy) probe offsets from the candidate pos; 00 00 end; box (x-4;y-5)..(x+4;y+3); dir 5 quirk dy -6
    .db $FC,$FB,$08,$00,$00,$00,$00,$00,$FC,$FB,$08,$00,$00,$08,$00,$00; 26:9907
    .db $04,$FB,$00,$08,$00,$00,$00,$00,$04,$FB,$00,$08,$F8,$00,$00,$00; 26:9917
    .db $FC,$03,$08,$00,$00,$00,$00,$00,$04,$03,$F8,$00,$00,$FA,$00,$00; 26:9927
    .db $FC,$FB,$00,$08,$00,$00,$00,$00,$FC,$03,$00,$F8,$08,$00,$00,$00; 26:9937
ObjMoveStep:  ; s46: ObjMoveSlide inner step: step vector + BgProbePoints; slot 0 mask $06 + ObjScreenEdge; others mask $27; success PLA PLA + commit; C=0
    LDA obj_x_lo,X                      ; 9947  b5 70
    STA pos_x_lo                        ; 9949  85 34
    LDA obj_x_hi,X                      ; 994B  b5 90
    STA pos_x_hi                        ; 994D  85 35
    LDA obj_y_lo,X                      ; 994F  b5 b0
    STA pos_y_lo                        ; 9951  85 36
    LDA obj_y_hi,X                      ; 9953  b5 d0
    STA pos_y_hi                        ; 9955  85 37
    LDA obj_meta_dir,X                  ; 9957  bd 60 03
    AND #$07                            ; 995A  29 07
    STA obj_meta_dir,X                  ; 995C  9d 60 03
    ASL A                               ; 995F  0a
    ASL A                               ; 9960  0a
    ASL A                               ; 9961  0a
    STA tmp_24                          ; 9962  85 24
    LDA obj_meta_dir,X                  ; 9964  bd 60 03
    JSR ObjStepVector                   ; 9967  20 80 84
    JSR TempAddStep                     ; 996A  20 61 98
    LDA pos_x_lo                        ; 996D  a5 34
    STA tmp_1C                          ; 996F  85 1c
    LDA pos_x_hi                        ; 9971  a5 35
    STA tmp_1D                          ; 9973  85 1d
    LDA pos_y_lo                        ; 9975  a5 36
    STA tmp_1E                          ; 9977  85 1e
    LDA pos_y_hi                        ; 9979  a5 37
    STA tmp_1F                          ; 997B  85 1f
loc_26_997D:
    LDY tmp_24                          ; 997D  a4 24
    LDA BgProbePoints,Y                 ; 997F  b9 07 99
    STA tmp_30                          ; 9982  85 30
    LDA $9908,Y                         ; 9984  b9 08 99
    STA tmp_31                          ; 9987  85 31
    ORA tmp_30                          ; 9989  05 30
    BEQ loc_26_99D4                     ; 998B  f0 47
    INY                                 ; 998D  c8
    INY                                 ; 998E  c8
    STY tmp_24                          ; 998F  84 24
    JSR TempAddStep                     ; 9991  20 61 98
    CPX #$00                            ; 9994  e0 00
    BNE loc_26_99BF                     ; 9996  d0 27
    JSR ObjScreenEdge                   ; 9998  20 ff 99
    BCS loc_26_99BE                     ; 999B  b0 21
    LDA #$80                            ; 999D  a9 80
    JSR ObjBgProbe                      ; 999F  20 30 9a
    LDA tmp_20                          ; 99A2  a5 20
    ORA tmp_25                          ; 99A4  05 25
    STA tmp_25                          ; 99A6  85 25
    LDA obj_flags2,X                    ; 99A8  bd 40 03
    EOR #$08                            ; 99AB  49 08
    BNE loc_26_99C4                     ; 99AD  d0 15
    BIT obj1_flash                      ; 99AF  2c 10 07
    BVC loc_26_99C4                     ; 99B2  50 10
    BCS loc_26_997D                     ; 99B4  b0 c7
    LDA tmp_20                          ; 99B6  a5 20
    BEQ loc_26_99BE                     ; 99B8  f0 04
    AND #$24                            ; 99BA  29 24
    BEQ loc_26_997D                     ; 99BC  f0 bf
loc_26_99BE:
    RTS                                 ; 99BE  60
loc_26_99BF:
    LDA #$80                            ; 99BF  a9 80
    JSR ObjBgProbe                      ; 99C1  20 30 9a
loc_26_99C4:
    LDA #$27                            ; 99C4  a9 27
    CPX #$00                            ; 99C6  e0 00
    BNE loc_26_99CC                     ; 99C8  d0 02
    LDA #$06                            ; 99CA  a9 06
loc_26_99CC:
    AND tmp_20                          ; 99CC  25 20
    AND obj_bg_mask,X                   ; 99CE  3d 60 04
    BEQ loc_26_997D                     ; 99D1  f0 aa
    RTS                                 ; 99D3  60
loc_26_99D4:
    PLA                                 ; 99D4  68
    PLA                                 ; 99D5  68
    LDA tmp_1C                          ; 99D6  a5 1c
    STA obj_x_lo,X                      ; 99D8  95 70
    STA pos_x_lo                        ; 99DA  85 34
    LDA tmp_1D                          ; 99DC  a5 1d
    STA obj_x_hi,X                      ; 99DE  95 90
    STA pos_x_hi                        ; 99E0  85 35
    LDA tmp_1E                          ; 99E2  a5 1e
    STA obj_y_lo,X                      ; 99E4  95 b0
    STA pos_y_lo                        ; 99E6  85 36
    LDA tmp_1F                          ; 99E8  a5 1f
    STA obj_y_hi,X                      ; 99EA  95 d0
    STA pos_y_hi                        ; 99EC  85 37
    LDA #$40                            ; 99EE  a9 40
    JSR ObjBgProbe                      ; 99F0  20 30 9a
    LDY obj_meta_dir,X                  ; 99F3  bc 60 03
    LDA tmp_23                          ; 99F6  a5 23
    STA obj_meta_dir,X                  ; 99F8  9d 60 03
    STY tmp_23                          ; 99FB  84 23
    CLC                                 ; 99FD  18
    RTS                                 ; 99FE  60
ObjScreenEdge:  ; s46: slot 0 only: blocked (attr 6; C=1) if screen x <$0A or >=$FC; or screen y <$18 or >=$D0
    LDA pos_x_lo                        ; 99FF  a5 34
    SEC                                 ; 9A01  38
    SBC scroll_shadow0                  ; 9A02  e5 02
    CMP #$0A                            ; 9A04  c9 0a
    BCC loc_26_9A25                     ; 9A06  90 1d
    CMP #$FC                            ; 9A08  c9 fc
    BCS loc_26_9A25                     ; 9A0A  b0 19
    LDA pos_y_lo                        ; 9A0C  a5 36
    SEC                                 ; 9A0E  38
    SBC cam_y_lo                        ; 9A0F  e5 04
    BCS loc_26_9A15                     ; 9A11  b0 02
    SBC #$0F                            ; 9A13  e9 0f
loc_26_9A15:
    CMP #$18                            ; 9A15  c9 18
    BCC loc_26_9A25                     ; 9A17  90 0c
    CMP #$D0                            ; 9A19  c9 d0
    BCS loc_26_9A25                     ; 9A1B  b0 08
    LDA obj_flags,X                     ; 9A1D  bd 80 03
    AND #$A0                            ; 9A20  29 a0
    STA tmp_20                          ; 9A22  85 20
    RTS                                 ; 9A24  60
loc_26_9A25:
    LDA obj_flags,X                     ; 9A25  bd 80 03
    AND #$A0                            ; 9A28  29 a0
    ORA #$06                            ; 9A2A  09 06
    STA tmp_20                          ; 9A2C  85 20
    SEC                                 ; 9A2E  38
    RTS                                 ; 9A2F  60
ObjBgProbe:  ; metatile collision attr at $34-$37 -> $20 (bank 9 page $69); A b7 = cached if same 16px cell; A b6 = update obj_flags b4/b6 (s46)
    STA tmp_14                          ; 9A30  85 14
    BPL loc_26_9A4D                     ; 9A32  10 19
    LDA pos_y_lo                        ; 9A34  a5 36
    EOR obj_y_lo,X                      ; 9A36  55 b0
    AND #$F0                            ; 9A38  29 f0
    BNE loc_26_9A4D                     ; 9A3A  d0 11
    LDA pos_x_lo                        ; 9A3C  a5 34
    EOR obj_x_lo,X                      ; 9A3E  55 70
    AND #$F0                            ; 9A40  29 f0
    BNE loc_26_9A4D                     ; 9A42  d0 09
    LDA obj_flags,X                     ; 9A44  bd 80 03
    AND #$50                            ; 9A47  29 50
    STA tmp_20                          ; 9A49  85 20
    SEC                                 ; 9A4B  38
    RTS                                 ; 9A4C  60
loc_26_9A4D:
    LDA pos_y_hi                        ; 9A4D  a5 37
    ASL A                               ; 9A4F  0a
    ASL A                               ; 9A50  0a
    ASL A                               ; 9A51  0a
    ORA pos_x_hi                        ; 9A52  05 35
    TAY                                 ; 9A54  a8
    LDA screen_grid,Y                   ; 9A55  b9 00 63
    AND #$1F                            ; 9A58  29 1f
    ORA #$A0                            ; 9A5A  09 a0
    STA tmp_11                          ; 9A5C  85 11
    LDA screen_grid,Y                   ; 9A5E  b9 00 63
    ROL A                               ; 9A61  2a
    ROL A                               ; 9A62  2a
    ROL A                               ; 9A63  2a
    ROL A                               ; 9A64  2a
    AND #$07                            ; 9A65  29 07
    STA r7_shadow                       ; 9A67  85 6f
    LDY #$07                            ; 9A69  a0 07
    STY mmc3_banksel_shadow             ; 9A6B  84 50
    STY DirGrid                         ; 9A6D  8c 00 80
    STA $8001                           ; 9A70  8d 01 80
    LDA pos_y_lo                        ; 9A73  a5 36
    AND #$F0                            ; 9A75  29 f0
    STA tmp_12                          ; 9A77  85 12
    LDA pos_x_lo                        ; 9A79  a5 34
    LSR A                               ; 9A7B  4a
    LSR A                               ; 9A7C  4a
    LSR A                               ; 9A7D  4a
    LSR A                               ; 9A7E  4a
    ORA tmp_12                          ; 9A7F  05 12
    STA jmp_trampoline                  ; 9A81  85 10
    LDY #$00                            ; 9A83  a0 00
    LDA (jmp_trampoline),Y              ; 9A85  b1 10
    STA zp_68                           ; 9A87  85 68
    LDY #$09                            ; 9A89  a0 09
    STY r7_shadow                       ; 9A8B  84 6f
    LDA #$07                            ; 9A8D  a9 07
    STA mmc3_banksel_shadow             ; 9A8F  85 50
    STA DirGrid                         ; 9A91  8d 00 80
    STY $8001                           ; 9A94  8c 01 80
    LDY #$00                            ; 9A97  a0 00
    LDA (zp_68),Y                       ; 9A99  b1 68
    STA tmp_20                          ; 9A9B  85 20
    AND #$08                            ; 9A9D  29 08
    BEQ loc_26_9AB9                     ; 9A9F  f0 18
    LDY pos_y_hi                        ; 9AA1  a4 37
    LDA visited_map,Y                   ; 9AA3  b9 f0 62
    LDY pos_x_hi                        ; 9AA6  a4 35
    AND BitMask_9AE0,Y                  ; 9AA8  39 e0 9a
    BEQ loc_26_9AB9                     ; 9AAB  f0 0c
    LDY zp_68                           ; 9AAD  a4 68
    LDA ($6A),Y                         ; 9AAF  b1 6a
    STA zp_68                           ; 9AB1  85 68
    LDY #$00                            ; 9AB3  a0 00
    LDA (zp_68),Y                       ; 9AB5  b1 68
    STA tmp_20                          ; 9AB7  85 20
loc_26_9AB9:
    LDY #$1B                            ; 9AB9  a0 1b
    STY r7_shadow                       ; 9ABB  84 6f
    LDA #$07                            ; 9ABD  a9 07
    STA mmc3_banksel_shadow             ; 9ABF  85 50
    STA DirGrid                         ; 9AC1  8d 00 80
    STY $8001                           ; 9AC4  8c 01 80
    BIT tmp_14                          ; 9AC7  24 14
    BVC loc_26_9ADE                     ; 9AC9  50 13
    LDA obj_flags,X                     ; 9ACB  bd 80 03
    AND #$AF                            ; 9ACE  29 af
    STA jmp_trampoline                  ; 9AD0  85 10
    LDA tmp_20                          ; 9AD2  a5 20
    AND obj_bg_mask,X                   ; 9AD4  3d 60 04
    AND #$50                            ; 9AD7  29 50
    ORA jmp_trampoline                  ; 9AD9  05 10
    STA obj_flags,X                     ; 9ADB  9d 80 03
loc_26_9ADE:
    CLC                                 ; 9ADE  18
    RTS                                 ; 9ADF  60
BitMask_9AE0:  ; s46: u8[8] 01 02 04 08 10 20 40 80 (visited_map bit by x_hi)
    .db $01,$02,$04,$08,$10,$20,$40,$80 ; 26:9AE0
SpendMp:  ; s44: A=cost; MP=0 or MP<cost -> sub_state $1C (out of MP) and return from the caller
    STA tmp_1F                          ; 9AE8  85 1f
    LDA mp_cur                          ; 9AEA  ad 08 07
    BEQ loc_26_9AF7                     ; 9AED  f0 08
    LDA mp_cur                          ; 9AEF  ad 08 07
    SEC                                 ; 9AF2  38
    SBC tmp_1F                          ; 9AF3  e5 1f
    BCS loc_26_9AFE                     ; 9AF5  b0 07
loc_26_9AF7:
    LDA #$1C                            ; 9AF7  a9 1c
    STA sub_state                       ; 9AF9  85 41
    PLA                                 ; 9AFB  68
    PLA                                 ; 9AFC  68
    RTS                                 ; 9AFD  60
loc_26_9AFE:
    STA mp_cur                          ; 9AFE  8d 08 07
    LDA #$04                            ; 9B01  a9 04
    JSR HudRedraw                       ; 9B03  20 46 8e
    RTS                                 ; 9B06  60
sub_26_9B07:
    STA jmp_trampoline                  ; 9B07  85 10
    LDA obj_flags2,X                    ; 9B09  bd 40 03
    BMI loc_26_9B11                     ; 9B0C  30 03
    JMP PaletteBuild                    ; 9B0E  4c 0e 8c
loc_26_9B11:
    LDA tmp_08                          ; 9B11  a5 08
    LSR A                               ; 9B13  4a
    BCC loc_26_9B40                     ; 9B14  90 2a
    LDY #$00                            ; 9B16  a0 00
loc_26_9B18:
    TYA                                 ; 9B18  98
    AND #$03                            ; 9B19  29 03
    BEQ loc_26_9B34                     ; 9B1B  f0 17
loc_26_9B1D:
    CPY #$10                            ; 9B1D  c0 10
    BCS loc_26_9B28                     ; 9B1F  b0 07
    LDA pal_buf,Y                       ; 9B21  b9 40 61
    CMP #$0F                            ; 9B24  c9 0f
    BEQ loc_26_9B2D                     ; 9B26  f0 05
loc_26_9B28:
    LDA #$30                            ; 9B28  a9 30
    STA pal_buf,Y                       ; 9B2A  99 40 61
loc_26_9B2D:
    INY                                 ; 9B2D  c8
    CPY #$20                            ; 9B2E  c0 20
    BNE loc_26_9B18                     ; 9B30  d0 e6
    BEQ loc_26_9B40                     ; 9B32  f0 0c
loc_26_9B34:
    LSR jmp_trampoline                  ; 9B34  46 10
    BCS loc_26_9B1D                     ; 9B36  b0 e5
    INY                                 ; 9B38  c8
    INY                                 ; 9B39  c8
    INY                                 ; 9B3A  c8
    INY                                 ; 9B3B  c8
    CPY #$20                            ; 9B3C  c0 20
    BNE loc_26_9B18                     ; 9B3E  d0 d8
loc_26_9B40:
    RTS                                 ; 9B40  60
    .db $88,$C2,$8A,$EF                 ; 26:9B41
PlayerUpdate:  ; player AI (type $04 AI $01; slot X=1): B new press + use_item_sel!=0 + sub_state 8 -> sub_state 6 (use item); then 9BF6/9CEF/A1C9 + status anims
    LDA pad_new                         ; 9B45  a5 4b
    AND #$40                            ; 9B47  29 40
    BEQ loc_26_9B5A                     ; 9B49  f0 0f
    LDA use_item_sel                    ; 9B4B  ad 15 07
    BEQ loc_26_9B5A                     ; 9B4E  f0 0a
    LDA sub_state                       ; 9B50  a5 41
    CMP #$08                            ; 9B52  c9 08
    BNE loc_26_9B5A                     ; 9B54  d0 04
    LDA #$06                            ; 9B56  a9 06
    STA sub_state                       ; 9B58  85 41
loc_26_9B5A:
    LDA obj_flags2,X                    ; 9B5A  bd 40 03
    AND #$0F                            ; 9B5D  29 0f
    JSR PlayerSetSpeed                  ; 9B5F  20 f6 9b
    JSR PlayerPlaceBody                 ; 9B62  20 ef 9c
    JSR PlayerFacing                    ; 9B65  20 c9 a1
    LDA obj1_flash                      ; 9B68  ad 10 07
    AND #$0F                            ; 9B6B  29 0f
    CMP #$04                            ; 9B6D  c9 04
    BNE loc_26_9B7D                     ; 9B6F  d0 0c
    LDA #$02                            ; 9B71  a9 02
    JSR PlayerSetSpeed                  ; 9B73  20 f6 9b
    LDA #$53                            ; 9B76  a9 53
    STA obj_meta_id,X                   ; 9B78  9d 00 03
    BNE loc_26_9BD2                     ; 9B7B  d0 55
loc_26_9B7D:
    BIT obj1_flash                      ; 9B7D  2c 10 07
    BVC loc_26_9BD2                     ; 9B80  50 50
    LDA #$09                            ; 9B82  a9 09
    JSR PlayerSetSpeed                  ; 9B84  20 f6 9b
    LDA obj_spawn_param_s01             ; 9B87  ad 61 05
    ORA player_bg_attr                  ; 9B8A  0d 41 06
    AND #$06                            ; 9B8D  29 06
    BNE loc_26_9B9C                     ; 9B8F  d0 0b
    LDA obj1_flash                      ; 9B91  ad 10 07
    AND #$BF                            ; 9B94  29 bf
    STA obj1_flash                      ; 9B96  8d 10 07
    JMP loc_26_9BD2                     ; 9B99  4c d2 9b
loc_26_9B9C:
    LDA obj_meta_id_s01                 ; 9B9C  ad 01 03
    AND #$03                            ; 9B9F  29 03
    TAY                                 ; 9BA1  a8
    LDA $9B41,Y                         ; 9BA2  b9 41 9b
    STA obj_meta_id                     ; 9BA5  8d 00 03
    LDA obj_flags_s01                   ; 9BA8  ad 81 03
    ORA #$40                            ; 9BAB  09 40
    STA obj_flags_s01                   ; 9BAD  8d 81 03
    DEC obj_anim                        ; 9BB0  ce e0 04
    LDA obj_phase                       ; 9BB3  ad 00 06
    BEQ loc_26_9BCC                     ; 9BB6  f0 14
    LDA obj_meta_id_s01                 ; 9BB8  ad 01 03
    AND #$03                            ; 9BBB  29 03
    CMP #$02                            ; 9BBD  c9 02
    BNE loc_26_9BD2                     ; 9BBF  d0 11
    BIT pad_dir                         ; 9BC1  24 49
    BPL loc_26_9BD2                     ; 9BC3  10 0d
    LDA #$96                            ; 9BC5  a9 96
    STA obj_meta_id_s01                 ; 9BC7  8d 01 03
    BNE loc_26_9BD2                     ; 9BCA  d0 06
loc_26_9BCC:
    LDA obj_anim                        ; 9BCC  ad e0 04
    STA obj_anim_s01                    ; 9BCF  8d e1 04
loc_26_9BD2:
    LDA obj1_flash                      ; 9BD2  ad 10 07
    BPL loc_26_9C03                     ; 9BD5  10 2c
    AND #$30                            ; 9BD7  29 30
    LSR A                               ; 9BD9  4a
    LSR A                               ; 9BDA  4a
    LSR A                               ; 9BDB  4a
    LSR A                               ; 9BDC  4a
    TAY                                 ; 9BDD  a8
    LDA $9BF2,Y                         ; 9BDE  b9 f2 9b
    STA jmp_trampoline                  ; 9BE1  85 10
    LDA obj_meta_id,X                   ; 9BE3  bd 00 03
    AND #$03                            ; 9BE6  29 03
    ORA jmp_trampoline                  ; 9BE8  05 10
    STA obj_meta_id,X                   ; 9BEA  9d 00 03
    LDA #$05                            ; 9BED  a9 05
    JMP PlayerSetSpeed                  ; 9BEF  4c f6 9b
    .db $2C,$30,$34,$38                 ; 26:9BF2
PlayerSetSpeed:  ; s44: slot 0 obj_flags2 low nibble = move speed by obj1_flash status (4 -> 2; b6 -> 9; b7 -> 5)
    STA jmp_trampoline                  ; 9BF6  85 10
    LDA obj_flags2                      ; 9BF8  ad 40 03
    AND #$F0                            ; 9BFB  29 f0
    ORA jmp_trampoline                  ; 9BFD  05 10
    STA obj_flags2                      ; 9BFF  8d 40 03
    RTS                                 ; 9C02  60
loc_26_9C03:
    LDA equip_shield                    ; 9C03  ad 14 07
    BNE loc_26_9C1F                     ; 9C06  d0 17
    LDA obj_meta_id,X                   ; 9C08  bd 00 03
    AND #$03                            ; 9C0B  29 03
    ORA #$08                            ; 9C0D  09 08
    TAY                                 ; 9C0F  a8
    LDA obj_meta_id,X                   ; 9C10  bd 00 03
    AND #$04                            ; 9C13  29 04
    BEQ loc_26_9C19                     ; 9C15  f0 02
    LDY #$4C                            ; 9C17  a0 4c
loc_26_9C19:
    TYA                                 ; 9C19  98
    STA obj_overlay_id,X                ; 9C1A  9d 80 05
    BNE loc_26_9C2A                     ; 9C1D  d0 0b
loc_26_9C1F:
    LDA obj_meta_id,X                   ; 9C1F  bd 00 03
    AND #$07                            ; 9C22  29 07
    CLC                                 ; 9C24  18
    ADC #$0C                            ; 9C25  69 0c
    STA obj_overlay_id,X                ; 9C27  9d 80 05
loc_26_9C2A:
    LDA obj_meta_id,X                   ; 9C2A  bd 00 03
    ASL A                               ; 9C2D  0a
    AND #$07                            ; 9C2E  29 07
    TAY                                 ; 9C30  a8
    LDA obj_phase                       ; 9C31  ad 00 06
    CMP #$10                            ; 9C34  c9 10
    BEQ loc_26_9C6C                     ; 9C36  f0 34
    BCS loc_26_9C65                     ; 9C38  b0 2b
    CMP #$04                            ; 9C3A  c9 04
    BMI loc_26_9C65                     ; 9C3C  30 27
    LDA obj_active                      ; 9C3E  ad a0 04
    CMP #$0B                            ; 9C41  c9 0b
    BNE loc_26_9C4C                     ; 9C43  d0 07
    LDA obj_phase                       ; 9C45  ad 00 06
    CMP #$0F                            ; 9C48  c9 0f
    BNE loc_26_9C65                     ; 9C4A  d0 19
loc_26_9C4C:
    LDA #$00                            ; 9C4C  a9 00
    STA $04A2                           ; 9C4E  8d a2 04
    LDA #$19                            ; 9C51  a9 19
    JSR SpawnShot                       ; 9C53  20 2d 97
    LDY equip_sword                     ; 9C56  ac 11 07
    LDA SwordElemBit,Y                  ; 9C59  b9 66 9c
    STA $0502                           ; 9C5C  8d 02 05
    LDA SwordAttackTable,Y              ; 9C5F  b9 d2 8b
    STA $03E2                           ; 9C62  8d e2 03
loc_26_9C65:
    RTS                                 ; 9C65  60
SwordElemBit:  ; s44: u8[6] 00 01 02 04 08 00 by equip_sword -> slash shot $0502
    .db $00,$01,$02,$04,$08,$00         ; 26:9C66
loc_26_9C6C:
    LDA equip_sword                     ; 9C6C  ad 11 07
    BEQ loc_26_9CD1                     ; 9C6F  f0 60
    CMP #$05                            ; 9C71  c9 05
    BEQ loc_26_9CDA                     ; 9C73  f0 65
    LDA orbit_level                     ; 9C75  ad c0 06
    LSR A                               ; 9C78  4a
    LSR A                               ; 9C79  4a
    LSR A                               ; 9C7A  4a
    STA jmp_trampoline                  ; 9C7B  85 10
    BNE loc_26_9C91                     ; 9C7D  d0 12
    LDA obj1_flash                      ; 9C7F  ad 10 07
    AND #$0F                            ; 9C82  29 0f
    CMP #$01                            ; 9C84  c9 01
    BEQ loc_26_9C91                     ; 9C86  f0 09
    LDA equip_accessory                 ; 9C88  ad 16 07
    CMP #$0F                            ; 9C8B  c9 0f
    BNE loc_26_9C91                     ; 9C8D  d0 02
    INC jmp_trampoline                  ; 9C8F  e6 10
loc_26_9C91:
    LDA jmp_trampoline                  ; 9C91  a5 10
    CMP #$03                            ; 9C93  c9 03
    BNE loc_26_9CA7                     ; 9C95  d0 10
    LDY equip_sword                     ; 9C97  ac 11 07
    LDA SwordMpCost,Y                   ; 9C9A  b9 d8 8b
    CMP mp_cur                          ; 9C9D  cd 08 07
    BCC loc_26_9CA7                     ; 9CA0  90 05
    DEC jmp_trampoline                  ; 9CA2  c6 10
    DEC orbit_level                     ; 9CA4  ce c0 06
loc_26_9CA7:
    LDA equip_sword                     ; 9CA7  ad 11 07
    SEC                                 ; 9CAA  38
    SBC #$01                            ; 9CAB  e9 01
    ASL A                               ; 9CAD  0a
    ASL A                               ; 9CAE  0a
    ORA jmp_trampoline                  ; 9CAF  05 10
    PHA                                 ; 9CB1  48
    LDA obj_meta_id,X                   ; 9CB2  bd 00 03
    AND #$03                            ; 9CB5  29 03
    ASL A                               ; 9CB7  0a
    TAY                                 ; 9CB8  a8
    PLA                                 ; 9CB9  68
    JSR SpawnShot                       ; 9CBA  20 2d 97
    BCC loc_26_9CD1                     ; 9CBD  90 12
    LDA orbit_level                     ; 9CBF  ad c0 06
    AND #$F8                            ; 9CC2  29 f8
    CMP #$18                            ; 9CC4  c9 18
    BNE loc_26_9CD1                     ; 9CC6  d0 09
    LDY equip_sword                     ; 9CC8  ac 11 07
    LDA SwordMpCost,Y                   ; 9CCB  b9 d8 8b
    JSR SpendMp                         ; 9CCE  20 e8 9a
loc_26_9CD1:
    LDA #$00                            ; 9CD1  a9 00
    STA orbit_level                     ; 9CD3  8d c0 06
    STA orbit_burst_timer               ; 9CD6  8d c1 06
    RTS                                 ; 9CD9  60
loc_26_9CDA:
    LDA area_id                         ; 9CDA  a5 6c
    CMP #$5F                            ; 9CDC  c9 5f
    BNE loc_26_9CD1                     ; 9CDE  d0 f1
    LDA obj_meta_id,X                   ; 9CE0  bd 00 03
    AND #$03                            ; 9CE3  29 03
    ASL A                               ; 9CE5  0a
    TAY                                 ; 9CE6  a8
    LDA #$13                            ; 9CE7  a9 13
    JSR SpawnShot                       ; 9CE9  20 2d 97
    JMP loc_26_9CD1                     ; 9CEC  4c d1 9c
PlayerPlaceBody:  ; s44: slot 1 = slot 0 pos + JumpHeightTable[$0620]; obj_coll $60 air / $50 ground
    LDY obj_timer2                      ; 9CEF  ac 20 06
    LDA JumpHeightTable,Y               ; 9CF2  b9 4d 9d
    STA tmp_31                          ; 9CF5  85 31
    BEQ loc_26_9D11                     ; 9CF7  f0 18
    LDA #$60                            ; 9CF9  a9 60
    STA obj_coll,X                      ; 9CFB  9d a0 03
    JSR ObjPosToTemp                    ; 9CFE  20 a8 98
    LDA #$40                            ; 9D01  a9 40
    JSR ObjBgProbe                      ; 9D03  20 30 9a
    LDA obj_flags,X                     ; 9D06  bd 80 03
    AND #$BF                            ; 9D09  29 bf
    STA obj_flags,X                     ; 9D0B  9d 80 03
    JMP loc_26_9D27                     ; 9D0E  4c 27 9d
loc_26_9D11:
    LDA #$50                            ; 9D11  a9 50
    STA obj_coll,X                      ; 9D13  9d a0 03
    LDA obj_flags,X                     ; 9D16  bd 80 03
    AND #$07                            ; 9D19  29 07
    STA jmp_trampoline                  ; 9D1B  85 10
    LDA obj_flags                       ; 9D1D  ad 80 03
    AND #$F8                            ; 9D20  29 f8
    ORA jmp_trampoline                  ; 9D22  05 10
    STA obj_flags,X                     ; 9D24  9d 80 03
loc_26_9D27:
    LDA tmp_31                          ; 9D27  a5 31
    CMP #$F9                            ; 9D29  c9 f9
    BNE loc_26_9D3A                     ; 9D2B  d0 0d
    LDA #$56                            ; 9D2D  a9 56
    JSR PlaySound                       ; 9D2F  20 25 c1
    LDA #$10                            ; 9D32  a9 10
    STA boss_cam_lock                   ; 9D34  8d d7 07
    STA ram_07D6                        ; 9D37  8d d6 07
loc_26_9D3A:
    STX jmp_trampoline                  ; 9D3A  86 10
    LDX #$00                            ; 9D3C  a2 00
    JSR ObjPosToTemp                    ; 9D3E  20 a8 98
    LDA #$00                            ; 9D41  a9 00
    STA tmp_30                          ; 9D43  85 30
    LDX jmp_trampoline                  ; 9D45  a6 10
    JSR TempAddOffset                   ; 9D47  20 24 98
    JMP TempToObjPos                    ; 9D4A  4c 97 98
JumpHeightTable:  ; s44: s8[49] slot 1 y offset per $0620 (idx $20 -> 0); verified s45
    .db $00,$00,$00,$FD,$FA,$F8,$F6,$F4,$F3,$F1,$F0,$EE,$ED,$EC,$EC,$EB; 26:9D4D
    .db $EB,$EB,$EB,$EB,$EB,$EC,$EC,$ED,$EE,$F0,$F1,$F3,$F4,$F6,$F8,$FA; 26:9D5D
    .db $FD,$F9,$F0,$E8,$E0,$D8,$D0,$C8,$C0,$B8,$B0,$A8,$A0,$9A,$92,$76; 26:9D6D
    .db $84                             ; 26:9D7D
SwordChargeCap:  ; u8[3] 08 10 18 max orbit_level per $0719 power level
    .db $08,$10,$18                     ; 26:9D7E
PlayerMoveLocked:  ; s44: AI $02 = PlayerMove with pad vars zeroed (cutscenes); area $58 -> AI $03 + pad_dir 4
    LDA #$00                            ; 9D81  a9 00
    STA pad_prev                        ; 9D83  85 43
    STA pad_new                         ; 9D85  85 4b
    STA pad_act                         ; 9D87  85 4d
    STA pad_dir                         ; 9D89  85 49
    LDA area_id                         ; 9D8B  a5 6c
    CMP #$58                            ; 9D8D  c9 58
    BNE PlayerMove                      ; 9D8F  d0 09
    LDA #$03                            ; 9D91  a9 03
    STA obj_active,X                    ; 9D93  9d a0 04
    LDA #$04                            ; 9D96  a9 04
    STA pad_dir                         ; 9D98  85 49
PlayerMove:  ; s44: AI $03 slot 0 (type $06) ground body: pad -> obj_behav; jump timer $0620; PlayerMagic; sword charge; ObjMoveSlide; BG probe; conveyor
    LDA obj1_flash                      ; 9D9A  ad 10 07
    BMI loc_26_9DB7                     ; 9D9D  30 18
    AND #$0F                            ; 9D9F  29 0f
    CMP #$04                            ; 9DA1  c9 04
    BEQ loc_26_9DB7                     ; 9DA3  f0 12
    LDA pad_dir                         ; 9DA5  a5 49
    STA obj_behav,X                     ; 9DA7  9d e0 06
    LDY #$A7                            ; 9DAA  a0 a7
    LDA obj_flags,X                     ; 9DAC  bd 80 03
    ASL A                               ; 9DAF  0a
    BPL loc_26_9DB9                     ; 9DB0  10 07
    LDA obj_timer2,X                    ; 9DB2  bd 20 06
    BNE loc_26_9DB9                     ; 9DB5  d0 02
loc_26_9DB7:
    LDY #$4C                            ; 9DB7  a0 4c
loc_26_9DB9:
    TYA                                 ; 9DB9  98
    STA obj_meta_id,X                   ; 9DBA  9d 00 03
    LDA obj_timer2,X                    ; 9DBD  bd 20 06
    BEQ loc_26_9DE1                     ; 9DC0  f0 1f
    DEC obj_timer2,X                    ; 9DC2  de 20 06
    LDA obj_timer2,X                    ; 9DC5  bd 20 06
    CMP #$11                            ; 9DC8  c9 11
    BNE loc_26_9DD1                     ; 9DCA  d0 05
    LDA #$31                            ; 9DCC  a9 31
    JSR PlaySound                       ; 9DCE  20 25 c1
loc_26_9DD1:
    LDA obj_timer2,X                    ; 9DD1  bd 20 06
    CMP #$1E                            ; 9DD4  c9 1e
    BCC loc_26_9DE1                     ; 9DD6  90 09
    LDA pad_dir                         ; 9DD8  a5 49
    BMI loc_26_9DE1                     ; 9DDA  30 05
    STA obj_act_timer,X                 ; 9DDC  9d 40 06
    BPL loc_26_9DE1                     ; 9DDF  10 00
loc_26_9DE1:
    JSR PlayerMagic                     ; 9DE1  20 22 a0
    LDA obj1_flash                      ; 9DE4  ad 10 07
    BMI loc_26_9E39                     ; 9DE7  30 50
    AND #$0F                            ; 9DE9  29 0f
    CMP #$04                            ; 9DEB  c9 04
    BEQ loc_26_9E39                     ; 9DED  f0 4a
    LDA pad_prev                        ; 9DEF  a5 43
    AND #$40                            ; 9DF1  29 40
    BEQ loc_26_9E39                     ; 9DF3  f0 44
    LDA obj_phase,X                     ; 9DF5  bd 00 06
    CMP #$0C                            ; 9DF8  c9 0c
    BCC loc_26_9E03                     ; 9DFA  90 07
    CMP #$11                            ; 9DFC  c9 11
    BEQ loc_26_9E03                     ; 9DFE  f0 03
    JMP loc_26_9E39                     ; 9E00  4c 39 9e
loc_26_9E03:
    LDA equip_sword                     ; 9E03  ad 11 07
    BEQ loc_26_9E39                     ; 9E06  f0 31
    LDA #$12                            ; 9E08  a9 12
    STA obj_phase,X                     ; 9E0A  9d 00 06
    LDA tmp_08                          ; 9E0D  a5 08
    AND #$03                            ; 9E0F  29 03
    BNE loc_26_9E39                     ; 9E11  d0 26
    INC orbit_level                     ; 9E13  ee c0 06
    LDA orbit_level                     ; 9E16  ad c0 06
    CMP #$08                            ; 9E19  c9 08
    BNE loc_26_9E22                     ; 9E1B  d0 05
    LDY #$0E                            ; 9E1D  a0 0e
    STY orbit_burst_timer               ; 9E1F  8c c1 06
loc_26_9E22:
    AND #$07                            ; 9E22  29 07
    BNE loc_26_9E2B                     ; 9E24  d0 05
    LDA #$26                            ; 9E26  a9 26
    JSR PlaySound                       ; 9E28  20 25 c1
loc_26_9E2B:
    LDY sword_power                     ; 9E2B  ac 19 07
    LDA SwordChargeCap,Y                ; 9E2E  b9 7e 9d
    CMP orbit_level                     ; 9E31  cd c0 06
    BCS loc_26_9E39                     ; 9E34  b0 03
    STA orbit_level                     ; 9E36  8d c0 06
loc_26_9E39:
    LDY obj_phase,X                     ; 9E39  bc 00 06
    BEQ loc_26_9E6F                     ; 9E3C  f0 31
    CPY #$12                            ; 9E3E  c0 12
    BNE loc_26_9E57                     ; 9E40  d0 15
    BIT pad_prev                        ; 9E42  24 43
    BVC loc_26_9E57                     ; 9E44  50 11
    BIT pad_dir                         ; 9E46  24 49
    BPL loc_26_9E6F                     ; 9E48  10 25
    LDA obj_timer2,X                    ; 9E4A  bd 20 06
    BEQ loc_26_9E57                     ; 9E4D  f0 08
    LDA #$00                            ; 9E4F  a9 00
    STA obj_meta_id_s01                 ; 9E51  8d 01 03
    JMP loc_26_9E62                     ; 9E54  4c 62 9e
loc_26_9E57:
    STY obj_anim_s01                    ; 9E57  8c e1 04
    LDA #$04                            ; 9E5A  a9 04
    STA obj_meta_id_s01                 ; 9E5C  8d 01 03
    DEC obj_phase,X                     ; 9E5F  de 00 06
loc_26_9E62:
    LDA obj_timer2,X                    ; 9E62  bd 20 06
    BNE loc_26_9E85                     ; 9E65  d0 1e
    LDA #$32                            ; 9E67  a9 32
    STA obj_bg_mask,X                   ; 9E69  9d 60 04
    JMP loc_26_9E85                     ; 9E6C  4c 85 9e
loc_26_9E6F:
    LDA #$00                            ; 9E6F  a9 00
    STA obj_meta_id_s01                 ; 9E71  8d 01 03
    LDA obj_timer2,X                    ; 9E74  bd 20 06
    BNE loc_26_9E85                     ; 9E77  d0 0c
    LDA #$32                            ; 9E79  a9 32
    STA obj_bg_mask,X                   ; 9E7B  9d 60 04
    BIT pad_dir                         ; 9E7E  24 49
    BMI loc_26_9E85                     ; 9E80  30 03
    DEC obj_anim_s01                    ; 9E82  ce e1 04
loc_26_9E85:
    BIT obj1_flash                      ; 9E85  2c 10 07
    BVC loc_26_9E8F                     ; 9E88  50 05
    LDA #$34                            ; 9E8A  a9 34
    STA obj_bg_mask                     ; 9E8C  8d 60 04
loc_26_9E8F:
    LDY obj_timer2,X                    ; 9E8F  bc 20 06
    BEQ loc_26_9E9E                     ; 9E92  f0 0a
    LDA obj_act_timer,X                 ; 9E94  bd 40 06
    STA pad_dir                         ; 9E97  85 49
    LDA JumpHeightTable,Y               ; 9E99  b9 4d 9d
    BEQ loc_26_9EEF                     ; 9E9C  f0 51
loc_26_9E9E:
    LDA obj_meta_dir,X                  ; 9E9E  bd 60 03
    STA tmp_26                          ; 9EA1  85 26
    LDA pad_dir                         ; 9EA3  a5 49
    BMI loc_26_9EEF                     ; 9EA5  30 48
    LDA obj_param1,X                    ; 9EA7  bd 80 06
    BPL loc_26_9EBA                     ; 9EAA  10 0e
    LDA #$00                            ; 9EAC  a9 00
    STA obj_anim_s01                    ; 9EAE  8d e1 04
    LDY pad_dir                         ; 9EB1  a4 49
    LDA DiagToCardinal,Y                ; 9EB3  b9 17 a0
    BMI loc_26_9EEF                     ; 9EB6  30 37
    STA pad_dir                         ; 9EB8  85 49
loc_26_9EBA:
    LDA pad_dir                         ; 9EBA  a5 49
    STA obj_meta_dir,X                  ; 9EBC  9d 60 03
    DEC obj_timer,X                     ; 9EBF  de 80 04
    JSR ObjMoveSlide                    ; 9EC2  20 d7 98
    BCS loc_26_9ED8                     ; 9EC5  b0 11
    LDA tmp_25                          ; 9EC7  a5 25
    STA obj_spawn_param_s01             ; 9EC9  8d 61 05
    LDA tmp_23                          ; 9ECC  a5 23
    STA obj_meta_dir,X                  ; 9ECE  9d 60 03
    LDA #$08                            ; 9ED1  a9 08
    STA obj_param2,X                    ; 9ED3  9d a0 06
    BNE loc_26_9EEF                     ; 9ED6  d0 17
loc_26_9ED8:
    LDA obj_param2,X                    ; 9ED8  bd a0 06
    BEQ loc_26_9EEF                     ; 9EDB  f0 12
    DEC obj_param2,X                    ; 9EDD  de a0 06
    LDA tmp_26                          ; 9EE0  a5 26
    STA obj_meta_dir,X                  ; 9EE2  9d 60 03
    JSR ObjMoveSlide                    ; 9EE5  20 d7 98
    BCS loc_26_9EEF                     ; 9EE8  b0 05
    LDA tmp_25                          ; 9EEA  a5 25
    STA obj_spawn_param_s01             ; 9EEC  8d 61 05
loc_26_9EEF:
    LDA tmp_08                          ; 9EEF  a5 08
    AND #$03                            ; 9EF1  29 03
    BNE loc_26_9EFA                     ; 9EF3  d0 05
    JSR sub_26_8D59                     ; 9EF5  20 59 8d
    LDX #$00                            ; 9EF8  a2 00
loc_26_9EFA:
    JSR ObjPosToTemp                    ; 9EFA  20 a8 98
    LDA #$00                            ; 9EFD  a9 00
    LDY obj_timer2,X                    ; 9EFF  bc 20 06
    BNE loc_26_9F06                     ; 9F02  d0 02
    LDA #$40                            ; 9F04  a9 40
loc_26_9F06:
    JSR ObjBgProbe                      ; 9F06  20 30 9a
    LDA obj_timer2,X                    ; 9F09  bd 20 06
    AND #$F0                            ; 9F0C  29 f0
    BNE loc_26_9F37                     ; 9F0E  d0 27
    LDA obj_spawn_param_s01             ; 9F10  ad 61 05
    ORA tmp_20                          ; 9F13  05 20
    AND #$02                            ; 9F15  29 02
    BEQ loc_26_9F37                     ; 9F17  f0 1e
    LDA obj_behav,X                     ; 9F19  bd e0 06
    STA obj_act_timer,X                 ; 9F1C  9d 40 06
    LDA obj_timer2                      ; 9F1F  ad 20 06
    BEQ loc_26_9F37                     ; 9F22  f0 13
    INC obj_timer2                      ; 9F24  ee 20 06
    LDA mp_cur                          ; 9F27  ad 08 07
    BEQ loc_26_9F37                     ; 9F2A  f0 0b
    LDA tmp_08                          ; 9F2C  a5 08
    AND #$0F                            ; 9F2E  29 0f
    BNE loc_26_9F37                     ; 9F30  d0 05
    LDA #$01                            ; 9F32  a9 01
    JSR SpendMp                         ; 9F34  20 e8 9a
loc_26_9F37:
    LDA obj_timer2,X                    ; 9F37  bd 20 06
    BNE loc_26_9F4C                     ; 9F3A  d0 10
    LDA tmp_20                          ; 9F3C  a5 20
    AND #$40                            ; 9F3E  29 40
    STA jmp_trampoline                  ; 9F40  85 10
    LDA obj_bg_mask,X                   ; 9F42  bd 60 04
    AND #$BF                            ; 9F45  29 bf
    ORA jmp_trampoline                  ; 9F47  05 10
    STA obj_bg_mask,X                   ; 9F49  9d 60 04
loc_26_9F4C:
    LDA obj_bg_mask,X                   ; 9F4C  bd 60 04
    AND #$40                            ; 9F4F  29 40
    ORA obj_flags,X                     ; 9F51  1d 80 03
    STA obj_flags,X                     ; 9F54  9d 80 03
    LDA tmp_20                          ; 9F57  a5 20
    AND #$20                            ; 9F59  29 20
    BNE loc_26_9F95                     ; 9F5B  d0 38
    LDA player_bg_attr                  ; 9F5D  ad 41 06
    AND #$20                            ; 9F60  29 20
    BEQ loc_26_9F7B                     ; 9F62  f0 17
    LDA obj_aux,X                       ; 9F64  bd 60 06
    CMP #$07                            ; 9F67  c9 07
    BPL loc_26_9F7B                     ; 9F69  10 10
    LDA #$00                            ; 9F6B  a9 00
    STA obj_aux,X                       ; 9F6D  9d 60 06
loc_26_9F70:
    LDA #$04                            ; 9F70  a9 04
    STA obj_param1,X                    ; 9F72  9d 80 06
    LDA tmp_20                          ; 9F75  a5 20
    STA player_bg_attr                  ; 9F77  8d 41 06
    RTS                                 ; 9F7A  60
loc_26_9F7B:
    LDA tmp_20                          ; 9F7B  a5 20
    STA player_bg_attr                  ; 9F7D  8d 41 06
    LDA obj_aux,X                       ; 9F80  bd 60 06
    BEQ loc_26_9F70                     ; 9F83  f0 eb
    LDA tmp_08                          ; 9F85  a5 08
    AND #$03                            ; 9F87  29 03
    BNE loc_26_9F8E                     ; 9F89  d0 03
    DEC obj_aux,X                       ; 9F8B  de 60 06
loc_26_9F8E:
    LDA #$84                            ; 9F8E  a9 84
    STA obj_param1,X                    ; 9F90  9d 80 06
    BNE PlayerConveyor                  ; 9F93  d0 3d
loc_26_9F95:
    LDA tmp_20                          ; 9F95  a5 20
    STA player_bg_attr                  ; 9F97  8d 41 06
    JSR PlayerConveyor                  ; 9F9A  20 d2 9f
    LDA obj_timer2,X                    ; 9F9D  bd 20 06
    CMP #$01                            ; 9FA0  c9 01
    BEQ loc_26_9FB4                     ; 9FA2  f0 10
    LDA obj_timer2,X                    ; 9FA4  bd 20 06
    BNE loc_26_9FD1                     ; 9FA7  d0 28
    BIT obj1_flash                      ; 9FA9  2c 10 07
    BVS loc_26_9FC4                     ; 9FAC  70 16
    LDA tmp_08                          ; 9FAE  a5 08
    AND #$0F                            ; 9FB0  29 0f
    BNE loc_26_9FD1                     ; 9FB2  d0 1d
loc_26_9FB4:
    LDA pad_dir                         ; 9FB4  a5 49
    CMP #$04                            ; 9FB6  c9 04
    BNE loc_26_9FC4                     ; 9FB8  d0 0a
    LDA #$06                            ; 9FBA  a9 06
    CMP obj_aux,X                       ; 9FBC  dd 60 06
    BCC loc_26_9FC4                     ; 9FBF  90 03
    STA obj_aux,X                       ; 9FC1  9d 60 06
loc_26_9FC4:
    INC obj_aux,X                       ; 9FC4  fe 60 06
    LDA #$09                            ; 9FC7  a9 09
    CMP obj_aux,X                       ; 9FC9  dd 60 06
    BCS loc_26_9FD1                     ; 9FCC  b0 03
    STA obj_aux,X                       ; 9FCE  9d 60 06
loc_26_9FD1:
    RTS                                 ; 9FD1  60
PlayerConveyor:  ; s44: BG attr b5: move in obj_param1&$0F at speed obj_aux $0660 (ramps 0..9)
    LDA obj_meta_dir,X                  ; 9FD2  bd 60 03
    PHA                                 ; 9FD5  48
    LDA obj_flags2,X                    ; 9FD6  bd 40 03
    PHA                                 ; 9FD9  48
    LDA obj_timer,X                     ; 9FDA  bd 80 04
    PHA                                 ; 9FDD  48
    LDA tmp_08                          ; 9FDE  a5 08
    STA obj_timer,X                     ; 9FE0  9d 80 04
    LDA obj_aux,X                       ; 9FE3  bd 60 06
    STA obj_flags2,X                    ; 9FE6  9d 40 03
    LDA obj_param1,X                    ; 9FE9  bd 80 06
    AND #$0F                            ; 9FEC  29 0f
    STA obj_meta_dir,X                  ; 9FEE  9d 60 03
    JSR ObjMoveSlide                    ; 9FF1  20 d7 98
    BCC $A002                           ; 9FF4  90 0c
    LDA obj_meta_dir,X                  ; 9FF6  bd 60 03
    AND #$07                            ; 9FF9  29 07
    TAY                                 ; 9FFB  a8
    LDA $A00F,Y                         ; 9FFC  b9 0f a0
    .db $9D                             ; 9FFF  STA obj_param1,X -- continues in the next window
