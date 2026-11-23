; re-c bank 15  file 0x1E010  CPU $A000-$BFFF  (tools/disasm_all.py)

ppuctrl_shadow = $00
ppumask_shadow = $01
tmp_08 = $08
frame_ready = $09
jmp_trampoline = $10
tmp_11 = $11
tmp_12 = $12
tmp_17 = $17
tmp_18 = $18
tmp_19 = $19
tmp_1F = $1F
tmp_20 = $20
tmp_21 = $21
tmp_2E = $2E
tmp_2F = $2F
tmp_30 = $30
tmp_31 = $31
pos_x_lo = $34
pos_x_hi = $35
pos_y_lo = $36
pos_y_hi = $37
sub_state = $41
pad_new = $4B
irq_mode = $51
chr_r0_alt = $58
chr_r1_alt = $59
chr_r2_alt = $5A
chr_r3_alt = $5B
area_id = $6C
cur_bank16_x2 = $6E
obj_x_lo = $70
obj_x_lo_s01 = $71
obj_x_hi = $90
obj_y_lo = $B0
obj_y_lo_s01 = $B1
obj_y_lo_s0D = $BD
obj_y_hi = $D0
snd_cur_music = $0102
obj_meta_id = $0300
obj_meta_id_s01 = $0301
obj_meta_id_s0D = $030D
obj_tile_ofs = $0320
obj_flags2 = $0340
obj_meta_dir = $0360
obj_flags = $0380
obj_coll = $03A0
player_hp_max = $03C0
obj_shot_id = $0440
obj_timer = $0480
obj_active = $04A0
obj_active_s1F = $04BF
obj_anim = $04E0
obj_anim_s0E = $04EE
obj_anim_s13 = $04F3
obj_spawn_param = $0560
obj_overlay_id = $0580
obj_freeze = $05A0
obj_phase = $0600
obj_timer2 = $0620
obj_timer2_s03 = $0623
obj_act_timer = $0640
obj_aux = $0660
obj_param1 = $0680
obj_param2 = $06A0
orbit_level = $06C0
pending_msg_id = $06C3
pending_msg_group = $06E3
obj1_flash = $0710
equip_sword = $0711
equip_shield = $0714
ram_07D5 = $07D5
ram_07D6 = $07D6
boss_cam_lock = $07D7
ram_07DB = $07DB
text_msg_id = $07DE
text_group = $07DF
pal_ids = $07E0
pal_id_bg1 = $07E1
pal_id_bg2 = $07E2
pal_id_bg3 = $07E3
pal_id_spr2 = $07E6
pal_id_spr3 = $07E7
pal_nofade_mask = $07E8
chr_r1_play = $07F1
chr_r2_play = $07F2
chr_r3_play = $07F3
chr_r4_play = $07F4
chr_r5_play = $07F5
wram_buf = $6000
wram_6001 = $6001
wram_6002 = $6002
wram_6003 = $6003
wram_6004 = $6004
wram_6005 = $6005
wram_6006 = $6006
wram_6007 = $6007
wram_6020 = $6020
wram_6040 = $6040
wram_6041 = $6041
wram_605F = $605F
wram_6060 = $6060
attr_shadow = $6100
area_f62FE = $62FE
player_name_hi = $6410
player_name6 = $6418
DirToTarget = $8409
ObjStepVector = $8480
PaletteBuild = $8C0E
Bank26_FadeA = $8C3D
Bank26_FadeB = $8C49
HudRedraw = $8E46
SpawnShot = $972D
Rand64 = $97D7
TempAddOffset = $9824
TempAddStep = $9861
TempToObjPos = $9897
ObjPosToTemp = $98A8
sub_26_9B07 = $9B07
sub_30_C008 = $C008
PlaySound = $C125
WaitFrame = $C169
sub_30_C176 = $C176
sub_30_C1AE = $C1AE
SetR6 = $C418
PpuQueueBlock = $C482
WaitPpuQueue = $C676
ReadPads = $FE80
ReadPadsMenu = $FEE0
FarCall_C25D = $FF80
FarCall_B14_800E = $FF8C
FarCall_D354 = $FFCF

    .org $A000
    .db $C0,$00,$FF,$10,$08,$00,$00,$40,$DC,$FF,$10,$08,$00,$00,$40,$DD; 15:A000
    .db $FF,$18,$08,$00,$00,$40,$DE,$FF,$18,$08,$00,$00,$C0,$00,$FF,$20; 15:A010
    .db $08,$00,$00,$C0,$00,$FF,$08,$04,$00,$00,$41,$07,$FF,$08,$04,$00; 15:A020
    .db $00,$C0,$00,$FF,$28,$08,$00,$00,$C0,$00,$FF,$28,$08,$00,$00,$40; 15:A030
    .db $39,$FF,$28,$08,$00,$00,$40,$37,$FF,$28,$08,$00,$00,$40,$0E,$FF; 15:A040
    .db $28,$08,$00,$00,$40,$3F,$FF,$28,$08,$00,$00,$40,$63,$FF,$28,$08; 15:A050
    .db $00,$00,$C0,$00,$FF,$63,$01,$11,$81,$40,$25,$2C,$00,$C0,$00,$2C; 15:A060
    .db $01,$C0,$00,$2C,$02,$C0,$00,$2C,$02,$C0,$00,$28,$00,$C0,$00,$1A; 15:A070
    .db $18,$00,$C0,$00,$20,$00,$C0,$00,$16,$01,$E0,$42,$40,$50,$15,$01; 15:A080
    .db $30,$00,$40,$0F,$6B,$01,$30,$00,$40,$A4,$0F,$2C,$04,$40,$0A,$AD; 15:A090
    .db $02,$84,$03,$80,$84,$40,$0D,$AE,$02,$84,$03,$C0,$00,$64,$01,$01; 15:A0A0
    .db $21,$40,$21,$80,$9B,$48,$00,$40,$54,$2C,$06,$C0,$00,$AF,$02,$54; 15:A0B0
    .db $08,$02,$83,$40,$8F,$E4,$84,$07,$C0,$00,$7E,$01,$62,$62,$40,$3C; 15:A0C0
    .db $23,$01,$6A,$50,$40,$10,$75,$01,$70,$00,$40,$65,$87,$01,$34,$05; 15:A0D0
    .db $40,$6B,$86,$01,$34,$05,$40,$69,$85,$01,$34,$05,$40,$6A,$88,$01; 15:A0E0
    .db $34,$05,$40,$35,$C9,$01,$9C,$0A,$41,$09,$CA,$01,$A4,$09,$41,$0A; 15:A0F0
    .db $CB,$01,$AC,$0B,$40,$86         ; 15:A100
ChestPresenceRecs:  ; s72: 58 x 2-byte flag refs (all !Fxxx; b7 set); A110 = !F000 shared by unused params + TrapChest $70; ends at EventTriggerTable
    .db $A0,$E7,$A0,$E6,$A0,$E5,$A0,$B3,$A0,$AF,$A0,$00,$A0,$AA,$A0,$AB; 15:A106
    .db $A0,$AD,$A0,$AE,$A0,$B0,$A0,$B1,$A0,$B2,$A0,$B5,$A0,$B6,$A0,$B7; 15:A116
    .db $A0,$B8,$A0,$B9,$A0,$BA,$A0,$BB,$A0,$BC,$A0,$DC,$A0,$BD,$A0,$BE; 15:A126
    .db $A0,$BF,$A0,$C0,$A0,$C1,$A0,$C2,$A0,$C3,$A0,$C4,$A0,$C5,$A0,$C6; 15:A136
    .db $A0,$C7,$A0,$C8,$A0,$C9,$A0,$CA,$A0,$CB,$A0,$DF,$A0,$CC,$A0,$CD; 15:A146
    .db $A0,$CE,$A0,$CF,$A0,$D0,$A0,$D1,$A0,$D3,$A0,$3C,$A0,$78,$A0,$87; 15:A156
    .db $A0,$5F,$A0,$7D,$A0,$4E,$A0,$17,$A0,$D2,$A0,$D4,$A0,$DE,$A0,$7A; 15:A166
    .db $A0,$00,$A0,$E4                 ; 15:A176
EventTriggerTable:  ; (bank fixed s32: was 14:A17A; bank 15 at $A000 via SwitchBank16(7)) 67 ptrs for trigger ids $80-$C2: conds (all true) + result + set-list
    .db $00,$A2,$08,$A2,$10,$A2,$18,$A2,$1E,$A2,$24,$A2,$2C,$A2,$32,$A2; 15:A17A
    .db $38,$A2,$3E,$A2,$44,$A2,$4A,$A2,$50,$A2,$5A,$A2,$64,$A2,$6A,$A2; 15:A18A
    .db $72,$A2,$F4,$A2,$80,$A2,$86,$A2,$8C,$A2,$92,$A2,$98,$A2,$9E,$A2; 15:A19A
    .db $A4,$A2,$AA,$A2,$B0,$A2,$B8,$A2,$BE,$A2,$C4,$A2,$CA,$A2,$D0,$A2; 15:A1AA
    .db $D6,$A2,$DC,$A2,$E2,$A2,$E8,$A2,$EE,$A2,$7A,$A2,$FA,$A2,$00,$A3; 15:A1BA
    .db $06,$A3,$0C,$A3,$12,$A3,$1A,$A3,$20,$A3,$26,$A3,$2C,$A3,$32,$A3; 15:A1CA
    .db $38,$A3,$3E,$A3,$44,$A3,$4C,$A3,$52,$A3,$5A,$A3,$60,$A3,$6A,$A3; 15:A1DA
    .db $74,$A3,$7A,$A3,$80,$A3,$86,$A3,$90,$A3,$98,$A3,$A2,$A3,$A8,$A3; 15:A1EA
    .db $AE,$A3,$B4,$A3,$BA,$A3,$20,$27,$80,$5F,$03,$B3,$40,$27,$A0,$3B; 15:A1FA
    .db $00,$00,$00,$26,$42,$FD,$00,$5F,$A0,$2D,$B8,$00,$40,$2D,$A0,$64; 15:A20A
    .db $00,$00,$40,$64,$A0,$51,$5B,$B2,$40,$51,$02,$F7,$A0,$0E,$C0,$60; 15:A21A
    .db $C0,$00,$A0,$A9,$CB,$8F,$C0,$00,$A0,$55,$D3,$01,$C0,$00,$A0,$56; 15:A22A
    .db $D3,$03,$C0,$00,$A0,$57,$D3,$05,$C0,$00,$A2,$FB,$CB,$B0,$C0,$00; 15:A23A
    .db $A0,$00,$00,$00,$42,$FB,$A0,$38,$00,$00,$00,$85,$00,$38,$40,$84; 15:A24A
    .db $00,$37,$A0,$47,$00,$00,$00,$47,$C0,$85,$A0,$00,$00,$00,$42,$F7; 15:A25A
    .db $80,$53,$D8,$00,$00,$45,$C0,$53,$A0,$00,$00,$00,$80,$A1,$42,$FA; 15:A26A
    .db $A0,$7E,$00,$AF,$40,$7E,$A0,$00,$00,$00,$40,$18,$A0,$20,$01,$4F; 15:A27A
    .db $C0,$00,$A0,$00,$00,$00,$40,$5B,$A0,$00,$38,$00,$40,$1B,$80,$13; 15:A28A
    .db $00,$00,$40,$2F,$A0,$00,$00,$00,$C0,$19,$A0,$66,$E8,$C0,$C0,$00; 15:A29A
    .db $A0,$00,$00,$00,$42,$F5,$00,$27,$A0,$67,$EB,$24,$C0,$00,$A0,$5D; 15:A2AA
    .db $EB,$00,$C0,$00,$A0,$60,$EB,$02,$C0,$00,$A0,$62,$EB,$04,$C0,$00; 15:A2BA
    .db $A0,$5C,$EB,$08,$C0,$00,$A0,$6C,$EB,$60,$C0,$00,$A0,$5E,$EB,$64; 15:A2CA
    .db $C0,$00,$A0,$EA,$03,$68,$40,$EA,$A0,$EB,$03,$69,$40,$EB,$A0,$EC; 15:A2DA
    .db $03,$6A,$40,$EC,$A0,$68,$F3,$6B,$40,$68,$A0,$7F,$04,$12,$40,$7F; 15:A2EA
    .db $A0,$00,$00,$00,$42,$F6,$A0,$00,$00,$00,$42,$F8,$A0,$00,$00,$00; 15:A2FA
    .db $42,$F9,$A0,$00,$00,$00,$42,$FC,$80,$A8,$00,$00,$40,$43,$42,$F7; 15:A30A
    .db $A0,$00,$00,$00,$42,$FE,$A0,$00,$00,$00,$42,$FF,$A0,$00,$00,$00; 15:A31A
    .db $40,$93,$A0,$00,$00,$00,$40,$94,$A0,$00,$00,$00,$40,$95,$80,$21; 15:A32A
    .db $F8,$00,$C0,$00,$A0,$00,$E8,$00,$C0,$00,$A0,$37,$43,$90,$00,$37; 15:A33A
    .db $40,$83,$A0,$00,$00,$00,$40,$8C,$00,$0A,$A0,$39,$78,$1B,$40,$39; 15:A34A
    .db $A0,$3F,$78,$AB,$40,$3F,$20,$13,$A0,$DB,$02,$0E,$00,$DB,$40,$8E; 15:A35A
    .db $80,$9C,$00,$00,$80,$9C,$80,$A2,$40,$20,$80,$00,$00,$00,$C0,$00; 15:A36A
    .db $A0,$00,$00,$00,$C0,$9F,$A0,$3F,$CB,$8F,$C0,$00,$20,$9E,$20,$1F; 15:A37A
    .db $80,$20,$D8,$00,$C0,$00,$20,$99,$A0,$28,$D8,$00,$C0,$00,$20,$2B; 15:A38A
    .db $20,$26,$A0,$27,$D8,$00,$C0,$00,$A0,$7B,$50,$00,$C0,$00,$80,$6A; 15:A39A
    .db $00,$00,$40,$90,$80,$6B,$00,$00,$40,$91,$80,$72,$00,$00,$40,$DA; 15:A3AA
    .db $A0,$00,$00,$00,$C0,$DB         ; 15:A3BA
StaleFill15_A3C0:  ; dead filler to A3FF: copy of 13:A3C0
    .db $C2,$70,$11,$B7,$C2,$70,$3C,$B7,$C2,$70,$27,$11,$00,$48,$26,$47; 15:A3C0
    .db $00,$48,$27,$A0,$40,$48,$27,$B0,$40,$48,$27,$C2,$40,$48,$27,$CB; 15:A3D0
    .db $00,$48,$12,$18,$00,$36,$3C,$27,$42,$58,$3C,$17,$42,$6E,$FF,$00; 15:A3E0
    .db $18,$18,$61,$74,$24,$07,$C1,$C8,$31,$0A,$00,$3F,$37,$05,$00,$3F; 15:A3F0
BossPhaseDispatch:  ; A=boss base: JMP (15:A413[(A+$0600 X)*2]) per-boss phase handlers
    CLC                                 ; A400  18
    ADC obj_phase,X                     ; A401  7d 00 06
    ASL A                               ; A404  0a
    TAY                                 ; A405  a8
    LDA BossPhaseTable,Y                ; A406  b9 13 a4
    STA jmp_trampoline                  ; A409  85 10
    LDA $A414,Y                         ; A40B  b9 14 a4
    STA tmp_11                          ; A40E  85 11
    JMP (jmp_trampoline)                ; A410  6c 10 00
BossPhaseTable:  ; ptr16[] phase handlers for BossPhaseDispatch
    .dw sub_15_A48B                     ; 15:A413
    .dw sub_15_A4C3                     ; 15:A415
    .dw sub_15_A4CC                     ; 15:A417
    .dw sub_15_A4D8                     ; 15:A419
    .dw sub_15_A50B                     ; 15:A41B
    .dw sub_15_A54A                     ; 15:A41D
    .dw sub_15_A565                     ; 15:A41F
    .dw sub_15_A581                     ; 15:A421
    .dw sub_15_A684                     ; 15:A423
    .dw sub_15_A87E                     ; 15:A425
    .dw sub_15_A971                     ; 15:A427
    .dw sub_15_A9C6                     ; 15:A429
    .dw sub_15_AA46                     ; 15:A42B
    .dw sub_15_AAA7                     ; 15:A42D
    .dw sub_15_AAE6                     ; 15:A42F
    .dw sub_15_ABE4                     ; 15:A431
    .dw sub_15_ABFC                     ; 15:A433
    .dw sub_15_AC0F                     ; 15:A435
    .dw sub_15_AC1A                     ; 15:A437
    .dw sub_15_AC1E                     ; 15:A439
    .dw sub_15_AC31                     ; 15:A43B
    .dw sub_15_AC41                     ; 15:A43D
    .dw sub_15_AC5B                     ; 15:A43F
    .dw sub_15_AC8F                     ; 15:A441
    .dw sub_15_ACF5                     ; 15:A443
    .db $00,$00,$00,$00,$E8,$AD,$35,$AE,$43,$AE,$5A,$AE,$C9,$AE,$EC,$AE; 15:A445
    .db $5C,$AF,$AD,$AF,$51,$B0,$C5,$B0,$F4,$B0,$05,$B1,$4F,$B1,$A5,$B1; 15:A455
    .db $15,$B2,$2C,$B2,$01,$B3,$BC,$B3,$DD,$B4,$24,$B5,$52,$B5,$87,$B5; 15:A465
    .db $7A,$B6                         ; 15:A475
BossAiE0:  ; boss AI $E0 (types $57 $A5) phase base $00 x8
    LDA #$C0                            ; A477  a9 c0
    JSR sub_26_9B07                     ; A479  20 07 9b
    LDA obj_phase,X                     ; A47C  bd 00 06
    BEQ loc_15_A486                     ; A47F  f0 05
    LDA #$10                            ; A481  a9 10
    STA boss_cam_lock                   ; A483  8d d7 07
loc_15_A486:
    LDA #$00                            ; A486  a9 00
    JMP BossPhaseDispatch               ; A488  4c 00 a4
sub_15_A48B:
    LDA obj_flags,X                     ; A48B  bd 80 03
    BMI loc_15_A4C2                     ; A48E  30 32
    JSR ClearEnemySlots                 ; A490  20 29 bb
    LDA #$21                            ; A493  a9 21
    STA pal_id_spr2                     ; A495  8d e6 07
    STA pal_id_spr3                     ; A498  8d e7 07
    LDA #$6C                            ; A49B  a9 6c
    STA chr_r4_play                     ; A49D  8d f4 07
    STA chr_r5_play                     ; A4A0  8d f5 07
    LDA #$10                            ; A4A3  a9 10
    STA obj_freeze                      ; A4A5  8d a0 05
    LDA #$FF                            ; A4A8  a9 ff
    STA boss_cam_lock                   ; A4AA  8d d7 07
    LDA ram_07D5                        ; A4AD  ad d5 07
    BEQ loc_15_A4C2                     ; A4B0  f0 10
    LDA #$00                            ; A4B2  a9 00
    STA obj_freeze                      ; A4B4  8d a0 05
    LDA #$12                            ; A4B7  a9 12
    JSR PlaySound                       ; A4B9  20 25 c1
    INC obj_phase,X                     ; A4BC  fe 00 06
    INC obj_phase,X                     ; A4BF  fe 00 06
loc_15_A4C2:
    RTS                                 ; A4C2  60
sub_15_A4C3:
    DEC obj_timer,X                     ; A4C3  de 80 04
    BNE loc_15_A4CB                     ; A4C6  d0 03
    INC obj_phase,X                     ; A4C8  fe 00 06
loc_15_A4CB:
    RTS                                 ; A4CB  60
sub_15_A4CC:
    JSR sub_15_A5D5                     ; A4CC  20 d5 a5
    LDA #$00                            ; A4CF  a9 00
    STA obj_anim,X                      ; A4D1  9d e0 04
    INC obj_phase,X                     ; A4D4  fe 00 06
    RTS                                 ; A4D7  60
sub_15_A4D8:
    INC obj_anim,X                      ; A4D8  fe e0 04
    LDA obj_anim,X                      ; A4DB  bd e0 04
    AND #$0F                            ; A4DE  29 0f
    BNE loc_15_A4CB                     ; A4E0  d0 e9
    STA obj_anim,X                      ; A4E2  9d e0 04
    LDA obj_coll,X                      ; A4E5  bd a0 03
    AND #$0F                            ; A4E8  29 0f
    STA obj_coll,X                      ; A4EA  9d a0 03
    LDA #$01                            ; A4ED  a9 01
    STA obj_flags,X                     ; A4EF  9d 80 03
    LDA #$4C                            ; A4F2  a9 4c
    STA obj_meta_id,X                   ; A4F4  9d 00 03
    INC obj_phase,X                     ; A4F7  fe 00 06
    RTS                                 ; A4FA  60
    .db $C3,$37,$B9,$4B,$65,$9A,$78,$95,$23,$C6,$DB,$97,$2B,$44,$7B,$83; 15:A4FB
sub_15_A50B:
    LDA obj_aux,X                       ; A50B  bd 60 06
    CLC                                 ; A50E  18
    ADC #$01                            ; A50F  69 01
    AND #$0F                            ; A511  29 0f
    STA obj_aux,X                       ; A513  9d 60 06
    CMP #$0C                            ; A516  c9 0c
    BCS loc_15_A524                     ; A518  b0 0a
    INC obj_anim,X                      ; A51A  fe e0 04
    LDA obj_anim,X                      ; A51D  bd e0 04
    CMP #$80                            ; A520  c9 80
    BNE loc_15_A4CB                     ; A522  d0 a7
loc_15_A524:
    INC obj_phase,X                     ; A524  fe 00 06
    LDA #$00                            ; A527  a9 00
    STA obj_timer2,X                    ; A529  9d 20 06
    LDA tmp_08                          ; A52C  a5 08
    AND #$0F                            ; A52E  29 0f
    TAY                                 ; A530  a8
    LDA $A4FB,Y                         ; A531  b9 fb a4
    AND #$F0                            ; A534  29 f0
    STA obj_x_lo,X                      ; A536  95 70
    LDA $A4FB,Y                         ; A538  b9 fb a4
    ASL A                               ; A53B  0a
    ASL A                               ; A53C  0a
    ASL A                               ; A53D  0a
    ASL A                               ; A53E  0a
    STA obj_y_lo,X                      ; A53F  95 b0
    LDA obj_x_hi                        ; A541  a5 90
    STA obj_x_hi,X                      ; A543  95 90
    LDA obj_y_hi                        ; A545  a5 d0
    STA obj_y_hi,X                      ; A547  95 d0
    RTS                                 ; A549  60
sub_15_A54A:
    LDA #$65                            ; A54A  a9 65
    STA obj_meta_id,X                   ; A54C  9d 00 03
    LDA obj_coll,X                      ; A54F  bd a0 03
    ORA #$30                            ; A552  09 30
    STA obj_coll,X                      ; A554  9d a0 03
    JSR sub_15_A5D5                     ; A557  20 d5 a5
    LDA #$00                            ; A55A  a9 00
    STA obj_anim,X                      ; A55C  9d e0 04
    INC obj_phase,X                     ; A55F  fe 00 06
    JMP loc_15_A5A2                     ; A562  4c a2 a5
sub_15_A565:
    INC obj_anim,X                      ; A565  fe e0 04
    LDA obj_anim,X                      ; A568  bd e0 04
    AND #$0F                            ; A56B  29 0f
    BNE loc_15_A5A1                     ; A56D  d0 32
    STA obj_anim,X                      ; A56F  9d e0 04
    LDA player_hp_max,X                 ; A572  bd c0 03
    ASL A                               ; A575  0a
    ASL A                               ; A576  0a
    CLC                                 ; A577  18
    ADC #$10                            ; A578  69 10
    STA obj_anim,X                      ; A57A  9d e0 04
    INC obj_phase,X                     ; A57D  fe 00 06
    RTS                                 ; A580  60
sub_15_A581:
    LDA obj_flags2,X                    ; A581  bd 40 03
    BMI loc_15_A592                     ; A584  30 0c
    DEC obj_anim,X                      ; A586  de e0 04
    BNE loc_15_A5A1                     ; A589  d0 16
    LDA obj_anim,X                      ; A58B  bd e0 04
    AND #$07                            ; A58E  29 07
    BNE loc_15_A5A1                     ; A590  d0 0f
loc_15_A592:
    LDY #$01                            ; A592  a0 01
    JSR DirToTarget                     ; A594  20 09 84
    LDA tmp_20                          ; A597  a5 20
    STA obj_meta_dir,X                  ; A599  9d 60 03
    LDA #$02                            ; A59C  a9 02
    STA obj_phase,X                     ; A59E  9d 00 06
loc_15_A5A1:
    RTS                                 ; A5A1  60
loc_15_A5A2:
    LDA obj_aux,X                       ; A5A2  bd 60 06
    AND #$03                            ; A5A5  29 03
    BNE loc_15_A5D4                     ; A5A7  d0 2b
    LDA #$02                            ; A5A9  a9 02
    STA tmp_2F                          ; A5AB  85 2f
loc_15_A5AD:
    LDY tmp_2F                          ; A5AD  a4 2f
    LDA obj_shot_id,X                   ; A5AF  bd 40 04
    JSR SpawnShot                       ; A5B2  20 2d 97
    BCC loc_15_A5D4                     ; A5B5  90 1d
    LDY jmp_trampoline                  ; A5B7  a4 10
    LDA tmp_2F                          ; A5B9  a5 2f
    ASL A                               ; A5BB  0a
    ASL A                               ; A5BC  0a
    SEC                                 ; A5BD  38
    SBC #$06                            ; A5BE  e9 06
    STA obj_param1,Y                    ; A5C0  99 80 06
    LDA obj_y_lo,X                      ; A5C3  b5 b0
    CMP obj_y_lo                        ; A5C5  c5 b0
    LDA #$FC                            ; A5C7  a9 fc
    BCC loc_15_A5CD                     ; A5C9  90 02
    LDA #$04                            ; A5CB  a9 04
loc_15_A5CD:
    STA obj_param2,Y                    ; A5CD  99 a0 06
    DEC tmp_2F                          ; A5D0  c6 2f
    BNE loc_15_A5AD                     ; A5D2  d0 d9
loc_15_A5D4:
    RTS                                 ; A5D4  60
sub_15_A5D5:
    LDA #$08                            ; A5D5  a9 08
    STA tmp_1F                          ; A5D7  85 1f
loc_15_A5D9:
    LDY tmp_1F                          ; A5D9  a4 1f
    LDA #$3C                            ; A5DB  a9 3c
    JSR SpawnShot                       ; A5DD  20 2d 97
    BCC loc_15_A5ED                     ; A5E0  90 0b
    LDY jmp_trampoline                  ; A5E2  a4 10
    LDA tmp_1F                          ; A5E4  a5 1f
    STA obj_freeze,Y                    ; A5E6  99 a0 05
    DEC tmp_1F                          ; A5E9  c6 1f
    BNE loc_15_A5D9                     ; A5EB  d0 ec
loc_15_A5ED:
    RTS                                 ; A5ED  60
sub_15_A5EE:
    STX jmp_trampoline                  ; A5EE  86 10
    LDX #$01                            ; A5F0  a2 01
    JSR ObjPosToTemp                    ; A5F2  20 a8 98
    LDX jmp_trampoline                  ; A5F5  a6 10
    LDA obj_flags,X                     ; A5F7  bd 80 03
    BPL loc_15_A602                     ; A5FA  10 06
    LDA #$00                            ; A5FC  a9 00
    STA obj_active,X                    ; A5FE  9d a0 04
    RTS                                 ; A601  60
loc_15_A602:
    LDA obj_meta_dir                    ; A602  ad 60 03
    CLC                                 ; A605  18
    ADC #$04                            ; A606  69 04
    AND #$07                            ; A608  29 07
    STA obj_meta_dir,X                  ; A60A  9d 60 03
    LDA obj_param1,X                    ; A60D  bd 80 06
    STA tmp_30                          ; A610  85 30
    LDA obj_param2,X                    ; A612  bd a0 06
    STA tmp_31                          ; A615  85 31
    LDA obj_anim,X                      ; A617  bd e0 04
    AND #$07                            ; A61A  29 07
    BNE loc_15_A658                     ; A61C  d0 3a
    LDA pos_x_lo                        ; A61E  a5 34
    SEC                                 ; A620  38
    SBC obj_x_lo,X                      ; A621  f5 70
    LDA pos_x_hi                        ; A623  a5 35
    SBC obj_x_hi,X                      ; A625  f5 90
    BPL loc_15_A62D                     ; A627  10 04
    LDA #$FF                            ; A629  a9 ff
    BMI loc_15_A62F                     ; A62B  30 02
loc_15_A62D:
    LDA #$01                            ; A62D  a9 01
loc_15_A62F:
    CLC                                 ; A62F  18
    ADC obj_param1,X                    ; A630  7d 80 06
    JSR sub_15_A665                     ; A633  20 65 a6
    STA obj_param1,X                    ; A636  9d 80 06
    STA tmp_30                          ; A639  85 30
    LDA pos_y_lo                        ; A63B  a5 36
    SEC                                 ; A63D  38
    SBC obj_y_lo,X                      ; A63E  f5 b0
    LDA pos_y_hi                        ; A640  a5 37
    SBC obj_y_hi,X                      ; A642  f5 d0
    BPL loc_15_A64A                     ; A644  10 04
    LDA #$FF                            ; A646  a9 ff
    BMI loc_15_A64C                     ; A648  30 02
loc_15_A64A:
    LDA #$01                            ; A64A  a9 01
loc_15_A64C:
    CLC                                 ; A64C  18
    ADC obj_param2,X                    ; A64D  7d a0 06
    JSR sub_15_A665                     ; A650  20 65 a6
    STA obj_param2,X                    ; A653  9d a0 06
    STA tmp_31                          ; A656  85 31
loc_15_A658:
    JSR ObjPosToTemp                    ; A658  20 a8 98
    JSR TempAddOffset                   ; A65B  20 24 98
    JSR TempToObjPos                    ; A65E  20 97 98
    DEC obj_anim,X                      ; A661  de e0 04
    RTS                                 ; A664  60
sub_15_A665:
    BMI loc_15_A66E                     ; A665  30 07
    CMP #$03                            ; A667  c9 03
    BCC loc_15_A66D                     ; A669  90 02
    LDA #$03                            ; A66B  a9 03
loc_15_A66D:
    RTS                                 ; A66D  60
loc_15_A66E:
    CMP #$FE                            ; A66E  c9 fe
    BCS loc_15_A674                     ; A670  b0 02
    LDA #$FD                            ; A672  a9 fd
loc_15_A674:
    RTS                                 ; A674  60
BossAiE2:  ; boss AI $E2 phase base $08 x2
    LDA #$C7                            ; A675  a9 c7
    JSR sub_26_9B07                     ; A677  20 07 9b
    LDA #$FF                            ; A67A  a9 ff
    STA boss_cam_lock                   ; A67C  8d d7 07
    LDA #$08                            ; A67F  a9 08
    JMP BossPhaseDispatch               ; A681  4c 00 a4
sub_15_A684:
    LDA #$10                            ; A684  a9 10
    STA obj_freeze                      ; A686  8d a0 05
    LDA ram_07D5                        ; A689  ad d5 07
    BNE loc_15_A68F                     ; A68C  d0 01
    RTS                                 ; A68E  60
loc_15_A68F:
    LDA #$12                            ; A68F  a9 12
    JSR PlaySound                       ; A691  20 25 c1
    INC obj_phase,X                     ; A694  fe 00 06
    LDA #$00                            ; A697  a9 00
    STA area_f62FE                      ; A699  8d fe 62
    LDA #$2C                            ; A69C  a9 2c
    STA chr_r1_play                     ; A69E  8d f1 07
    LDA #$78                            ; A6A1  a9 78
    STA chr_r4_play                     ; A6A3  8d f4 07
    LDA #$6F                            ; A6A6  a9 6f
    STA chr_r5_play                     ; A6A8  8d f5 07
    TXA                                 ; A6AB  8a
    PHA                                 ; A6AC  48
    LDA #$AF                            ; A6AD  a9 af
    STA pal_ids                         ; A6AF  8d e0 07
    STA pal_id_bg1                      ; A6B2  8d e1 07
    STA pal_id_bg2                      ; A6B5  8d e2 07
    LDA #$4F                            ; A6B8  a9 4f
    STA pal_id_spr2                     ; A6BA  8d e6 07
    STA pal_id_spr3                     ; A6BD  8d e7 07
    JSR PaletteBuild                    ; A6C0  20 0e 8c
    LDY #$00                            ; A6C3  a0 00
    JSR sub_15_A73C                     ; A6C5  20 3c a7
    LDY #$16                            ; A6C8  a0 16
    STY pal_id_spr2                     ; A6CA  8c e6 07
    INY                                 ; A6CD  c8
    STY pal_id_spr3                     ; A6CE  8c e7 07
    LDA #$38                            ; A6D1  a9 38
    STA pal_nofade_mask                 ; A6D3  8d e8 07
    JSR FarCall_B14_800E                ; A6D6  20 8c ff
    LDA #$10                            ; A6D9  a9 10
    JSR Bank26_FadeB                    ; A6DB  20 49 8c
    LDA #$10                            ; A6DE  a9 10
    JSR Bank26_FadeA                    ; A6E0  20 3d 8c
    LDY #$13                            ; A6E3  a0 13
    JSR sub_15_A73C                     ; A6E5  20 3c a7
    LDA #$C8                            ; A6E8  a9 c8
    STA obj_meta_id_s0D                 ; A6EA  8d 0d 03
    LDA #$88                            ; A6ED  a9 88
    STA $7D                             ; A6EF  85 7d
    LDA #$60                            ; A6F1  a9 60
    STA obj_y_lo_s0D                    ; A6F3  85 bd
    JSR FarCall_B14_800E                ; A6F5  20 8c ff
    JSR WaitPpuQueue                    ; A6F8  20 76 c6
    LDX #$00                            ; A6FB  a2 00
loc_15_A6FD:
    LDA $A77C,X                         ; A6FD  bd 7c a7
    STA wram_buf,X                      ; A700  9d 00 60
    INX                                 ; A703  e8
    CPX #$90                            ; A704  e0 90
    BNE loc_15_A6FD                     ; A706  d0 f5
    LDX #$17                            ; A708  a2 17
loc_15_A70A:
    LDA $6078,X                         ; A70A  bd 78 60
    STA attr_shadow,X                   ; A70D  9d 00 61
    DEX                                 ; A710  ca
    BPL loc_15_A70A                     ; A711  10 f7
    LDA #$2C                            ; A713  a9 2c
    JSR PpuQueueBlock                   ; A715  20 82 c4
    LDA #$2D                            ; A718  a9 2d
    JSR PpuQueueBlock                   ; A71A  20 82 c4
    LDY #$88                            ; A71D  a0 88
    STY pal_ids                         ; A71F  8c e0 07
    INY                                 ; A722  c8
    STY pal_id_bg1                      ; A723  8c e1 07
    INY                                 ; A726  c8
    STY pal_id_bg2                      ; A727  8c e2 07
    LDA #$20                            ; A72A  a9 20
    JSR Bank26_FadeB                    ; A72C  20 49 8c
    LDA #$00                            ; A72F  a9 00
    STA pal_nofade_mask                 ; A731  8d e8 07
    PLA                                 ; A734  68
    TAX                                 ; A735  aa
    LDA #$13                            ; A736  a9 13
    STA obj_coll,X                      ; A738  9d a0 03
    RTS                                 ; A73B  60
sub_15_A73C:
    LDX #$0E                            ; A73C  a2 0e
loc_15_A73E:
    LDA $A814,Y                         ; A73E  b9 14 a8
    CMP #$FF                            ; A741  c9 ff
    BEQ loc_15_A76A                     ; A743  f0 25
    STA obj_x_lo,X                      ; A745  95 70
    LDA $A815,Y                         ; A747  b9 15 a8
    STA obj_y_lo,X                      ; A74A  95 b0
    LDA $9D                             ; A74C  a5 9d
    STA obj_x_hi,X                      ; A74E  95 90
    LDA $DD                             ; A750  a5 dd
    STA obj_y_hi,X                      ; A752  95 d0
    STX jmp_trampoline                  ; A754  86 10
    LDA #$C2                            ; A756  a9 c2
    STA tmp_11                          ; A758  85 11
    JSR FarCall_C25D                    ; A75A  20 80 ff
    LDA $A816,Y                         ; A75D  b9 16 a8
    STA obj_meta_id,X                   ; A760  9d 00 03
    INY                                 ; A763  c8
    INY                                 ; A764  c8
    INY                                 ; A765  c8
    INX                                 ; A766  e8
    JMP loc_15_A73E                     ; A767  4c 3e a7
loc_15_A76A:
    LDA obj_active,X                    ; A76A  bd a0 04
    CMP #$7F                            ; A76D  c9 7f
    BEQ loc_15_A773                     ; A76F  f0 02
    LDA #$00                            ; A771  a9 00
loc_15_A773:
    STA obj_active,X                    ; A773  9d a0 04
    INX                                 ; A776  e8
    CPX #$1F                            ; A777  e0 1f
    BNE loc_15_A76A                     ; A779  d0 ef
    RTS                                 ; A77B  60
    .db $FE,$FE,$80,$81,$82,$83,$84,$85,$FE,$FE,$FE,$86,$87,$88,$89,$8A; 15:A77C
    .db $8B,$8C,$8D,$FE,$FE,$8E,$8F,$90,$91,$92,$93,$94,$95,$FE,$86,$96; 15:A78C
    .db $97,$98,$99,$9A,$9B,$9C,$9D,$8D,$8F,$90,$9E,$9F,$A0,$A1,$A2,$A3; 15:A79C
    .db $93,$94,$97,$98,$88,$A4,$A5,$A6,$A7,$8B,$9B,$9C,$A8,$A9,$8F,$90; 15:A7AC
    .db $91,$92,$93,$94,$AA,$AB,$FE,$AC,$97,$98,$A5,$A6,$9B,$9C,$AD,$FE; 15:A7BC
    .db $FE,$FE,$AE,$AF,$B0,$B1,$B2,$B3,$FE,$FE,$FE,$FE,$B4,$B5,$B6,$B7; 15:A7CC
    .db $B8,$B9,$FE,$FE,$FE,$FE,$FE,$BA,$FE,$FE,$BB,$FE,$FE,$FE,$FE,$FE; 15:A7DC
    .db $FE,$FE,$FE,$FE,$FE,$FE,$FE,$FE,$00,$00,$00,$00,$40,$10,$00,$00; 15:A7EC
    .db $00,$00,$00,$28,$48,$21,$00,$00,$00,$00,$00,$08,$48,$11,$00,$00; 15:A7FC
    .db $00,$00,$00,$00,$04,$01,$00,$00,$68,$37,$C4,$78,$28,$C4,$78,$47; 15:A80C
    .db $C4,$99,$28,$C3,$99,$47,$C3,$A9,$37,$C3,$FF,$68,$48,$C6,$70,$58; 15:A81C
    .db $C6,$A8,$48,$C7,$A0,$58,$C7,$88,$34,$C5,$01,$08,$4C,$FF,$77,$48; 15:A82C
    .db $E4,$77,$57,$4C,$7F,$40,$E9,$71,$40,$E7,$77,$6D,$EA,$01,$08,$4C; 15:A83C
    .db $FF                             ; 15:A84C
loc_15_A84D:
    LDA #$08                            ; A84D  a9 08
    STA obj_anim,X                      ; A84F  9d e0 04
    DEC orbit_level,X                   ; A852  de c0 06
    LDA orbit_level,X                   ; A855  bd c0 06
    AND #$07                            ; A858  29 07
    BNE loc_15_A87D                     ; A85A  d0 21
    JSR Rand64                          ; A85C  20 d7 97
    BEQ loc_15_A868                     ; A85F  f0 07
    LDA #$3A                            ; A861  a9 3a
    LDY #$04                            ; A863  a0 04
    JMP SpawnShot                       ; A865  4c 2d 97
loc_15_A868:
    LDA #$39                            ; A868  a9 39
    JSR SpawnShot                       ; A86A  20 2d 97
    BCC loc_15_A87D                     ; A86D  90 0e
    STX tmp_11                          ; A86F  86 11
    LDX jmp_trampoline                  ; A871  a6 10
    LDA #$40                            ; A873  a9 40
    STA obj_tile_ofs,X                  ; A875  9d 20 03
    DEC a:obj_y_hi,X                    ; A878  de d0 00
    LDX tmp_11                          ; A87B  a6 11
loc_15_A87D:
    RTS                                 ; A87D  60
sub_15_A87E:
    LDA #$00                            ; A87E  a9 00
    STA obj_anim,X                      ; A880  9d e0 04
    LDA #$07                            ; A883  a9 07
    STA irq_mode                        ; A885  85 51
    LDA orbit_level,X                   ; A887  bd c0 06
    BNE loc_15_A84D                     ; A88A  d0 c1
    DEC obj_timer,X                     ; A88C  de 80 04
    BNE loc_15_A8BC                     ; A88F  d0 2b
    JSR Rand64                          ; A891  20 d7 97
    ADC obj_x_lo                        ; A894  65 70
    AND #$0F                            ; A896  29 0f
    CLC                                 ; A898  18
    ADC #$20                            ; A899  69 20
    STA obj_timer,X                     ; A89B  9d 80 04
    JSR Rand64                          ; A89E  20 d7 97
    CMP #$02                            ; A8A1  c9 02
    BCC loc_15_A8AA                     ; A8A3  90 05
    LDA #$80                            ; A8A5  a9 80
    STA orbit_level,X                   ; A8A7  9d c0 06
loc_15_A8AA:
    LDA obj_x_lo,X                      ; A8AA  b5 70
    CMP obj_x_lo_s01                    ; A8AC  c5 71
    LDA obj_x_hi,X                      ; A8AE  b5 90
    SBC $91                             ; A8B0  e5 91
    LDY #$06                            ; A8B2  a0 06
    BCS loc_15_A8B8                     ; A8B4  b0 02
    LDY #$02                            ; A8B6  a0 02
loc_15_A8B8:
    TYA                                 ; A8B8  98
    STA obj_meta_dir,X                  ; A8B9  9d 60 03
loc_15_A8BC:
    LDA #$00                            ; A8BC  a9 00
    LDY obj_x_lo,X                      ; A8BE  b4 70
    CPY #$30                            ; A8C0  c0 30
    BCS loc_15_A8C6                     ; A8C2  b0 02
    LDA #$02                            ; A8C4  a9 02
loc_15_A8C6:
    CPY #$D8                            ; A8C6  c0 d8
    BCC loc_15_A8CC                     ; A8C8  90 02
    LDA #$06                            ; A8CA  a9 06
loc_15_A8CC:
    TAY                                 ; A8CC  a8
    BEQ loc_15_A8D2                     ; A8CD  f0 03
    STA obj_meta_dir,X                  ; A8CF  9d 60 03
loc_15_A8D2:
    JSR sub_15_A8FF                     ; A8D2  20 ff a8
    LDA tmp_30                          ; A8D5  a5 30
    ASL A                               ; A8D7  0a
    EOR #$FF                            ; A8D8  49 ff
    SEC                                 ; A8DA  38
    ADC obj_anim_s13                    ; A8DB  6d f3 04
    STA obj_anim_s13                    ; A8DE  8d f3 04
    LDA obj_anim_s13                    ; A8E1  ad f3 04
    STA $04EF                           ; A8E4  8d ef 04
    CLC                                 ; A8E7  18
    ADC #$10                            ; A8E8  69 10
    STA obj_anim_s0E                    ; A8EA  8d ee 04
    LDA obj_anim_s13                    ; A8ED  ad f3 04
    EOR #$FF                            ; A8F0  49 ff
    CLC                                 ; A8F2  18
    ADC #$09                            ; A8F3  69 09
    STA $04F1                           ; A8F5  8d f1 04
    CLC                                 ; A8F8  18
    ADC #$10                            ; A8F9  69 10
    STA $04F0                           ; A8FB  8d f0 04
    RTS                                 ; A8FE  60
sub_15_A8FF:
    STX tmp_2F                          ; A8FF  86 2f
    JSR ObjPosToTemp                    ; A901  20 a8 98
    LDA obj_meta_dir,X                  ; A904  bd 60 03
    JSR ObjStepVector                   ; A907  20 80 84
    JSR TempAddStep                     ; A90A  20 61 98
    JSR TempToObjPos                    ; A90D  20 97 98
    LDX #$0E                            ; A910  a2 0e
loc_15_A912:
    JSR ObjPosToTemp                    ; A912  20 a8 98
    JSR TempAddStep                     ; A915  20 61 98
    JSR TempToObjPos                    ; A918  20 97 98
    INX                                 ; A91B  e8
    CPX #$13                            ; A91C  e0 13
    BNE loc_15_A912                     ; A91E  d0 f2
    LDA tmp_30                          ; A920  a5 30
    EOR #$FF                            ; A922  49 ff
    CLC                                 ; A924  18
    ADC #$01                            ; A925  69 01
    STA tmp_30                          ; A927  85 30
    LDA tmp_31                          ; A929  a5 31
    EOR #$FF                            ; A92B  49 ff
    CLC                                 ; A92D  18
    ADC #$01                            ; A92E  69 01
    STA tmp_31                          ; A930  85 31
    LDX #$13                            ; A932  a2 13
    JSR ObjPosToTemp                    ; A934  20 a8 98
    JSR TempAddStep                     ; A937  20 61 98
    JSR TempToObjPos                    ; A93A  20 97 98
    LDX tmp_2F                          ; A93D  a6 2f
    LDA obj_anim_s13                    ; A93F  ad f3 04
    STA $04EF                           ; A942  8d ef 04
    CLC                                 ; A945  18
    ADC #$10                            ; A946  69 10
    STA obj_anim_s0E                    ; A948  8d ee 04
    LDA obj_anim_s13                    ; A94B  ad f3 04
    EOR #$FF                            ; A94E  49 ff
    CLC                                 ; A950  18
    ADC #$09                            ; A951  69 09
    STA $04F1                           ; A953  8d f1 04
    CLC                                 ; A956  18
    ADC #$10                            ; A957  69 10
    STA $04F0                           ; A959  8d f0 04
    RTS                                 ; A95C  60
BossAiE3:  ; boss AI $E3 (types $68 $8B) phase base $0A x5
    LDA obj_phase,X                     ; A95D  bd 00 06
    BEQ loc_15_A967                     ; A960  f0 05
    LDA #$04                            ; A962  a9 04
    STA boss_cam_lock                   ; A964  8d d7 07
loc_15_A967:
    LDA #$C0                            ; A967  a9 c0
    JSR sub_26_9B07                     ; A969  20 07 9b
    LDA #$0A                            ; A96C  a9 0a
    JMP BossPhaseDispatch               ; A96E  4c 00 a4
sub_15_A971:
    LDA obj_flags,X                     ; A971  bd 80 03
    BMI loc_15_A9C5                     ; A974  30 4f
    LDA #$10                            ; A976  a9 10
    STA obj_freeze                      ; A978  8d a0 05
    LDA #$FF                            ; A97B  a9 ff
    STA boss_cam_lock                   ; A97D  8d d7 07
    LDA #$77                            ; A980  a9 77
    STA chr_r4_play                     ; A982  8d f4 07
    LDA #$1C                            ; A985  a9 1c
    STA pal_id_spr3                     ; A987  8d e7 07
    JSR PaletteBuild                    ; A98A  20 0e 8c
    JSR ClearEnemySlots                 ; A98D  20 29 bb
    LDA ram_07D5                        ; A990  ad d5 07
    BEQ loc_15_A9C5                     ; A993  f0 30
    LDA #$00                            ; A995  a9 00
    STA obj_freeze                      ; A997  8d a0 05
    LDA #$12                            ; A99A  a9 12
    JSR PlaySound                       ; A99C  20 25 c1
    LDA #$00                            ; A99F  a9 00
    STA pending_msg_id                  ; A9A1  8d c3 06
    LDA #$06                            ; A9A4  a9 06
    STA pending_msg_group               ; A9A6  8d e3 06
    LDA area_id                         ; A9A9  a5 6c
    CMP #$28                            ; A9AB  c9 28
    BEQ loc_15_A9B4                     ; A9AD  f0 05
    LDA #$18                            ; A9AF  a9 18
    STA pending_msg_group               ; A9B1  8d e3 06
loc_15_A9B4:
    LDA #$10                            ; A9B4  a9 10
    STA sub_state                       ; A9B6  85 41
    LDA #$09                            ; A9B8  a9 09
    STA obj_flags,X                     ; A9BA  9d 80 03
    LDA #$30                            ; A9BD  a9 30
    STA obj_coll,X                      ; A9BF  9d a0 03
    INC obj_phase,X                     ; A9C2  fe 00 06
loc_15_A9C5:
    RTS                                 ; A9C5  60
sub_15_A9C6:
    LDA obj_flags,X                     ; A9C6  bd 80 03
    BMI loc_15_A9FA                     ; A9C9  30 2f
    LDA tmp_08                          ; A9CB  a5 08
    AND #$07                            ; A9CD  29 07
    BNE loc_15_A9FA                     ; A9CF  d0 29
    JSR Rand64                          ; A9D1  20 d7 97
    BNE loc_15_A9FA                     ; A9D4  d0 24
    LDA #$03                            ; A9D6  a9 03
    STA obj_phase,X                     ; A9D8  9d 00 06
    LDA #$4F                            ; A9DB  a9 4f
    STA obj_timer,X                     ; A9DD  9d 80 04
    LDA #$4F                            ; A9E0  a9 4f
    STA obj_anim,X                      ; A9E2  9d e0 04
    LDA area_id                         ; A9E5  a5 6c
    CMP #$28                            ; A9E7  c9 28
    BEQ loc_15_A9C5                     ; A9E9  f0 da
    LDY #$01                            ; A9EB  a0 01
    JSR DirToTarget                     ; A9ED  20 09 84
    LDA tmp_20                          ; A9F0  a5 20
    ASL A                               ; A9F2  0a
    STA obj_act_timer,X                 ; A9F3  9d 40 06
    INC obj_phase,X                     ; A9F6  fe 00 06
    RTS                                 ; A9F9  60
loc_15_A9FA:
    LDA #$D3                            ; A9FA  a9 d3
    STA obj_meta_id,X                   ; A9FC  9d 00 03
    DEC obj_timer,X                     ; A9FF  de 80 04
    DEC obj_anim,X                      ; AA02  de e0 04
    LDA obj_timer,X                     ; AA05  bd 80 04
    AND #$07                            ; AA08  29 07
    BNE loc_15_AA37                     ; AA0A  d0 2b
    LDA a:obj_y_lo,X                    ; AA0C  bd b0 00
    CMP #$48                            ; AA0F  c9 48
    BCS loc_15_AA19                     ; AA11  b0 06
    INC obj_phase,X                     ; AA13  fe 00 06
    JMP loc_15_AA89                     ; AA16  4c 89 aa
loc_15_AA19:
    LDY #$00                            ; AA19  a0 00
    JSR DirToTarget                     ; AA1B  20 09 84
    LDA tmp_20                          ; AA1E  a5 20
    STA obj_meta_dir,X                  ; AA20  9d 60 03
    LDA tmp_08                          ; AA23  a5 08
    CMP #$A0                            ; AA25  c9 a0
    BCS loc_15_AA37                     ; AA27  b0 0e
    LDA tmp_21                          ; AA29  a5 21
    CMP #$02                            ; AA2B  c9 02
    BCS loc_15_AA37                     ; AA2D  b0 08
    INC obj_phase,X                     ; AA2F  fe 00 06
    LDA #$30                            ; AA32  a9 30
    STA obj_timer,X                     ; AA34  9d 80 04
loc_15_AA37:
    LDA obj_meta_dir,X                  ; AA37  bd 60 03
    JSR ObjStepVector                   ; AA3A  20 80 84
    JSR ObjPosToTemp                    ; AA3D  20 a8 98
    JSR TempAddOffset                   ; AA40  20 24 98
    JMP TempToObjPos                    ; AA43  4c 97 98
sub_15_AA46:
    DEC obj_anim,X                      ; AA46  de e0 04
    DEC obj_timer,X                     ; AA49  de 80 04
    BNE loc_15_AA5A                     ; AA4C  d0 0c
    DEC obj_phase,X                     ; AA4E  de 00 06
    LDA obj_meta_dir,X                  ; AA51  bd 60 03
    ORA #$04                            ; AA54  09 04
    STA obj_meta_dir,X                  ; AA56  9d 60 03
    RTS                                 ; AA59  60
loc_15_AA5A:
    LDA obj_timer,X                     ; AA5A  bd 80 04
    AND #$07                            ; AA5D  29 07
    BNE loc_15_AA37                     ; AA5F  d0 d6
    LDY #$00                            ; AA61  a0 00
    JSR DirToTarget                     ; AA63  20 09 84
    LDA tmp_20                          ; AA66  a5 20
    CLC                                 ; AA68  18
    ADC #$04                            ; AA69  69 04
    AND #$07                            ; AA6B  29 07
    TAY                                 ; AA6D  a8
    LDA $AA97,Y                         ; AA6E  b9 97 aa
    STA obj_meta_dir,X                  ; AA71  9d 60 03
    LDA obj_y_lo,X                      ; AA74  b5 b0
    CMP #$48                            ; AA76  c9 48
    BCC loc_15_AA89                     ; AA78  90 0f
    LDA obj_x_lo,X                      ; AA7A  b5 70
    CMP #$30                            ; AA7C  c9 30
    BCC loc_15_AA84                     ; AA7E  90 04
    CMP #$D0                            ; AA80  c9 d0
    BCC loc_15_AA37                     ; AA82  90 b3
loc_15_AA84:
    LDA #$00                            ; AA84  a9 00
    STA obj_meta_dir,X                  ; AA86  9d 60 03
loc_15_AA89:
    JSR Rand64                          ; AA89  20 d7 97
    TAY                                 ; AA8C  a8
    LDA $AA9F,Y                         ; AA8D  b9 9f aa
    STA obj_meta_dir,X                  ; AA90  9d 60 03
    DEC obj_phase,X                     ; AA93  de 00 06
    RTS                                 ; AA96  60
    .db $00,$01,$01,$01,$00,$07,$07,$07,$04,$03,$03,$03,$04,$05,$05,$05; 15:AA97
sub_15_AAA7:
    DEC obj_timer,X                     ; AAA7  de 80 04
    BNE loc_15_AAB2                     ; AAAA  d0 06
    DEC obj_phase,X                     ; AAAC  de 00 06
    JMP loc_15_AA89                     ; AAAF  4c 89 aa
loc_15_AAB2:
    LDA #$D5                            ; AAB2  a9 d5
    STA obj_meta_id,X                   ; AAB4  9d 00 03
    LDA obj_meta_id_s01                 ; AAB7  ad 01 03
    ASL A                               ; AABA  0a
    CLC                                 ; AABB  18
    ADC #$04                            ; AABC  69 04
    AND #$07                            ; AABE  29 07
    STA obj_meta_dir,X                  ; AAC0  9d 60 03
    LDA tmp_08                          ; AAC3  a5 08
    LSR A                               ; AAC5  4a
    BCC loc_15_AACB                     ; AAC6  90 03
    DEC obj_anim,X                      ; AAC8  de e0 04
loc_15_AACB:
    LDA obj_anim,X                      ; AACB  bd e0 04
    EOR #$07                            ; AACE  49 07
    AND #$0F                            ; AAD0  29 0f
    BNE loc_15_AAE5                     ; AAD2  d0 11
    LDA #$08                            ; AAD4  a9 08
    STA ram_07D6                        ; AAD6  8d d6 07
    LDA #$3D                            ; AAD9  a9 3d
    JSR PlaySound                       ; AADB  20 25 c1
    LDA #$38                            ; AADE  a9 38
    LDY #$00                            ; AAE0  a0 00
    JMP SpawnShot                       ; AAE2  4c 2d 97
loc_15_AAE5:
    RTS                                 ; AAE5  60
sub_15_AAE6:
    LDA #$D3                            ; AAE6  a9 d3
    STA obj_meta_id,X                   ; AAE8  9d 00 03
    DEC obj_timer,X                     ; AAEB  de 80 04
    BNE loc_15_AAF8                     ; AAEE  d0 08
    LDA #$02                            ; AAF0  a9 02
    STA obj_phase,X                     ; AAF2  9d 00 06
    JMP loc_15_AA89                     ; AAF5  4c 89 aa
loc_15_AAF8:
    LDA obj_timer,X                     ; AAF8  bd 80 04
    CMP #$20                            ; AAFB  c9 20
    BCS loc_15_AB24                     ; AAFD  b0 25
    LDA #$D4                            ; AAFF  a9 d4
    STA obj_meta_id,X                   ; AB01  9d 00 03
    LDA obj_timer,X                     ; AB04  bd 80 04
    LSR A                               ; AB07  4a
    BCS loc_15_AB24                     ; AB08  b0 1a
    LDA obj_timer,X                     ; AB0A  bd 80 04
    AND #$06                            ; AB0D  29 06
    BNE loc_15_AB1C                     ; AB0F  d0 0b
    LDY #$01                            ; AB11  a0 01
    JSR DirToTarget                     ; AB13  20 09 84
    LDA tmp_20                          ; AB16  a5 20
    ASL A                               ; AB18  0a
    JSR sub_15_AB25                     ; AB19  20 25 ab
loc_15_AB1C:
    LDY obj_act_timer,X                 ; AB1C  bc 40 06
    LDA #$36                            ; AB1F  a9 36
    JMP SpawnShot                       ; AB21  4c 2d 97
loc_15_AB24:
    RTS                                 ; AB24  60
sub_15_AB25:
    EOR #$FF                            ; AB25  49 ff
    CLC                                 ; AB27  18
    ADC #$01                            ; AB28  69 01
    CLC                                 ; AB2A  18
    ADC obj_act_timer,X                 ; AB2B  7d 40 06
    AND #$0F                            ; AB2E  29 0f
    TAY                                 ; AB30  a8
    LDA $AD86,Y                         ; AB31  b9 86 ad
    CLC                                 ; AB34  18
    ADC obj_act_timer,X                 ; AB35  7d 40 06
    AND #$0F                            ; AB38  29 0f
    STA obj_act_timer,X                 ; AB3A  9d 40 06
    RTS                                 ; AB3D  60
sub_15_AB3E:
    LDA obj_phase,X                     ; AB3E  bd 00 06
    BNE loc_15_AB96                     ; AB41  d0 53
    INC obj_phase,X                     ; AB43  fe 00 06
    JSR Rand64                          ; AB46  20 d7 97
    STA jmp_trampoline                  ; AB49  85 10
    LDA cur_bank16_x2                   ; AB4B  a5 6e
    PHA                                 ; AB4D  48
    JSR Rand64                          ; AB4E  20 d7 97
    JSR SetR6                           ; AB51  20 18 c4
    LDA jmp_trampoline                  ; AB54  a5 10
    ASL A                               ; AB56  0a
    ADC obj_x_lo,X                      ; AB57  75 70
    EOR obj_y_lo,X                      ; AB59  55 b0
    EOR tmp_08                          ; AB5B  45 08
    TAY                                 ; AB5D  a8
    LDA #$00                            ; AB5E  a9 00
    STA jmp_trampoline                  ; AB60  85 10
    LDA #$80                            ; AB62  a9 80
    STA tmp_11                          ; AB64  85 11
    LDA (jmp_trampoline),Y              ; AB66  b1 10
    STA tmp_30                          ; AB68  85 30
    INY                                 ; AB6A  c8
    LDA (jmp_trampoline),Y              ; AB6B  b1 10
    STA tmp_31                          ; AB6D  85 31
    PLA                                 ; AB6F  68
    JSR SetR6                           ; AB70  20 18 c4
    JSR ObjPosToTemp                    ; AB73  20 a8 98
    LDA tmp_30                          ; AB76  a5 30
    JSR sub_15_AB8A                     ; AB78  20 8a ab
    STA tmp_30                          ; AB7B  85 30
    LDA tmp_31                          ; AB7D  a5 31
    JSR sub_15_AB8A                     ; AB7F  20 8a ab
    STA tmp_31                          ; AB82  85 31
    JSR TempAddOffset                   ; AB84  20 24 98
    JMP TempToObjPos                    ; AB87  4c 97 98
sub_15_AB8A:
    STA jmp_trampoline                  ; AB8A  85 10
    ASL A                               ; AB8C  0a
    LDA jmp_trampoline                  ; AB8D  a5 10
    AND #$3F                            ; AB8F  29 3f
    BCC loc_15_AB95                     ; AB91  90 02
    ORA #$C0                            ; AB93  09 c0
loc_15_AB95:
    RTS                                 ; AB95  60
loc_15_AB96:
    CMP #$01                            ; AB96  c9 01
    BNE loc_15_ABAD                     ; AB98  d0 13
    DEC obj_anim,X                      ; AB9A  de e0 04
    BNE loc_15_ABAC                     ; AB9D  d0 0d
    LDA #$20                            ; AB9F  a9 20
    STA obj_anim,X                      ; ABA1  9d e0 04
    LDA #$9E                            ; ABA4  a9 9e
    STA obj_meta_id,X                   ; ABA6  9d 00 03
    INC obj_phase,X                     ; ABA9  fe 00 06
loc_15_ABAC:
    RTS                                 ; ABAC  60
loc_15_ABAD:
    DEC obj_y_lo,X                      ; ABAD  d6 b0
    DEC obj_anim,X                      ; ABAF  de e0 04
    BNE loc_15_ABAC                     ; ABB2  d0 f8
    LDA #$80                            ; ABB4  a9 80
    STA obj_anim,X                      ; ABB6  9d e0 04
    LDA #$10                            ; ABB9  a9 10
    STA obj_active,X                    ; ABBB  9d a0 04
    LDY #$00                            ; ABBE  a0 00
    JSR DirToTarget                     ; ABC0  20 09 84
    ASL tmp_20                          ; ABC3  06 20
    TXA                                 ; ABC5  8a
    CLC                                 ; ABC6  18
    ADC tmp_08                          ; ABC7  65 08
    AND #$03                            ; ABC9  29 03
    TAY                                 ; ABCB  a8
    LDA $ABD6,Y                         ; ABCC  b9 d6 ab
    CLC                                 ; ABCF  18
    ADC tmp_20                          ; ABD0  65 20
    STA obj_meta_dir,X                  ; ABD2  9d 60 03
    RTS                                 ; ABD5  60
    .db $00,$FF,$00,$01                 ; 15:ABD6
AiTalkEventE5:  ; area $43 type $70: talk event (NpcTalk p $C3; act 26 Ball of Water / act 27 throw out)
    LDA #$10                            ; ABDA  a9 10
    STA obj_freeze                      ; ABDC  8d a0 05
    LDA #$0F                            ; ABDF  a9 0f
    JMP BossPhaseDispatch               ; ABE1  4c 00 a4
sub_15_ABE4:
    LDA #$BA                            ; ABE4  a9 ba
    STA obj_overlay_id,X                ; ABE6  9d 80 05
    INC obj_anim,X                      ; ABE9  fe e0 04
    LDA obj_anim,X                      ; ABEC  bd e0 04
    CMP #$1F                            ; ABEF  c9 1f
    BNE loc_15_ABFB                     ; ABF1  d0 08
    LDA #$BB                            ; ABF3  a9 bb
    STA obj_meta_id,X                   ; ABF5  9d 00 03
    INC obj_phase,X                     ; ABF8  fe 00 06
loc_15_ABFB:
    RTS                                 ; ABFB  60
sub_15_ABFC:
    DEC obj_anim,X                      ; ABFC  de e0 04
    BNE loc_15_ABFB                     ; ABFF  d0 fa
    INC obj_phase,X                     ; AC01  fe 00 06
    LDA #$B0                            ; AC04  a9 b0
    STA obj_meta_id,X                   ; AC06  9d 00 03
    LDA #$4C                            ; AC09  a9 4c
    STA obj_overlay_id,X                ; AC0B  9d 80 05
    RTS                                 ; AC0E  60
sub_15_AC0F:
    LDA #$11                            ; AC0F  a9 11
    STA sub_state                       ; AC11  85 41
    STX obj_timer2_s03                  ; AC13  8e 23 06
    INC obj_phase,X                     ; AC16  fe 00 06
    RTS                                 ; AC19  60
sub_15_AC1A:
    DEC obj_anim,X                      ; AC1A  de e0 04
    RTS                                 ; AC1D  60
sub_15_AC1E:
    LDA #$BB                            ; AC1E  a9 bb
    STA obj_meta_id,X                   ; AC20  9d 00 03
    LDA #$BA                            ; AC23  a9 ba
    STA obj_overlay_id,X                ; AC25  9d 80 05
    LDA #$00                            ; AC28  a9 00
    STA obj_anim,X                      ; AC2A  9d e0 04
    INC obj_phase,X                     ; AC2D  fe 00 06
    RTS                                 ; AC30  60
sub_15_AC31:
    INC obj_anim,X                      ; AC31  fe e0 04
    LDA obj_anim,X                      ; AC34  bd e0 04
    CMP #$1F                            ; AC37  c9 1f
    BNE loc_15_ABFB                     ; AC39  d0 c0
    LDA #$00                            ; AC3B  a9 00
    STA obj_active,X                    ; AC3D  9d a0 04
    RTS                                 ; AC40  60
sub_15_AC41:
    LDA #$B1                            ; AC41  a9 b1
    STA obj_meta_id,X                   ; AC43  9d 00 03
    LDA #$4C                            ; AC46  a9 4c
    STA obj_overlay_id,X                ; AC48  9d 80 05
    LDA #$0E                            ; AC4B  a9 0e
    LDY #$04                            ; AC4D  a0 04
    JSR SpawnShot                       ; AC4F  20 2d 97
    LDA #$08                            ; AC52  a9 08
    STA obj_anim,X                      ; AC54  9d e0 04
    INC obj_phase,X                     ; AC57  fe 00 06
    RTS                                 ; AC5A  60
sub_15_AC5B:
    DEC obj_anim,X                      ; AC5B  de e0 04
    BNE loc_15_ABFB                     ; AC5E  d0 9b
    LDA obj_y_lo                        ; AC60  a5 b0
    CLC                                 ; AC62  18
    ADC #$08                            ; AC63  69 08
    STA obj_y_lo                        ; AC65  85 b0
    STA obj_y_lo_s01                    ; AC67  85 b1
    INC obj_anim,X                      ; AC69  fe e0 04
    CMP #$B0                            ; AC6C  c9 b0
    BCC loc_15_ABFB                     ; AC6E  90 8b
    LDA #$FF                            ; AC70  a9 ff
    STA obj_act_timer                   ; AC72  8d 40 06
    LDA #$04                            ; AC75  a9 04
    STA obj_phase,X                     ; AC77  9d 00 06
    RTS                                 ; AC7A  60
BossAiE6:  ; boss AI $E6 (type $90) phase base $17 x2
    LDA obj_phase,X                     ; AC7B  bd 00 06
    BEQ loc_15_AC85                     ; AC7E  f0 05
    LDA #$04                            ; AC80  a9 04
    STA boss_cam_lock                   ; AC82  8d d7 07
loc_15_AC85:
    LDA #$C0                            ; AC85  a9 c0
    JSR sub_26_9B07                     ; AC87  20 07 9b
    LDA #$17                            ; AC8A  a9 17
    JMP BossPhaseDispatch               ; AC8C  4c 00 a4
sub_15_AC8F:
    LDA obj_flags,X                     ; AC8F  bd 80 03
    BMI loc_15_ACF4                     ; AC92  30 60
    LDA #$FF                            ; AC94  a9 ff
    STA boss_cam_lock                   ; AC96  8d d7 07
    LDA #$10                            ; AC99  a9 10
    STA obj_freeze                      ; AC9B  8d a0 05
    JSR ClearEnemySlots                 ; AC9E  20 29 bb
    LDA #$75                            ; ACA1  a9 75
    STA chr_r4_play                     ; ACA3  8d f4 07
    LDA ram_07D5                        ; ACA6  ad d5 07
    BEQ loc_15_ACF4                     ; ACA9  f0 49
    LDA #$00                            ; ACAB  a9 00
    STA obj_freeze                      ; ACAD  8d a0 05
    LDA #$12                            ; ACB0  a9 12
    JSR PlaySound                       ; ACB2  20 25 c1
    INC obj_timer2,X                    ; ACB5  fe 20 06
    LDA #$00                            ; ACB8  a9 00
    STA obj_freeze                      ; ACBA  8d a0 05
    LDA obj_timer2,X                    ; ACBD  bd 20 06
    CMP #$40                            ; ACC0  c9 40
    BNE loc_15_ACF4                     ; ACC2  d0 30
    INC obj_phase,X                     ; ACC4  fe 00 06
    LDA #$0B                            ; ACC7  a9 0b
    STA obj_flags,X                     ; ACC9  9d 80 03
    LDA #$30                            ; ACCC  a9 30
    STA obj_coll,X                      ; ACCE  9d a0 03
    LDA #$4C                            ; ACD1  a9 4c
    STA obj_overlay_id,X                ; ACD3  9d 80 05
    LDA #$08                            ; ACD6  a9 08
    STA obj_meta_dir,X                  ; ACD8  9d 60 03
    LDA #$50                            ; ACDB  a9 50
    STA obj_timer2,X                    ; ACDD  9d 20 06
    LDA area_id                         ; ACE0  a5 6c
    CMP #$AC                            ; ACE2  c9 ac
    BNE loc_15_ACF4                     ; ACE4  d0 0e
    LDA #$18                            ; ACE6  a9 18
    STA pending_msg_group               ; ACE8  8d e3 06
    LDA #$02                            ; ACEB  a9 02
    STA pending_msg_id                  ; ACED  8d c3 06
    LDA #$10                            ; ACF0  a9 10
    STA sub_state                       ; ACF2  85 41
loc_15_ACF4:
    RTS                                 ; ACF4  60
sub_15_ACF5:
    LDA tmp_08                          ; ACF5  a5 08
    AND #$3F                            ; ACF7  29 3f
    BNE loc_15_AD08                     ; ACF9  d0 0d
    LDA area_id                         ; ACFB  a5 6c
    CMP #$AC                            ; ACFD  c9 ac
    BNE loc_15_AD08                     ; ACFF  d0 07
    LDA #$35                            ; AD01  a9 35
    LDY #$00                            ; AD03  a0 00
    JSR SpawnShot                       ; AD05  20 2d 97
loc_15_AD08:
    LDA obj_timer2,X                    ; AD08  bd 20 06
    BEQ loc_15_AD10                     ; AD0B  f0 03
    JMP loc_15_AD96                     ; AD0D  4c 96 ad
loc_15_AD10:
    DEC obj_anim,X                      ; AD10  de e0 04
    LDA obj_anim,X                      ; AD13  bd e0 04
    AND #$07                            ; AD16  29 07
    BNE loc_15_AD5E                     ; AD18  d0 44
    LDA obj_y_lo                        ; AD1A  a5 b0
    PHA                                 ; AD1C  48
    LDA tmp_08                          ; AD1D  a5 08
    AND #$80                            ; AD1F  29 80
    LSR A                               ; AD21  4a
    ORA #$30                            ; AD22  09 30
    STA obj_y_lo                        ; AD24  85 b0
    LDY #$00                            ; AD26  a0 00
    JSR DirToTarget                     ; AD28  20 09 84
    PLA                                 ; AD2B  68
    STA obj_y_lo                        ; AD2C  85 b0
    LDA tmp_20                          ; AD2E  a5 20
    ASL A                               ; AD30  0a
    EOR #$FF                            ; AD31  49 ff
    CLC                                 ; AD33  18
    ADC #$01                            ; AD34  69 01
    CLC                                 ; AD36  18
    ADC obj_meta_dir,X                  ; AD37  7d 60 03
    AND #$0F                            ; AD3A  29 0f
    TAY                                 ; AD3C  a8
    LDA $AD86,Y                         ; AD3D  b9 86 ad
    CLC                                 ; AD40  18
    ADC obj_meta_dir,X                  ; AD41  7d 60 03
    AND #$0F                            ; AD44  29 0f
    STA obj_meta_dir,X                  ; AD46  9d 60 03
    JSR Rand64                          ; AD49  20 d7 97
    BNE loc_15_AD5E                     ; AD4C  d0 10
    LDY #$01                            ; AD4E  a0 01
    JSR DirToTarget                     ; AD50  20 09 84
    LDA #$50                            ; AD53  a9 50
    STA obj_timer2,X                    ; AD55  9d 20 06
    LDA tmp_20                          ; AD58  a5 20
    ASL A                               ; AD5A  0a
    STA obj_meta_dir,X                  ; AD5B  9d 60 03
loc_15_AD5E:
    LDA player_hp_max,X                 ; AD5E  bd c0 03
    LSR A                               ; AD61  4a
    LSR A                               ; AD62  4a
    LSR A                               ; AD63  4a
    LSR A                               ; AD64  4a
    CMP #$03                            ; AD65  c9 03
    BCS loc_15_AD70                     ; AD67  b0 07
    TAY                                 ; AD69  a8
    LDA $AD82,Y                         ; AD6A  b9 82 ad
    STA obj_flags2,X                    ; AD6D  9d 40 03
loc_15_AD70:
    DEC obj_timer,X                     ; AD70  de 80 04
    JSR ObjPosToTemp                    ; AD73  20 a8 98
    LDA obj_meta_dir,X                  ; AD76  bd 60 03
    JSR ObjStepVector                   ; AD79  20 80 84
    JSR TempAddStep                     ; AD7C  20 61 98
    JMP TempToObjPos                    ; AD7F  4c 97 98
    .db $0E,$0D,$0F,$0C,$00,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$01,$01,$01,$01; 15:AD82
    .db $01,$01,$01,$01                 ; 15:AD92
loc_15_AD96:
    LDA #$DD                            ; AD96  a9 dd
    STA obj_overlay_id,X                ; AD98  9d 80 05
    LDA #$DC                            ; AD9B  a9 dc
    STA obj_meta_id,X                   ; AD9D  9d 00 03
    DEC obj_timer2,X                    ; ADA0  de 20 06
    BNE loc_15_ADAF                     ; ADA3  d0 0a
    LDA #$DB                            ; ADA5  a9 db
    STA obj_meta_id,X                   ; ADA7  9d 00 03
    LDA #$4C                            ; ADAA  a9 4c
    STA obj_overlay_id,X                ; ADAC  9d 80 05
loc_15_ADAF:
    INC obj_anim,X                      ; ADAF  fe e0 04
    INC obj_anim,X                      ; ADB2  fe e0 04
    LDA obj_timer2,X                    ; ADB5  bd 20 06
    AND #$07                            ; ADB8  29 07
    BEQ loc_15_ADBD                     ; ADBA  f0 01
    RTS                                 ; ADBC  60
loc_15_ADBD:
    LDA obj_timer2,X                    ; ADBD  bd 20 06
    LSR A                               ; ADC0  4a
    LSR A                               ; ADC1  4a
    LSR A                               ; ADC2  4a
    LSR A                               ; ADC3  4a
    SEC                                 ; ADC4  38
    SBC #$02                            ; ADC5  e9 02
    CLC                                 ; ADC7  18
    ADC obj_meta_dir,X                  ; ADC8  7d 60 03
    AND #$0F                            ; ADCB  29 0f
    TAY                                 ; ADCD  a8
    LDA obj_shot_id,X                   ; ADCE  bd 40 04
    JMP SpawnShot                       ; ADD1  4c 2d 97
AiBoss88:  ; AI $E7 (type $88): $07D7=4 when phase!=0; 26:9B07 A=$C0; BossPhaseDispatch base $1B
    LDA obj_phase,X                     ; ADD4  bd 00 06
    BEQ loc_15_ADDE                     ; ADD7  f0 05
    LDA #$04                            ; ADD9  a9 04
    STA boss_cam_lock                   ; ADDB  8d d7 07
loc_15_ADDE:
    LDA #$C0                            ; ADDE  a9 c0
    JSR sub_26_9B07                     ; ADE0  20 07 9b
    LDA #$1B                            ; ADE3  a9 1b
    JMP BossPhaseDispatch               ; ADE5  4c 00 a4
    .db $BD,$80,$03,$30,$47,$20,$29,$BB,$A9,$76,$8D,$F4,$07,$A9,$1E,$8D; 15:ADE8
    .db $E6,$07,$A9,$10,$8D,$A0,$05,$A9,$FF,$8D,$D7,$07,$AD,$D5,$07,$F0; 15:ADF8
    .db $2B,$A9,$00,$8D,$A0,$05,$A9,$12,$20,$25,$C1,$FE,$00,$06,$A9,$30; 15:AE08
    .db $9D,$A0,$03,$A9,$09,$9D,$80,$03,$A5,$6C,$C9,$B9,$D0,$0E,$A9,$18; 15:AE18
    .db $8D,$E3,$06,$A9,$04,$8D,$C3,$06,$A9,$10,$85,$41,$60,$FE,$00,$06; 15:AE28
    .db $A9,$D8,$9D,$00,$03,$A9,$0F,$9D,$E0,$04,$60,$DE,$E0,$04,$D0,$EC; 15:AE38
    .db $FE,$00,$06,$A9,$D7,$9D,$00,$03,$BD,$C0,$03,$18,$69,$20,$9D,$40; 15:AE48
    .db $06,$60,$DE,$80,$04,$DE,$E0,$04,$DE,$E0,$04,$20,$BA,$AE,$A0,$00; 15:AE58
    .db $B5,$B0,$C9,$15,$90,$14,$A0,$10,$C9,$D0,$B0,$0E,$A0,$18,$B5,$70; 15:AE68
    .db $C9,$20,$90,$06,$A0,$08,$C9,$E0,$90,$1C,$84,$10,$A9,$3D,$20,$25; 15:AE78
    .db $C1,$A9,$08,$8D,$D6,$07,$20,$D7,$97,$18,$65,$10,$A8,$B9,$28,$AF; 15:AE88
    .db $9D,$60,$03,$20,$BA,$AE,$DE,$20,$06,$BD,$20,$06,$4A,$90,$12,$DE; 15:AE98
    .db $40,$06,$D0,$0D,$FE,$00,$06,$A9,$D8,$9D,$00,$03,$A9,$00,$9D,$E0; 15:AEA8
    .db $04,$60,$20,$A8,$98,$BD,$60,$03,$20,$80,$84,$20,$61,$98,$4C,$97; 15:AEB8
    .db $98,$FE,$E0,$04,$BD,$E0,$04,$C9,$0F,$D0,$E6,$FE,$00,$06,$A9,$28; 15:AEC8
    .db $9D,$E0,$04,$A0,$01,$20,$09,$84,$A5,$20,$0A,$9D,$40,$06,$A9,$D9; 15:AED8
    .db $9D,$00,$03,$60,$BD,$E0,$04,$F0,$31,$DE,$E0,$04,$A9,$03,$A4,$6C; 15:AEE8
    .db $C0,$B9,$F0,$02,$A9,$07,$3D,$E0,$04,$D0,$24,$4A,$4A,$4A,$18,$7D; 15:AEF8
    .db $40,$06,$38,$E9,$02,$29,$0F,$85,$1F,$4A,$A8,$A9,$5C,$20,$2D,$97; 15:AF08
    .db $90,$0D,$A4,$10,$A5,$1F,$99,$60,$03,$60,$A9,$01,$9D,$00,$06,$60; 15:AF18
    .db $05,$06,$07,$08,$09,$0A,$0B,$08,$0F,$0E,$0D,$0C,$0B,$0A,$09,$0C; 15:AF28
    .db $0D,$0E,$0F,$00,$01,$02,$03,$00,$01,$02,$03,$04,$05,$06,$07,$04; 15:AF38
BossAiE8:  ; boss AI $E8 (type $97) phase base $21 x3
    LDA obj_phase,X                     ; AF48  bd 00 06
    BEQ loc_15_AF52                     ; AF4B  f0 05
    LDA #$04                            ; AF4D  a9 04
    STA boss_cam_lock                   ; AF4F  8d d7 07
loc_15_AF52:
    LDA #$C0                            ; AF52  a9 c0
    JSR sub_26_9B07                     ; AF54  20 07 9b
    LDA #$21                            ; AF57  a9 21
    JMP BossPhaseDispatch               ; AF59  4c 00 a4
    .db $BD,$80,$03,$30,$4B,$A9,$FF,$8D,$D7,$07,$A9,$1F,$8D,$E7,$07,$A9; 15:AF5C
    .db $74,$8D,$F4,$07,$A9,$10,$8D,$A0,$05,$20,$29,$BB,$AD,$D5,$07,$F0; 15:AF6C
    .db $2F,$A9,$00,$8D,$A0,$05,$A9,$12,$20,$25,$C1,$FE,$00,$06,$A9,$18; 15:AF7C
    .db $8D,$E3,$06,$A9,$08,$8D,$C3,$06,$A9,$10,$85,$41,$A9,$0D,$9D,$40; 15:AF8C
    .db $03,$A9,$30,$9D,$A0,$03,$A9,$2B,$9D,$80,$03,$A9,$4C,$9D,$80,$05; 15:AF9C
    .db $60,$A9,$4C,$9D,$80,$05,$A9,$DB,$9D,$00,$03,$AD,$01,$03,$0A,$0A; 15:AFAC
    .db $29,$06,$9D,$60,$03,$BD,$20,$06,$0A,$A8,$B9,$2B,$B0,$C9,$FF,$D0; 15:AFBC
    .db $0F,$FE,$00,$06,$A9,$50,$9D,$40,$06,$20,$D7,$97,$9D,$60,$06,$60; 15:AFCC
    .db $C9,$FE,$D0,$0C,$A9,$00,$9D,$E0,$04,$9D,$20,$06,$A8,$B9,$2B,$B0; 15:AFDC
    .db $0A,$0A,$0A,$0A,$85,$34,$A9,$00,$85,$35,$B9,$2C,$B0,$0A,$0A,$0A; 15:AFEC
    .db $0A,$85,$36,$B9,$2C,$B0,$4A,$4A,$4A,$4A,$85,$37,$86,$1F,$A2,$1F; 15:AFFC
    .db $20,$97,$98,$A6,$1F,$20,$02,$A6,$A0,$1F,$20,$09,$84,$DE,$E0,$04; 15:B00C
    .db $F0,$04,$A5,$21,$D0,$08,$FE,$20,$06,$A9,$00,$9D,$E0,$04,$60,$07; 15:B01C
    .db $24,$FF,$FF,$02,$28,$0A,$24,$FF,$FF,$02,$23,$FF,$FF,$0E,$26,$02; 15:B02C
    .db $29,$0E,$2C,$0D,$24,$02,$27,$06,$22,$FF,$FF,$0D,$26,$FF,$FF,$0D; 15:B03C
    .db $28,$FF,$FF,$FE,$FF,$BD,$60,$06,$C9,$02,$90,$32,$20,$A5,$B0,$DE; 15:B04C
    .db $40,$06,$F0,$17,$BD,$40,$06,$29,$0F,$D0,$22,$BD,$40,$06,$4A,$4A; 15:B05C
    .db $18,$69,$02,$29,$0F,$A8,$A9,$33,$4C,$2D,$97,$FE,$20,$06,$A9,$01; 15:B06C
    .db $9D,$00,$06,$A9,$08,$9D,$60,$03,$A9,$00,$9D,$E0,$04,$60,$20,$A5; 15:B07C
    .db $B0,$DE,$40,$06,$F0,$E5,$BD,$40,$06,$C9,$20,$D0,$F0,$A0,$01,$20; 15:B08C
    .db $09,$84,$A4,$20,$A9,$46,$4C,$2D,$97,$A9,$F1,$9D,$80,$05,$A9,$DE; 15:B09C
    .db $9D,$00,$03,$DE,$E0,$04,$DE,$E0,$04,$60; 15:B0AC
BossAiE9:  ; boss AI $E9 (statues $9C/$9D) phase base $24 x3
    LDA obj_phase,X                     ; B0B6  bd 00 06
    BEQ loc_15_B0C0                     ; B0B9  f0 05
    LDA #$7F                            ; B0BB  a9 7f
    STA boss_cam_lock                   ; B0BD  8d d7 07
loc_15_B0C0:
    LDA #$24                            ; B0C0  a9 24
    JMP BossPhaseDispatch               ; B0C2  4c 00 a4
    .db $BD,$80,$03,$30,$29,$A9,$FF,$8D,$D7,$07,$A9,$10,$8D,$A0,$05,$AD; 15:B0C5
    .db $D5,$07,$F0,$1A,$A9,$00,$8D,$A0,$05,$FE,$00,$06,$E0,$0D,$D0,$0E; 15:B0D5
    .db $A9,$1B,$8D,$E3,$06,$A9,$03,$8D,$C3,$06,$A9,$10,$85,$41,$60,$BD; 15:B0E5
    .db $E0,$04,$D0,$0C,$B5,$70,$45,$71,$30,$05,$A9,$24,$9D,$E0,$04,$60; 15:B0F5
    .db $BD,$E0,$04,$38,$E9,$04,$9D,$E0,$04,$C9,$20,$D0,$F2,$A0,$01,$20; 15:B105
    .db $09,$84,$06,$20,$20,$D7,$97,$29,$03,$A8,$B9,$2C,$B1,$18,$65,$20; 15:B115
    .db $A8,$BD,$40,$04,$4C,$2D,$97,$00,$00,$01,$FF; 15:B125
BossAiEA:  ; boss AI $EA (type $9B) phase base $27 x4
    LDA #$C0                            ; B130  a9 c0
    JSR sub_26_9B07                     ; B132  20 07 9b
    LDY area_id                         ; B135  a4 6c
    CPY #$A6                            ; B137  c0 a6
    BNE loc_15_B140                     ; B139  d0 05
    LDA #$FF                            ; B13B  a9 ff
    STA player_hp_max,X                 ; B13D  9d c0 03
loc_15_B140:
    LDA obj_phase,X                     ; B140  bd 00 06
    BEQ loc_15_B14A                     ; B143  f0 05
    LDA #$10                            ; B145  a9 10
    STA boss_cam_lock                   ; B147  8d d7 07
loc_15_B14A:
    LDA #$27                            ; B14A  a9 27
    JMP BossPhaseDispatch               ; B14C  4c 00 a4
    .db $BD,$80,$03,$30,$50,$A9,$10,$8D,$A0,$05,$A9,$79,$8D,$F4,$07,$A9; 15:B14F
    .db $41,$8D,$E6,$07,$A9,$40,$8D,$E7,$07,$A9,$FF,$8D,$D7,$07,$AD,$D5; 15:B15F
    .db $07,$F0,$32,$A9,$0B,$9D,$80,$03,$A9,$4C,$9D,$80,$05,$A9,$30,$9D; 15:B16F
    .db $A0,$03,$A9,$00,$8D,$A0,$05,$A9,$12,$20,$25,$C1,$A9,$04,$A4,$6C; 15:B17F
    .db $C0,$A6,$F0,$02,$A9,$00,$8D,$C3,$06,$A9,$1B,$8D,$E3,$06,$A9,$10; 15:B18F
    .db $85,$41,$FE,$00,$06,$60,$BD,$20,$06,$F0,$03,$4C,$78,$B2,$A9,$DB; 15:B19F
    .db $9D,$00,$03,$A9,$4C,$9D,$80,$05,$A9,$04,$9D,$40,$03,$DE,$E0,$04; 15:B1AF
    .db $BD,$E0,$04,$C9,$C0,$B0,$0F,$29,$1F,$D0,$1C,$20,$D7,$97,$4A,$90; 15:B1BF
    .db $05,$A9,$40,$9D,$20,$06,$B5,$70,$C5,$70,$B5,$90,$E5,$90,$A9,$02; 15:B1CF
    .db $90,$02,$A9,$06,$9D,$60,$03,$38,$A5,$70,$F5,$70,$85,$10,$A5,$90; 15:B1DF
    .db $F5,$90,$0A,$A5,$10,$90,$05,$49,$FF,$38,$E9,$01,$C9,$08,$B0,$04; 15:B1EF
    .db $FE,$00,$06,$60,$DE,$80,$04,$20,$A8,$98,$BD,$60,$03,$20,$80,$84; 15:B1FF
    .db $20,$61,$98,$4C,$97,$98,$B5,$B0,$C9,$D0,$90,$04,$FE,$00,$06,$60; 15:B20F
    .db $A9,$08,$9D,$40,$03,$A9,$04,$9D,$60,$03,$4C,$03,$B2,$DE,$E0,$04; 15:B21F
    .db $A9,$E0,$9D,$00,$03,$A9,$E1,$9D,$80,$05,$BD,$20,$06,$D0,$3A,$B5; 15:B22F
    .db $B0,$C9,$A0,$B0,$27,$38,$E9,$20,$29,$3F,$C9,$20,$D0,$05,$A9,$40; 15:B23F
    .db $9D,$20,$06,$B5,$B0,$C9,$30,$B0,$13,$A9,$01,$9D,$00,$06,$20,$D7; 15:B24F
    .db $97,$4A,$A9,$02,$90,$02,$A9,$06,$9D,$60,$03,$60,$A9,$02,$9D,$40; 15:B25F
    .db $03,$A9,$00,$9D,$60,$03,$4C,$03,$B2,$DE,$20,$06,$BD,$20,$06,$C9; 15:B26F
    .db $20,$90,$0E,$A9,$E0,$9D,$00,$03,$A9,$E1,$9D,$80,$05,$DE,$E0,$04; 15:B27F
    .db $60,$A9,$E2,$9D,$00,$03,$A9,$F2,$9D,$80,$05,$BD,$E0,$04,$38,$E9; 15:B28F
    .db $04,$9D,$E0,$04,$A5,$08,$29,$07,$D0,$28,$A9,$03,$85,$1E,$A5,$1E; 15:B29F
    .db $0A,$A8,$B9,$D3,$B2,$85,$1F,$B9,$D2,$B2,$A8,$BD,$40,$04,$20,$2D; 15:B2AF
    .db $97,$90,$0F,$A4,$10,$B9,$B0,$00,$18,$65,$1F,$99,$B0,$00,$C6,$1E; 15:B2BF
    .db $10,$DC,$60,$06,$00,$07,$10,$09,$10,$0A,$00; 15:B2CF
AiBowOfTruth9E:  ; AI $EB (type $9E): 26:9B07 A=$C7; $07D7=$10 + IRQ mode 7 when phase!=0; BossPhaseDispatch base $2B
    LDA #$C7                            ; B2DA  a9 c7
    JSR sub_26_9B07                     ; B2DC  20 07 9b
    LDA obj_phase,X                     ; B2DF  bd 00 06
    BEQ loc_15_B2F3                     ; B2E2  f0 0f
    LDA #$10                            ; B2E4  a9 10
    STA boss_cam_lock                   ; B2E6  8d d7 07
    LDA irq_mode                        ; B2E9  a5 51
    CMP #$02                            ; B2EB  c9 02
    BEQ loc_15_B2F3                     ; B2ED  f0 04
    LDA #$07                            ; B2EF  a9 07
    STA irq_mode                        ; B2F1  85 51
loc_15_B2F3:
    LDA #$2B                            ; B2F3  a9 2b
    JMP BossPhaseDispatch               ; B2F5  4c 00 a4
    .db $A2,$00,$9D,$00,$60,$CA,$D0,$FA,$60,$86,$1F,$20,$8C,$FF,$A9,$38; 15:B2F8
    .db $8D,$E8,$07,$A9,$10,$20,$3D,$8C,$A9,$0C,$20,$25,$C1,$A9,$2E,$8D; 15:B308
    .db $F0,$07,$8D,$F1,$07,$A9,$7A,$8D,$F4,$07,$A9,$75,$8D,$F5,$07,$A9; 15:B318
    .db $A0,$8D,$E0,$07,$A9,$A1,$8D,$E1,$07,$A9,$07,$8D,$E1,$07,$A9,$43; 15:B328
    .db $8D,$E6,$07,$A9,$42,$8D,$E7,$07,$20,$76,$C6,$A9,$FF,$20,$F8,$B2; 15:B338
    .db $A2,$39,$8A,$20,$82,$C4,$A9,$00,$85,$09,$E8,$E0,$3C,$D0,$F3,$A2; 15:B348
    .db $3F,$A9,$00,$9D,$00,$61,$9D,$C0,$60,$CA,$10,$F5,$A9,$44,$8D,$D3; 15:B358
    .db $60,$8D,$13,$61,$A9,$3C,$20,$82,$C4,$A9,$04,$85,$1D,$A5,$1D,$20; 15:B368
    .db $C1,$B6,$C6,$1D,$10,$F7,$A0,$26,$20,$3C,$A7,$A6,$1F,$A9,$78,$95; 15:B378
    .db $70,$A9,$58,$95,$B0,$A9,$4C,$9D,$00,$03,$A9,$02,$9D,$60,$03,$FE; 15:B388
    .db $00,$06,$20,$8C,$FF,$A9,$62,$8D,$B0,$03,$A9,$40,$8D,$30,$04,$A9; 15:B398
    .db $63,$8D,$B1,$03,$A9,$40,$8D,$31,$04,$A9,$10,$20,$49,$8C,$A9,$00; 15:B3A8
    .db $8D,$E8,$07,$60,$A9,$01,$9D,$40,$03,$DE,$80,$04,$BD,$20,$06,$D0; 15:B3B8
    .db $5C,$DE,$E0,$04,$BD,$E0,$04,$29,$1F,$D0,$24,$20,$D7,$97,$4A,$90; 15:B3C8
    .db $1E,$A9,$C0,$9D,$20,$06,$24,$08,$30,$15,$A5,$71,$45,$B1,$4A,$B0; 15:B3D8
    .db $03,$4C,$41,$B5,$A9,$06,$9D,$00,$06,$A9,$80,$9D,$20,$06,$60,$A9; 15:B3E8
    .db $00,$8D,$EE,$04,$20,$1D,$B7,$BD,$60,$03,$B4,$70,$C0,$40,$B0,$02; 15:B3F8
    .db $A9,$02,$C0,$C0,$90,$02,$A9,$06,$9D,$60,$03,$CE,$EF,$04,$CE,$F2; 15:B408
    .db $04,$20,$FF,$A8,$BD,$E0,$06,$F0,$03,$4C,$C8,$B4,$60,$BD,$E0,$06; 15:B418
    .db $C9,$09,$F0,$03,$20,$C8,$B4,$DE,$20,$06,$F0,$5F,$BD,$20,$06,$29; 15:B428
    .db $07,$D0,$4F,$BD,$20,$06,$29,$18,$4A,$4A,$A8,$B9,$8B,$B4,$8D,$10; 15:B438
    .db $03,$B9,$8C,$B4,$8D,$11,$03,$BD,$20,$06,$29,$08,$D0,$34,$20,$D7; 15:B448
    .db $97,$18,$69,$04,$29,$0F,$A8,$A9,$2E,$20,$2D,$97,$90,$24,$A4,$10; 15:B458
    .db $B9,$B0,$00,$38,$E9,$1C,$99,$B0,$00,$BD,$20,$06,$29,$10,$85,$10; 15:B468
    .db $B9,$70,$00,$38,$E9,$18,$C6,$10,$30,$02,$69,$30,$99,$70,$00,$DE; 15:B478
    .db $E0,$04,$60,$E9,$E6,$E9,$E7,$E8,$E7,$E9,$E7,$20,$1D,$B7,$20,$D7; 15:B488
    .db $97,$4A,$90,$1E,$A9,$02,$9D,$00,$06,$20,$D7,$97,$A8,$B9,$BB,$B4; 15:B498
    .db $9D,$60,$03,$B5,$70,$18,$65,$71,$4A,$29,$03,$A8,$B9,$C4,$B4,$9D; 15:B4A8
    .db $40,$03,$60,$05,$05,$06,$07,$08,$09,$0A,$0B,$0B,$0D,$0E,$0F,$0C; 15:B4B8
    .db $BD,$E0,$06,$20,$C1,$B6,$BC,$E0,$06,$C8,$C0,$0A,$D0,$02,$A0,$00; 15:B4C8
    .db $98,$9D,$E0,$06,$60,$CE,$F2,$04,$DE,$80,$04,$BD,$C0,$03,$10,$03; 15:B4D8
    .db $20,$C8,$B4,$20,$A3,$B6,$B5,$B0,$C9,$C0,$B0,$03,$4C,$FF,$A8,$A9; 15:B4E8
    .db $20,$9D,$20,$06,$20,$D7,$97,$A8,$B9,$1C,$B5,$9D,$60,$03,$A9,$04; 15:B4F8
    .db $9D,$00,$06,$60,$00,$0F,$0E,$0D,$0C,$0B,$0A,$09,$08,$07,$06,$05; 15:B508
    .db $04,$03,$02,$01,$00,$0D,$0E,$0F,$00,$01,$02,$03,$CE,$F2,$04,$DE; 15:B518
    .db $80,$04,$BD,$C0,$03,$10,$03,$20,$C8,$B4,$20,$A3,$B6,$B5,$B0,$C9; 15:B528
    .db $50,$90,$03,$4C,$FF,$A8,$9D,$E0,$04,$A9,$05,$9D,$00,$06,$A0,$01; 15:B538
    .db $20,$09,$84,$A5,$20,$0A,$9D,$40,$06,$60,$BD,$E0,$06,$C9,$09,$F0; 15:B548
    .db $03,$20,$C8,$B4,$DE,$20,$06,$BD,$20,$06,$C9,$10,$D0,$0C,$A0,$01; 15:B558
    .db $20,$09,$84,$A4,$20,$A9,$46,$4C,$2D,$97,$A9,$08,$8D,$EE,$04,$BD; 15:B568
    .db $20,$06,$D0,$0A,$A9,$00,$8D,$EE,$04,$A9,$03,$9D,$00,$06,$60,$A9; 15:B578
    .db $00,$20,$18,$B6,$BD,$E0,$06,$F0,$03,$20,$C8,$B4,$DE,$20,$06,$D0; 15:B588
    .db $17,$20,$D7,$97,$4A,$A9,$02,$90,$02,$A9,$06,$9D,$60,$03,$A9,$01; 15:B598
    .db $9D,$00,$06,$A9,$0F,$4C,$18,$B6,$A9,$30,$85,$2E,$A9,$08,$85,$2D; 15:B5A8
    .db $20,$23,$B6,$BD,$20,$06,$29,$0F,$D0,$36,$20,$76,$C6,$BD,$20,$06; 15:B5B8
    .db $4A,$4A,$4A,$4A,$A8,$B9,$F9,$B5,$A8,$B9,$09,$B6,$8D,$0F,$03,$98; 15:B5C8
    .db $0A,$0A,$A8,$B9,$0C,$B6,$8D,$00,$60,$B9,$0D,$B6,$8D,$01,$60,$B9; 15:B5D8
    .db $0E,$B6,$8D,$02,$60,$B9,$0F,$B6,$8D,$03,$60,$A9,$3D,$4C,$82,$C4; 15:B5E8
    .db $60,$00,$01,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02; 15:B5F8
    .db $01,$4C,$4C,$E5,$45,$46,$47,$48,$49,$4A,$4B,$4C,$4D,$4E,$4F,$50; 15:B608
    .db $A0,$13,$99,$00,$05,$88,$C0,$0C,$D0,$F8,$60,$A5,$08,$29,$03,$D0; 15:B618
    .db $18,$A9,$00,$85,$2F,$A5,$08,$4A,$4A,$4A,$4A,$4A,$48,$20,$42,$B6; 15:B628
    .db $68,$18,$69,$04,$C6,$2F,$4C,$42,$B6,$60,$29,$07,$A8,$B9,$72,$B6; 15:B638
    .db $18,$7D,$40,$06,$29,$0F,$A8,$A5,$2E,$20,$2D,$97,$90,$EB,$A4,$10; 15:B648
    .db $B9,$70,$00,$38,$E9,$12,$24,$2F,$10,$03,$18,$69,$24,$99,$70,$00; 15:B658
    .db $B9,$B0,$00,$18,$65,$2D,$99,$B0,$00,$60,$01,$02,$01,$00,$FF,$FE; 15:B668
    .db $FF,$00,$A9,$08,$8D,$EE,$04,$DE,$20,$06,$D0,$03,$4C,$99,$B5,$BD; 15:B678
    .db $20,$06,$29,$0F,$D0,$14,$A9,$2F,$A0,$04,$20,$2D,$97,$90,$0B,$A4; 15:B688
    .db $10,$B9,$B0,$00,$38,$E9,$10,$99,$B0,$00,$60,$BD,$60,$03,$29,$08; 15:B698
    .db $F0,$07,$B5,$70,$C9,$38,$90,$07,$60,$B5,$70,$C9,$D8,$90,$09,$BC; 15:B6A8
    .db $60,$03,$B9,$0C,$B5,$9D,$60,$03,$60; 15:B6B8
BossBgStrip:  ; s72: A=n 0-9 -> BossBgStripPtrs; SetR6(7); copy $C0 B of bank-7 $xxF0 rows (14 kept/page) to $6000; PpuQueueBlock($2F+n) (14x3 strip); callers 15:B377 15:B4CB (AI $EB phases)
    STA tmp_2F                          ; B6C1  85 2f
    JSR WaitPpuQueue                    ; B6C3  20 76 c6
    STX tmp_2E                          ; B6C6  86 2e
    LDA tmp_2F                          ; B6C8  a5 2f
    ASL A                               ; B6CA  0a
    TAY                                 ; B6CB  a8
    LDA BossBgStripPtrs,Y               ; B6CC  b9 09 b7
    STA jmp_trampoline                  ; B6CF  85 10
    LDA $B70A,Y                         ; B6D1  b9 0a b7
    STA tmp_11                          ; B6D4  85 11
    LDA #$C0                            ; B6D6  a9 c0
    STA tmp_12                          ; B6D8  85 12
    LDA #$07                            ; B6DA  a9 07
    JSR SetR6                           ; B6DC  20 18 c4
    LDX #$00                            ; B6DF  a2 00
    LDY #$00                            ; B6E1  a0 00
loc_15_B6E3:
    LDA (jmp_trampoline),Y              ; B6E3  b1 10
    STA wram_buf,X                      ; B6E5  9d 00 60
    INX                                 ; B6E8  e8
    INC jmp_trampoline                  ; B6E9  e6 10
    BNE loc_15_B6F5                     ; B6EB  d0 08
    LDA #$F0                            ; B6ED  a9 f0
    STA jmp_trampoline                  ; B6EF  85 10
    INC tmp_11                          ; B6F1  e6 11
    DEX                                 ; B6F3  ca
    DEX                                 ; B6F4  ca
loc_15_B6F5:
    DEC tmp_12                          ; B6F5  c6 12
    BNE loc_15_B6E3                     ; B6F7  d0 ea
    LDA #$1A                            ; B6F9  a9 1a
    JSR SetR6                           ; B6FB  20 18 c4
    LDA tmp_2F                          ; B6FE  a5 2f
    CLC                                 ; B700  18
    ADC #$2F                            ; B701  69 2f
    JSR PpuQueueBlock                   ; B703  20 82 c4
    LDX tmp_2E                          ; B706  a6 2e
    RTS                                 ; B708  60
BossBgStripPtrs:  ; s72: .dw[10] bank-7 $80F0+n*$300; n 0-4 / 5-9 = two 14x15-tile BG pictures (5 strips each; blocks $2F-$33 / $34-$38)
    .db $F0,$80,$F0,$83,$F0,$86,$F0,$89,$F0,$8C,$F0,$90,$F0,$93,$F0,$96; 15:B709
    .db $F0,$99,$F0,$9C,$A9,$E9,$8D,$10,$03,$A9,$E7,$8D,$11,$03,$60; 15:B719
sub_15_B728:
    LDA obj_spawn_param,X               ; B728  bd 60 05
    BPL loc_15_B736                     ; B72B  10 09
    STX jmp_trampoline                  ; B72D  86 10
    LDA #$B3                            ; B72F  a9 b3
    STA tmp_11                          ; B731  85 11
    JMP FarCall_C25D                    ; B733  4c 80 ff
loc_15_B736:
    LDA obj_timer2,X                    ; B736  bd 20 06
    BEQ loc_15_B73E                     ; B739  f0 03
    JMP loc_15_B7BB                     ; B73B  4c bb b7
loc_15_B73E:
    LDY #$0D                            ; B73E  a0 0d
loc_15_B740:
    STY jmp_trampoline                  ; B740  84 10
    CPX jmp_trampoline                  ; B742  e4 10
    BEQ loc_15_B761                     ; B744  f0 1b
    LDA obj_active,Y                    ; B746  b9 a0 04
    CMP #$78                            ; B749  c9 78
    BEQ loc_15_B761                     ; B74B  f0 14
    CMP #$70                            ; B74D  c9 70
    BEQ loc_15_B761                     ; B74F  f0 10
    CMP #$7F                            ; B751  c9 7f
    BEQ loc_15_B761                     ; B753  f0 0c
    CMP #$30                            ; B755  c9 30
    BEQ loc_15_B761                     ; B757  f0 08
    CMP #$39                            ; B759  c9 39
    BEQ loc_15_B761                     ; B75B  f0 04
    CMP #$00                            ; B75D  c9 00
    BNE loc_15_B7BB                     ; B75F  d0 5a
loc_15_B761:
    INY                                 ; B761  c8
    CPY #$1F                            ; B762  c0 1f
    BNE loc_15_B740                     ; B764  d0 da
    LDA obj_phase,X                     ; B766  bd 00 06
    CMP #$03                            ; B769  c9 03
    BEQ loc_15_B77B                     ; B76B  f0 0e
    INC obj_timer2,X                    ; B76D  fe 20 06
    LDA snd_cur_music                   ; B770  ad 02 01
    LDY #$00                            ; B773  a0 00
    STY snd_cur_music                   ; B775  8c 02 01
    JSR PlaySound                       ; B778  20 25 c1
loc_15_B77B:
    LDA #$8D                            ; B77B  a9 8d
    STA obj_coll,X                      ; B77D  9d a0 03
    LDA #$AA                            ; B780  a9 aa
    STA obj_meta_id,X                   ; B782  9d 00 03
    LDA obj_phase,X                     ; B785  bd 00 06
    ASL A                               ; B788  0a
    ASL A                               ; B789  0a
    CLC                                 ; B78A  18
    ADC obj_phase,X                     ; B78B  7d 00 06
    TAY                                 ; B78E  a8
    LDA $B7C1,Y                         ; B78F  b9 c1 b7
    BEQ loc_15_B7BB                     ; B792  f0 27
    STA pal_id_spr3                     ; B794  8d e7 07
    LDA $B7C2,Y                         ; B797  b9 c2 b7
    STA chr_r5_play                     ; B79A  8d f5 07
    LDA $B7C3,Y                         ; B79D  b9 c3 b7
    STA obj_tile_ofs,X                  ; B7A0  9d 20 03
    LDA $B7C4,Y                         ; B7A3  b9 c4 b7
    STA pending_msg_group               ; B7A6  8d e3 06
    LDA $B7C5,Y                         ; B7A9  b9 c5 b7
    STA pending_msg_id                  ; B7AC  8d c3 06
    ORA pending_msg_group               ; B7AF  0d e3 06
    BEQ loc_15_B7B8                     ; B7B2  f0 04
    LDA #$10                            ; B7B4  a9 10
    STA sub_state                       ; B7B6  85 41
loc_15_B7B8:
    JSR PaletteBuild                    ; B7B8  20 0e 8c
loc_15_B7BB:
    LDA #$10                            ; B7BB  a9 10
    STA boss_cam_lock                   ; B7BD  8d d7 07
    RTS                                 ; B7C0  60
    .db $00,$00,$00,$00,$00,$23,$4F,$00,$00,$00,$09,$51,$49,$20,$14,$2F; 15:B7C1
    .db $62,$09,$00,$00,$1D,$75,$00,$20,$15,$06,$50,$00,$20,$17,$30,$5F; 15:B7D1
    .db $40,$00,$00,$1D,$50,$00,$00,$00,$1E,$50,$00,$00,$00,$18,$74,$00; 15:B7E1
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$20,$68,$40,$00; 15:B7F1
    .db $00,$00,$00,$00,$00,$00         ; 15:B801
BossDefeated:  ; AI $6F (type $CF): slot -> type $0F reward drop; BossAreaList[area] -> 21-byte BossRecs record
    STX jmp_trampoline                  ; B807  86 10
    LDA #$0F                            ; B809  a9 0f
    STA tmp_11                          ; B80B  85 11
    JSR FarCall_C25D                    ; B80D  20 80 ff
    LDA #$80                            ; B810  a9 80
    STA obj_x_lo,X                      ; B812  95 70
    LDA #$90                            ; B814  a9 90
    STA obj_y_lo,X                      ; B816  95 b0
    LDY #$00                            ; B818  a0 00
loc_15_B81A:
    LDA BossAreaList,Y                  ; B81A  b9 5d b9
    CMP area_id                         ; B81D  c5 6c
    BEQ loc_15_B824                     ; B81F  f0 03
    INY                                 ; B821  c8
    BNE loc_15_B81A                     ; B822  d0 f6
loc_15_B824:
    TYA                                 ; B824  98
    STA obj_phase,X                     ; B825  9d 00 06
    ASL A                               ; B828  0a
    TAY                                 ; B829  a8
    LDA BossRecPtrs,Y                   ; B82A  b9 6b b9
    STA tmp_18                          ; B82D  85 18
    PHA                                 ; B82F  48
    LDA $B96C,Y                         ; B830  b9 6c b9
    STA tmp_19                          ; B833  85 19
    PHA                                 ; B835  48
    TXA                                 ; B836  8a
    PHA                                 ; B837  48
    LDY #$00                            ; B838  a0 00
    LDA #$4C                            ; B83A  a9 4c
    STA jmp_trampoline                  ; B83C  85 10
    LDA (tmp_18),Y                      ; B83E  b1 18
    STA tmp_11                          ; B840  85 11
    INY                                 ; B842  c8
    LDA (tmp_18),Y                      ; B843  b1 18
    STA tmp_12                          ; B845  85 12
    ORA tmp_11                          ; B847  05 11
    BEQ loc_15_B84E                     ; B849  f0 03
    JSR a:jmp_trampoline                ; B84B  20 10 00
loc_15_B84E:
    PLA                                 ; B84E  68
    TAX                                 ; B84F  aa
    PLA                                 ; B850  68
    STA tmp_19                          ; B851  85 19
    PLA                                 ; B853  68
    STA tmp_18                          ; B854  85 18
    LDY #$03                            ; B856  a0 03
    LDA (tmp_18),Y                      ; B858  b1 18
    STA snd_cur_music                   ; B85A  8d 02 01
    INY                                 ; B85D  c8
    LDA (tmp_18),Y                      ; B85E  b1 18
    STA obj_spawn_param,X               ; B860  9d 60 05
    INY                                 ; B863  c8
    LDA #$08                            ; B864  a9 08
    STA jmp_trampoline                  ; B866  85 10
loc_15_B868:
    LDA (tmp_18),Y                      ; B868  b1 18
    BEQ loc_15_B86F                     ; B86A  f0 03
    STA ram_07DB,Y                      ; B86C  99 db 07
loc_15_B86F:
    INY                                 ; B86F  c8
    DEC jmp_trampoline                  ; B870  c6 10
    BNE loc_15_B868                     ; B872  d0 f4
    LDA #$06                            ; B874  a9 06
    STA jmp_trampoline                  ; B876  85 10
loc_15_B878:
    LDA (tmp_18),Y                      ; B878  b1 18
    BEQ loc_15_B87F                     ; B87A  f0 03
    STA pal_id_bg3,Y                    ; B87C  99 e3 07
loc_15_B87F:
    INY                                 ; B87F  c8
    DEC jmp_trampoline                  ; B880  c6 10
    BNE loc_15_B878                     ; B882  d0 f4
    LDA (tmp_18),Y                      ; B884  b1 18
    STA area_f62FE                      ; B886  8d fe 62
    INY                                 ; B889  c8
    LDA (tmp_18),Y                      ; B88A  b1 18
    BEQ loc_15_B8CC                     ; B88C  f0 3e
    LDY #$0E                            ; B88E  a0 0e
loc_15_B890:
    LDA obj_active,Y                    ; B890  b9 a0 04
    AND #$7F                            ; B893  29 7f
    BEQ loc_15_B8C7                     ; B895  f0 30
    CMP #$78                            ; B897  c9 78
    BEQ loc_15_B8C7                     ; B899  f0 2c
    CMP #$7F                            ; B89B  c9 7f
    BEQ loc_15_B8C7                     ; B89D  f0 28
    CMP #$30                            ; B89F  c9 30
    BEQ loc_15_B8C7                     ; B8A1  f0 24
    CMP #$39                            ; B8A3  c9 39
    BEQ loc_15_B8C7                     ; B8A5  f0 20
    CMP #$70                            ; B8A7  c9 70
    BEQ loc_15_B8C7                     ; B8A9  f0 1c
    STY jmp_trampoline                  ; B8AB  84 10
    LDA #$D5                            ; B8AD  a9 d5
    STA tmp_11                          ; B8AF  85 11
    JSR FarCall_C25D                    ; B8B1  20 80 ff
    LDA #$80                            ; B8B4  a9 80
    STA obj_tile_ofs,Y                  ; B8B6  99 20 03
    LDA #$EB                            ; B8B9  a9 eb
    STA obj_meta_id,Y                   ; B8BB  99 00 03
    TYA                                 ; B8BE  98
    SEC                                 ; B8BF  38
    SBC #$0E                            ; B8C0  e9 0e
    ASL A                               ; B8C2  0a
    ASL A                               ; B8C3  0a
    STA obj_freeze,Y                    ; B8C4  99 a0 05
loc_15_B8C7:
    INY                                 ; B8C7  c8
    CPY #$1F                            ; B8C8  c0 1f
    BNE loc_15_B890                     ; B8CA  d0 c4
loc_15_B8CC:
    JMP PaletteBuild                    ; B8CC  4c 0e 8c
    .db $20,$76,$C6,$A0,$7F,$A9,$7E,$99,$00,$60,$88,$10,$F8,$A9,$2C,$20; 15:B8CF
    .db $82,$C4,$A0,$0E,$A9,$73,$99,$A0,$04,$C8,$C0,$14,$D0,$F8,$60,$A9; 15:B8DF
    .db $60,$95,$B0,$A9,$1E,$85,$10,$A9,$B2,$85,$11,$4C,$80,$FF,$A9,$00; 15:B8EF
    .db $85,$51,$A0,$0D,$A9,$00,$99,$A0,$04,$C8,$C0,$14,$D0,$F6,$20,$8C; 15:B8FF
    .db $FF,$A0,$03,$B9,$3C,$00,$99,$34,$00,$88,$10,$F7,$A9,$14,$85,$10; 15:B90F
    .db $AD,$D4,$04,$85,$11,$20,$80,$FF,$A9,$15,$85,$10,$A9,$B3,$85,$11; 15:B91F
    .db $20,$80,$FF,$A5,$01,$29,$E7,$85,$01,$20,$69,$C1,$A9,$01,$20,$31; 15:B92F
    .db $C8,$A5,$01,$09,$18,$85,$01,$60,$A9,$00,$8D,$AD,$04,$8D,$AE,$04; 15:B93F
    .db $8D,$AF,$04,$A9,$14,$85,$10,$A9,$B5,$85,$11,$4C,$80,$FF; 15:B94F
BossAreaList:  ; 14 areas: 0A 1A 28 FF 6E F2 A9 AC B9 B6 9F A6 6C 5F
    .db $0A,$1A,$28,$FF,$6E,$F2,$A9,$AC,$B9,$B6,$9F,$A6,$6C,$5F; 15:B95D
BossRecPtrs:  ; ptr16[14] -> 21-byte records (code ptr; ?; music; drop param; 8 pal ids; 6 CHR; $62FE; clear-enemies flag)
    .db $87,$B9,$9C,$B9,$B1,$B9,$00,$00,$C6,$B9,$DB,$B9,$F0,$B9,$05,$BA; 15:B96B
    .db $1A,$BA,$2F,$BA,$44,$BA,$59,$BA,$6E,$BA,$83,$BA; 15:B97B
BossRecs:  ; 13 x 21 bytes (see REVERSE.md Boss defeat)
    .db $00,$00,$00,$11,$2E,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 15:B987
    .db $00,$00,$00,$00,$01,$CF,$B8,$00,$03,$07,$84,$85,$86,$00,$00,$00; 15:B997
    .db $10,$00,$00,$00,$00,$00,$6F,$00,$02,$01,$98,$BA,$00,$18,$08,$00; 15:B9A7
    .db $00,$00,$00,$00,$00,$0C,$00,$00,$00,$00,$00,$6B,$77,$00,$00,$98; 15:B9B7
    .db $BA,$00,$08,$38,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 15:B9C7
    .db $00,$00,$00,$00,$98,$BA,$00,$0D,$0B,$00,$00,$00,$00,$00,$00,$00; 15:B9D7
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$08,$26,$00,$00; 15:B9E7
    .db $00,$00,$00,$00,$23,$00,$00,$00,$00,$00,$63,$00,$00,$01,$00,$00; 15:B9F7
    .db $00,$08,$23,$00,$00,$00,$00,$00,$00,$06,$00,$00,$00,$00,$00,$75; 15:BA07
    .db $00,$00,$01,$00,$00,$00,$08,$12,$00,$00,$00,$00,$00,$00,$06,$00; 15:BA17
    .db $00,$00,$00,$00,$76,$00,$00,$01,$00,$00,$00,$08,$3D,$00,$00,$00; 15:BA27
    .db $00,$00,$00,$18,$00,$00,$00,$00,$00,$61,$00,$00,$01,$EE,$B8,$00; 15:BA37
    .db $19,$1C,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$60,$00; 15:BA47
    .db $02,$01,$FD,$B8,$00,$19,$FF,$3C,$2C,$3E,$7F,$00,$00,$06,$0C,$1A; 15:BA57
    .db $24,$00,$00,$53,$50,$02,$00,$00,$00,$00,$08,$61,$00,$00,$00,$00; 15:BA67
    .db $00,$00,$25,$00,$00,$00,$00,$00,$6B,$00,$00,$01,$47,$B9,$00,$FF; 15:BA77
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 15:BA87
    .db $01                             ; 15:BA97
BossFleeSpeech:  ; record hook for areas 28/6E/F2: player reset; slot $1F type $2B; msg from BossFleeRecs; clear enemies
    LDY #$20                            ; BA98  a0 20
    JSR sub_30_C176                     ; BA9A  20 76 c1
    LDA #$00                            ; BA9D  a9 00
    STA obj1_flash                      ; BA9F  8d 10 07
    STA obj_phase                       ; BAA2  8d 00 06
    STA obj_timer2                      ; BAA5  8d 20 06
    STA orbit_level                     ; BAA8  8d c0 06
    JSR sub_30_C008                     ; BAAB  20 08 c0
    LDA #$80                            ; BAAE  a9 80
    JSR PlaySound                       ; BAB0  20 25 c1
    STX tmp_1F                          ; BAB3  86 1f
    JSR ObjPosToTemp                    ; BAB5  20 a8 98
    LDX #$1F                            ; BAB8  a2 1f
    STX jmp_trampoline                  ; BABA  86 10
    LDA #$2B                            ; BABC  a9 2b
    STA tmp_11                          ; BABE  85 11
    JSR FarCall_C25D                    ; BAC0  20 80 ff
    JSR TempToObjPos                    ; BAC3  20 97 98
    LDX tmp_1F                          ; BAC6  a6 1f
    LDA obj_phase,X                     ; BAC8  bd 00 06
    ASL A                               ; BACB  0a
    TAY                                 ; BACC  a8
    LDA BossFleeRecPtrs,Y               ; BACD  b9 fe ba
    STA jmp_trampoline                  ; BAD0  85 10
    LDA $BAFF,Y                         ; BAD2  b9 ff ba
    STA tmp_11                          ; BAD5  85 11
    LDY #$01                            ; BAD7  a0 01
    LDA (jmp_trampoline),Y              ; BAD9  b1 10
    STA $031F                           ; BADB  8d 1f 03
    INY                                 ; BADE  c8
    LDA (jmp_trampoline),Y              ; BADF  b1 10
    STA $033F                           ; BAE1  8d 3f 03
    INY                                 ; BAE4  c8
    LDA (jmp_trampoline),Y              ; BAE5  b1 10
    STA text_msg_id                     ; BAE7  8d de 07
    INY                                 ; BAEA  c8
    LDA (jmp_trampoline),Y              ; BAEB  b1 10
    STA text_group                      ; BAED  8d df 07
    JSR ClearEnemySlots                 ; BAF0  20 29 bb
    LDA #$29                            ; BAF3  a9 29
    JSR PlaySound                       ; BAF5  20 25 c1
    JSR FarCall_D354                    ; BAF8  20 cf ff
    JMP loc_15_BB5B                     ; BAFB  4c 5b bb
BossFleeRecPtrs:  ; ptr16[14] by boss rec index ($0600) -> 5-byte recs (?; $031F; $033F; msg id; group)
    .db $00,$00,$00,$00,$1A,$BB,$00,$00,$1F,$BB,$24,$BB,$00,$00,$00,$00; 15:BAFE
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$D3,$D3,$00,$01; 15:BB0E
    .db $06,$DB,$D1,$00,$13,$10,$D9,$D9,$00,$05,$19; 15:BB1E
ClearEnemySlots:  ; obj_active=0 for slots $0D-$1E except X and AI $78/$7F/$30/$39; slots 4-11 cleared
    LDY #$0D                            ; BB29  a0 0d
loc_15_BB2B:
    STY jmp_trampoline                  ; BB2B  84 10
    CPX jmp_trampoline                  ; BB2D  e4 10
    BEQ loc_15_BB49                     ; BB2F  f0 18
    LDA obj_active,Y                    ; BB31  b9 a0 04
    CMP #$78                            ; BB34  c9 78
    BEQ loc_15_BB49                     ; BB36  f0 11
    CMP #$7F                            ; BB38  c9 7f
    BEQ loc_15_BB49                     ; BB3A  f0 0d
    CMP #$30                            ; BB3C  c9 30
    BEQ loc_15_BB49                     ; BB3E  f0 09
    CMP #$39                            ; BB40  c9 39
    BEQ loc_15_BB49                     ; BB42  f0 05
    LDA #$00                            ; BB44  a9 00
    STA obj_active,Y                    ; BB46  99 a0 04
loc_15_BB49:
    INY                                 ; BB49  c8
    CPY #$1F                            ; BB4A  c0 1f
    BNE loc_15_BB2B                     ; BB4C  d0 dd
    LDY #$04                            ; BB4E  a0 04
    LDA #$00                            ; BB50  a9 00
loc_15_BB52:
    STA obj_active,Y                    ; BB52  99 a0 04
    INY                                 ; BB55  c8
    CPY #$0C                            ; BB56  c0 0c
    BNE loc_15_BB52                     ; BB58  d0 f8
    RTS                                 ; BB5A  60
loc_15_BB5B:
    LDX #$1F                            ; BB5B  a2 1f
    JSR ObjPosToTemp                    ; BB5D  20 a8 98
    LDA #$FD                            ; BB60  a9 fd
    STA tmp_31                          ; BB62  85 31
    LDA #$FF                            ; BB64  a9 ff
    STA obj_active_s1F                  ; BB66  8d bf 04
    DEC obj_anim,X                      ; BB69  de e0 04
    LDA obj_anim,X                      ; BB6C  bd e0 04
    AND #$1F                            ; BB6F  29 1f
    TAY                                 ; BB71  a8
    LDA $BB8F,Y                         ; BB72  b9 8f bb
    STA tmp_30                          ; BB75  85 30
    JSR TempAddStep                     ; BB77  20 61 98
    JSR TempToObjPos                    ; BB7A  20 97 98
    JSR WaitFrame                       ; BB7D  20 69 c1
    JSR FarCall_B14_800E                ; BB80  20 8c ff
    LDA $CF                             ; BB83  a5 cf
    CMP #$C0                            ; BB85  c9 c0
    BCC loc_15_BB5B                     ; BB87  90 d2
    LDA #$00                            ; BB89  a9 00
    STA obj_active_s1F                  ; BB8B  8d bf 04
    RTS                                 ; BB8E  60
    .db $08,$07,$06,$05,$04,$03,$02,$01,$FF,$FE,$FD,$FC,$FB,$FA,$F9,$F8; 15:BB8F
    .db $F8,$F9,$FA,$FB,$FC,$FD,$FE,$FF,$01,$02,$03,$04,$05,$06,$07,$08; 15:BB9F
StaleFill15_BBAF:  ; dead filler to BC3F: copy of 13 ObjType records
    .db $09,$CC,$00,$64,$1F,$06,$FF,$1B,$00,$FF,$30,$FF,$00,$01,$00,$65; 15:BBAF
    .db $FF,$F9,$00,$0B,$02,$09,$CD,$00,$64,$1F,$00,$20,$57,$03,$00,$30; 15:BBBF
    .db $FF,$00,$01,$00,$74,$FF,$FA,$00,$08,$02,$09,$CD,$00,$64,$1F,$00; 15:BBCF
    .db $20,$2F,$03,$00,$30,$FF,$00,$01,$00,$00,$FF,$9E,$00,$0E,$02,$09; 15:BBDF
    .db $CD,$00,$78,$1F,$00,$40,$57,$03,$08,$30,$FF,$00,$01,$00,$00,$FF; 15:BBEF
    .db $9E,$42,$0D,$02,$09,$CD,$00,$3C,$1F,$14,$60,$10,$03,$00,$30,$FF; 15:BBFF
    .db $00,$01,$01,$00,$FF,$9E,$00,$07,$02,$09,$CD,$00,$20,$1F,$14,$60; 15:BC0F
    .db $10,$03,$00,$30,$FF,$00,$01,$01,$00,$FF,$9E,$01,$07,$02,$09,$CD; 15:BC1F
    .db $00,$2E,$1F,$14,$60,$10,$03,$00,$30,$FF,$00,$01,$01,$00,$FF,$9E; 15:BC2F
    .db $02                             ; 15:BC3F
PauseScreen:  ; s60: START (called directly from 30:CBA8); also substate 12 entry; builds window; INC $41; falls into PauseWait
    LDA irq_mode                        ; BC40  a5 51
    PHA                                 ; BC42  48
    JSR WaitPpuQueue                    ; BC43  20 76 c6
    JSR WaitFrame                       ; BC46  20 69 c1
    LDA irq_mode                        ; BC49  a5 51
    ORA #$80                            ; BC4B  09 80
    STA irq_mode                        ; BC4D  85 51
    STA frame_ready                     ; BC4F  85 09
    LDA ppumask_shadow                  ; BC51  a5 01
    AND #$EF                            ; BC53  29 ef
    STA ppumask_shadow                  ; BC55  85 01
    LDX #$00                            ; BC57  a2 00
    STX $04AC                           ; BC59  8e ac 04
    LDA #$20                            ; BC5C  a9 20
loc_15_BC5E:
    STA wram_buf,X                      ; BC5E  9d 00 60
    DEX                                 ; BC61  ca
    BNE loc_15_BC5E                     ; BC62  d0 fa
    LDX #$E0                            ; BC64  a2 e0
loc_15_BC66:
    LDA #$1E                            ; BC66  a9 1e
    STA wram_6001,X                     ; BC68  9d 01 60
    LDA #$1F                            ; BC6B  a9 1f
    STA $601F,X                         ; BC6D  9d 1f 60
    TXA                                 ; BC70  8a
    SEC                                 ; BC71  38
    SBC #$20                            ; BC72  e9 20
    TAX                                 ; BC74  aa
    BCS loc_15_BC66                     ; BC75  b0 ef
    LDA #$05                            ; BC77  a9 05
    JSR PpuQueueBlock                   ; BC79  20 82 c4
    LDA #$06                            ; BC7C  a9 06
    JSR PpuQueueBlock                   ; BC7E  20 82 c4
    LDX #$1F                            ; BC81  a2 1f
loc_15_BC83:
    LDA #$FF                            ; BC83  a9 ff
    STA wram_buf,X                      ; BC85  9d 00 60
    STA wram_6020,X                     ; BC88  9d 20 60
    LDA #$19                            ; BC8B  a9 19
    STA wram_6040,X                     ; BC8D  9d 40 60
    LDA #$1C                            ; BC90  a9 1c
    STA $60A0,X                         ; BC92  9d a0 60
    DEX                                 ; BC95  ca
    BPL loc_15_BC83                     ; BC96  10 eb
    LDA #$18                            ; BC98  a9 18
    STA wram_6041                       ; BC9A  8d 41 60
    LDY #$1A                            ; BC9D  a0 1a
    STY wram_605F                       ; BC9F  8c 5f 60
    INY                                 ; BCA2  c8
    STY $60A1                           ; BCA3  8c a1 60
    LDA #$1D                            ; BCA6  a9 1d
    STA $60BF                           ; BCA8  8d bf 60
    LDA #$07                            ; BCAB  a9 07
    JSR PpuQueueBlock                   ; BCAD  20 82 c4
    LDA #$08                            ; BCB0  a9 08
    JSR PpuQueueBlock                   ; BCB2  20 82 c4
    LDX #$3F                            ; BCB5  a2 3f
    LDA #$FF                            ; BCB7  a9 ff
loc_15_BCB9:
    STA wram_6060,X                     ; BCB9  9d 60 60
    DEX                                 ; BCBC  ca
    BPL loc_15_BCB9                     ; BCBD  10 fa
    LDA #$03                            ; BCBF  a9 03
    JSR PpuQueueBlock                   ; BCC1  20 82 c4
    JSR WaitFrame                       ; BCC4  20 69 c1
    JSR sub_15_BEBF                     ; BCC7  20 bf be
    LDA ppumask_shadow                  ; BCCA  a5 01
    ORA #$10                            ; BCCC  09 10
    STA ppumask_shadow                  ; BCCE  85 01
    LDA #$02                            ; BCD0  a9 02
    STA irq_mode                        ; BCD2  85 51
    LDA #$3C                            ; BCD4  a9 3c
    STA chr_r0_alt                      ; BCD6  85 58
    LDA #$38                            ; BCD8  a9 38
    STA chr_r1_alt                      ; BCDA  85 59
    LDA chr_r2_play                     ; BCDC  ad f2 07
    STA chr_r2_alt                      ; BCDF  85 5a
    LDA chr_r3_play                     ; BCE1  ad f3 07
    STA chr_r3_alt                      ; BCE4  85 5b
    LDA ppuctrl_shadow                  ; BCE6  a5 00
    AND #$FC                            ; BCE8  29 fc
    ORA #$02                            ; BCEA  09 02
    STA ppuctrl_shadow                  ; BCEC  85 00
    LDX #$00                            ; BCEE  a2 00
loc_15_BCF0:
    LDA $BD84,X                         ; BCF0  bd 84 bd
    STA wram_buf,X                      ; BCF3  9d 00 60
    DEX                                 ; BCF6  ca
    BNE loc_15_BCF0                     ; BCF7  d0 f7
    JSR sub_15_BE11                     ; BCF9  20 11 be
    LDA #$0A                            ; BCFC  a9 0a
    JSR PpuQueueBlock                   ; BCFE  20 82 c4
    LDX #$05                            ; BD01  a2 05
loc_15_BD03:
    LDA player_name_hi,X                ; BD03  bd 10 64
    STA wram_buf,X                      ; BD06  9d 00 60
    LDA player_name6,X                  ; BD09  bd 18 64
    STA wram_6006,X                     ; BD0C  9d 06 60
    DEX                                 ; BD0F  ca
    BPL loc_15_BD03                     ; BD10  10 f1
    LDA #$09                            ; BD12  a9 09
    JSR PpuQueueBlock                   ; BD14  20 82 c4
    LDX #$0F                            ; BD17  a2 0f
loc_15_BD19:
    TXA                                 ; BD19  8a
    JSR PpuQueueBlock                   ; BD1A  20 82 c4
    INX                                 ; BD1D  e8
    CPX #$14                            ; BD1E  e0 14
    BNE loc_15_BD19                     ; BD20  d0 f7
    LDA #$1A                            ; BD22  a9 1a
    JSR SetR6                           ; BD24  20 18 c4
    LDA #$06                            ; BD27  a9 06
    JSR HudRedraw                       ; BD29  20 46 8e
    LDA #$07                            ; BD2C  a9 07
    JSR HudRedraw                       ; BD2E  20 46 8e
    LDA #$08                            ; BD31  a9 08
    JSR HudRedraw                       ; BD33  20 46 8e
    LDA #$09                            ; BD36  a9 09
    JSR HudRedraw                       ; BD38  20 46 8e
    LDA #$0A                            ; BD3B  a9 0a
    JSR HudRedraw                       ; BD3D  20 46 8e
    LDA #$10                            ; BD40  a9 10
    JSR HudRedraw                       ; BD42  20 46 8e
    LDA #$0B                            ; BD45  a9 0b
    STA tmp_17                          ; BD47  85 17
    LDA #$00                            ; BD49  a9 00
    STA tmp_18                          ; BD4B  85 18
    LDA #$03                            ; BD4D  a9 03
    STA tmp_19                          ; BD4F  85 19
loc_15_BD51:
    LDA tmp_18                          ; BD51  a5 18
    ASL A                               ; BD53  0a
    ASL A                               ; BD54  0a
    CLC                                 ; BD55  18
    ADC #$03                            ; BD56  69 03
    TAY                                 ; BD58  a8
    LDX #$03                            ; BD59  a2 03
loc_15_BD5B:
    LDA $BDCB,Y                         ; BD5B  b9 cb bd
    STA $600C,X                         ; BD5E  9d 0c 60
    DEY                                 ; BD61  88
    DEX                                 ; BD62  ca
    BPL loc_15_BD5B                     ; BD63  10 f6
    JSR EquipNameRow                    ; BD65  20 7a be
    LDA tmp_17                          ; BD68  a5 17
    JSR PpuQueueBlock                   ; BD6A  20 82 c4
    INC tmp_17                          ; BD6D  e6 17
    INC tmp_18                          ; BD6F  e6 18
    DEC tmp_19                          ; BD71  c6 19
    BPL loc_15_BD51                     ; BD73  10 dc
    LDA #$02                            ; BD75  a9 02
    STA irq_mode                        ; BD77  85 51
    LDA ppumask_shadow                  ; BD79  a5 01
    ORA #$18                            ; BD7B  09 18
    STA ppumask_shadow                  ; BD7D  85 01
    INC sub_state                       ; BD7F  e6 41
    JMP PauseWait                       ; BD81  4c db bd
    .db $F5,$F6,$F6,$F7,$F8,$20,$20,$F9,$FA,$FB,$FB,$FC,$4C,$56,$20,$20; 15:BD84
    .db $3A,$20,$43,$4F,$4E,$44,$49,$54,$49,$4F,$4E,$3A,$20,$20,$20,$20; 15:BD94
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$4C,$49,$46,$45,$3A,$20; 15:BDA4
    .db $20,$20,$2D,$41,$54,$54,$41,$43,$4B,$20,$3A,$20,$44,$45,$46,$45; 15:BDB4
    .db $4E,$53,$45,$3A,$20,$20,$2D,$29,$2F,$2B,$20,$29,$2A,$2B,$20,$7B; 15:BDC4
    .db $11,$12,$20,$0D,$0E,$0F,$20     ; 15:BDD4
PauseWait:  ; s60: substate 13: loop until START; sub_state=8 at 15:BE07
    JSR WaitFrame                       ; BDDB  20 69 c1
    JSR sub_15_BEBF                     ; BDDE  20 bf be
    JSR ReadPadsMenu                    ; BDE1  20 e0 fe
    JSR sub_15_BF46                     ; BDE4  20 46 bf
    LDA pad_new                         ; BDE7  a5 4b
    AND #$10                            ; BDE9  29 10
    BEQ PauseWait                       ; BDEB  f0 ee
    LDX #$3F                            ; BDED  a2 3f
    LDA #$FF                            ; BDEF  a9 ff
loc_15_BDF1:
    STA $6080,X                         ; BDF1  9d 80 60
    DEX                                 ; BDF4  ca
    BPL loc_15_BDF1                     ; BDF5  10 fa
    LDA #$14                            ; BDF7  a9 14
    JSR PpuQueueBlock                   ; BDF9  20 82 c4
    JSR WaitPpuQueue                    ; BDFC  20 76 c6
    JSR sub_30_C008                     ; BDFF  20 08 c0
    LDA #$00                            ; BE02  a9 00
    STA obj_active_s1F                  ; BE04  8d bf 04
    LDA #$08                            ; BE07  a9 08
    STA sub_state                       ; BE09  85 41
    PLA                                 ; BE0B  68
    STA irq_mode                        ; BE0C  85 51
    JMP ReadPads                        ; BE0E  4c 80 fe
sub_15_BE11:
    LDA obj1_flash                      ; BE11  ad 10 07
    AND #$0F                            ; BE14  29 0f
    ASL A                               ; BE16  0a
    ASL A                               ; BE17  0a
    ASL A                               ; BE18  0a
    ASL A                               ; BE19  0a
    TAY                                 ; BE1A  a8
    LDX #$00                            ; BE1B  a2 00
loc_15_BE1D:
    LDA $BE2A,Y                         ; BE1D  b9 2a be
    STA $601C,X                         ; BE20  9d 1c 60
    INY                                 ; BE23  c8
    INX                                 ; BE24  e8
    CPX #$0B                            ; BE25  e0 0b
    BNE loc_15_BE1D                     ; BE27  d0 f4
    RTS                                 ; BE29  60
    .db $20,$20,$20,$4E,$4F,$52,$4D,$41,$4C,$20,$20,$20,$20,$20,$20,$20; 15:BE2A
    .db $20,$50,$41,$52,$41,$4C,$59,$5A,$45,$44,$20,$20,$20,$20,$20,$20; 15:BE3A
    .db $20,$20,$20,$53,$54,$4F,$4E,$45,$20,$20,$20,$20,$20,$20,$20,$20; 15:BE4A
    .db $20,$20,$50,$4F,$49,$53,$4F,$4E,$45,$44,$20,$20,$20,$20,$20,$20; 15:BE5A
    .db $20,$20,$4D,$55,$54,$41,$54,$45,$44,$20,$20,$20,$20,$20,$20,$20; 15:BE6A
EquipNameRow:  ; s72: SetR6(6); $18=slot 0-3; row = EquipNameBase[slot] + $0711[slot]*$100 (empty -> $80F0 blanks) -> $6010-$601F; SetR6($1A). Caller status screen 15:BD65
    LDA #$06                            ; BE7A  a9 06
    JSR SetR6                           ; BE7C  20 18 c4
    LDA tmp_18                          ; BE7F  a5 18
    ASL A                               ; BE81  0a
    TAY                                 ; BE82  a8
    LDA EquipNameBase,Y                 ; BE83  b9 b7 be
    STA jmp_trampoline                  ; BE86  85 10
    LDA $BEB8,Y                         ; BE88  b9 b8 be
    STA tmp_11                          ; BE8B  85 11
    LDY tmp_18                          ; BE8D  a4 18
    LDA equip_sword,Y                   ; BE8F  b9 11 07
    BNE loc_15_BE9E                     ; BE92  d0 0a
    LDA #$F0                            ; BE94  a9 f0
    STA jmp_trampoline                  ; BE96  85 10
    LDA #$80                            ; BE98  a9 80
    STA tmp_11                          ; BE9A  85 11
    LDA #$00                            ; BE9C  a9 00
loc_15_BE9E:
    CLC                                 ; BE9E  18
    ADC tmp_11                          ; BE9F  65 11
    STA tmp_11                          ; BEA1  85 11
    LDX #$00                            ; BEA3  a2 00
    LDY #$00                            ; BEA5  a0 00
loc_15_BEA7:
    LDA (jmp_trampoline),Y              ; BEA7  b1 10
    STA $6010,X                         ; BEA9  9d 10 60
    INY                                 ; BEAC  c8
    INX                                 ; BEAD  e8
    CPX #$10                            ; BEAE  e0 10
    BNE loc_15_BEA7                     ; BEB0  d0 f5
    LDA #$1A                            ; BEB2  a9 1a
    JMP SetR6                           ; BEB4  4c 18 c4
EquipNameBase:  ; s72 (was PtrTable15_BEB7): .dw[4] bank-6 bases $80F0 sword / $85F0 magic / $8EF0 armor / $97F0 shield; +id*$100 = 16-char name row (tools/equip_name_dump.py)
    .db $F0,$80,$F0,$85,$F0,$8E,$F0,$97 ; 15:BEB7
sub_15_BEBF:
    JSR WaitPpuQueue                    ; BEBF  20 76 c6
    LDA obj1_flash                      ; BEC2  ad 10 07
    BMI loc_15_BF0A                     ; BEC5  30 43
    AND #$0F                            ; BEC7  29 0f
    CMP #$04                            ; BEC9  c9 04
    BNE loc_15_BED1                     ; BECB  d0 04
    LDY #$20                            ; BECD  a0 20
    BNE loc_15_BF0E                     ; BECF  d0 3d
loc_15_BED1:
    LDA #$30                            ; BED1  a9 30
    STA wram_buf                        ; BED3  8d 00 60
    LDA #$36                            ; BED6  a9 36
    STA wram_6001                       ; BED8  8d 01 60
    LDA #$02                            ; BEDB  a9 02
    LDY equip_sword                     ; BEDD  ac 11 07
    BEQ loc_15_BEE4                     ; BEE0  f0 02
    LDA #$06                            ; BEE2  a9 06
loc_15_BEE4:
    STA wram_6002                       ; BEE4  8d 02 60
    LDA #$0E                            ; BEE7  a9 0e
    LDY equip_shield                    ; BEE9  ac 14 07
    BNE loc_15_BEF0                     ; BEEC  d0 02
    LDA #$0A                            ; BEEE  a9 0a
loc_15_BEF0:
    STA wram_6003                       ; BEF0  8d 03 60
    LDA #$0B                            ; BEF3  a9 0b
    STA wram_6004                       ; BEF5  8d 04 60
    LDA #$00                            ; BEF8  a9 00
    STA wram_6005                       ; BEFA  8d 05 60
    STA wram_6006                       ; BEFD  8d 06 60
    STA wram_6007                       ; BF00  8d 07 60
loc_15_BF03:
    LDA #$00                            ; BF03  a9 00
    STA tmp_20                          ; BF05  85 20
    JMP sub_30_C1AE                     ; BF07  4c ae c1
loc_15_BF0A:
    LSR A                               ; BF0A  4a
    AND #$38                            ; BF0B  29 38
    TAY                                 ; BF0D  a8
loc_15_BF0E:
    LDX #$00                            ; BF0E  a2 00
loc_15_BF10:
    LDA $BF1E,Y                         ; BF10  b9 1e bf
    STA wram_buf,X                      ; BF13  9d 00 60
    INY                                 ; BF16  c8
    INX                                 ; BF17  e8
    CPX #$08                            ; BF18  e0 08
    BNE loc_15_BF10                     ; BF1A  d0 f4
    BEQ loc_15_BF03                     ; BF1C  f0 e5
    .db $30,$36,$2E,$00,$09,$80,$00,$00,$30,$36,$32,$00,$09,$80,$00,$00; 15:BF1E
    .db $30,$36,$36,$00,$09,$80,$00,$00,$30,$36,$3A,$00,$09,$80,$00,$00; 15:BF2E
    .db $30,$36,$53,$00,$09,$80,$00,$00 ; 15:BF3E
sub_15_BF46:
    RTS                                 ; BF46  60
    .db $A5,$0F,$D0,$01,$60,$A5,$44,$29,$0F,$F0,$1E,$A2,$17,$BD,$30,$64; 15:BF47
    .db $10,$06,$BD,$7F,$BF,$9D,$30,$64,$CA,$10,$F2,$A9,$0F,$8D,$21,$04; 15:BF57
    .db $A9,$FF,$8D,$08,$07,$8D,$09,$07,$60,$A5,$44,$29,$80,$F0,$F9,$A9; 15:BF67
    .db $09,$8D,$40,$03,$8D,$41,$03,$60,$00,$01,$02,$03,$15,$16,$17,$18; 15:BF77
    .db $0D,$0E,$0F,$10,$06,$08,$0A,$0C,$1D,$1E,$1F,$20,$21,$22,$23,$24; 15:BF87
StaleFill15_BF97:  ; dead filler to BFFF: copy of 13:BF97
    .db $36,$36,$33,$33,$36,$33,$37,$36,$33,$32,$36,$33,$37,$32,$36,$32; 15:BF97
    .db $36,$36,$33,$33,$36,$33,$37,$33,$33,$32,$36,$33,$37,$33,$32,$32; 15:BFA7
    .db $32,$36,$36,$33,$33,$33,$37,$37,$37,$33,$33,$33,$33,$37,$33,$32; 15:BFB7
    .db $36,$33,$33,$33,$33,$33,$37,$33,$33,$33,$33,$33,$37,$33,$37,$33; 15:BFC7
    .db $36,$36,$33,$33,$33,$37,$37,$33,$33,$37,$33,$37,$37,$37,$37,$37; 15:BFD7
    .db $37,$37,$37,$37,$37,$37,$33,$33,$33,$00,$20,$20,$6F,$6F,$FF,$0A; 15:BFE7
    .db $0A,$00,$00,$FF,$FF,$FF,$FF,$FF,$FF; 15:BFF7
