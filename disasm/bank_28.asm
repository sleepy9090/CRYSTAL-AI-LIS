; re-c bank 28  file 0x38010  CPU $8000-$9FFF  (tools/disasm_all.py)

scroll_shadow0 = $02
cam_x_hi = $03
cam_y_lo = $04
cam_y_hi = $05
tmp_08 = $08
jmp_trampoline = $10
tmp_11 = $11
tmp_12 = $12
tmp_13 = $13
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
obj_x_lo = $70
obj_x_hi = $90
obj_y_lo = $B0
obj_y_hi = $D0
oam_y = $0200
oam_tile = $0201
oam_attr = $0202
oam_x = $0203
obj_meta_id = $0300
obj_tile_ofs = $0320
obj_flags2 = $0340
obj_meta_dir = $0360
obj_flags = $0380
obj_flags_s01 = $0381
obj_active = $04A0
obj_anim = $04E0
obj_overlay_id = $0580
obj_screen_x = $05C0
obj_screen_y = $05E0
orbit_level = $06C0
orbit_burst_timer = $06C1
obj1_flash = $0710
obj1_oam_ofs = $07EF
obj_draw_rot = $07FF

    .org $8000
sub_28_8000:
    LDX #$1F                            ; 8000  a2 1f
    LDY #$01                            ; 8002  a0 01
    STY tmp_1B                          ; 8004  84 1b
    JMP ObjDrawMeta                     ; 8006  4c 2e 82
    .db $EA,$EA,$EA,$EA,$EA             ; 28:8009
ObjSpriteDraw:  ; per-frame pass over 32 object slots -> OAM $0204+; order from ObjDrawOrderTable[$07FF&3]; then ObjHideUnusedOam + OrbitSprites
    LDA tmp_08                          ; 800E  a5 08
    LSR A                               ; 8010  4a
    LSR A                               ; 8011  4a
    LDY #$00                            ; 8012  a0 00
    BCC loc_28_8018                     ; 8014  90 02
    LDY #$03                            ; 8016  a0 03
loc_28_8018:
    STY tmp_1E                          ; 8018  84 1e
    DEC obj_draw_rot                    ; 801A  ce ff 07
    LDA obj_draw_rot                    ; 801D  ad ff 07
    AND #$03                            ; 8020  29 03
    TAY                                 ; 8022  a8
    LDA ObjDrawOrderOfs,Y               ; 8023  b9 d8 83
    STA tmp_11                          ; 8026  85 11
    LDA #$20                            ; 8028  a9 20
    STA tmp_1B                          ; 802A  85 1b
    LDA #$04                            ; 802C  a9 04
    STA jmp_trampoline                  ; 802E  85 10
loc_28_8030:
    LDY tmp_11                          ; 8030  a4 11
    LDA ObjDrawOrderTable,Y             ; 8032  b9 dc 83
    TAX                                 ; 8035  aa
    LDA obj_active,X                    ; 8036  bd a0 04
    BEQ ObjSpriteDraw_Offscreen         ; 8039  f0 5f
    LDA obj_x_lo,X                      ; 803B  b5 70
    SEC                                 ; 803D  38
    SBC scroll_shadow0                  ; 803E  e5 02
    TAY                                 ; 8040  a8
    LDA obj_x_hi,X                      ; 8041  b5 90
    SBC cam_x_hi                        ; 8043  e5 03
    BNE ObjSpriteDraw_Offscreen         ; 8045  d0 53
    STY tmp_12                          ; 8047  84 12
    TYA                                 ; 8049  98
    STA obj_screen_x,X                  ; 804A  9d c0 05
    LDA obj_y_lo,X                      ; 804D  b5 b0
    CMP cam_y_lo                        ; 804F  c5 04
    LDA obj_y_hi,X                      ; 8051  b5 d0
    SBC cam_y_hi                        ; 8053  e5 05
    BNE ObjSpriteDraw_Offscreen         ; 8055  d0 43
    LDA obj_y_lo,X                      ; 8057  b5 b0
    SEC                                 ; 8059  38
    SBC cam_y_lo                        ; 805A  e5 04
    BCS loc_28_8060                     ; 805C  b0 02
    SBC #$0F                            ; 805E  e9 0f
loc_28_8060:
    CMP #$E8                            ; 8060  c9 e8
    BCS ObjSpriteDraw_Next              ; 8062  b0 2a
    CMP #$10                            ; 8064  c9 10
    BCC ObjSpriteDraw_Offscreen         ; 8066  90 32
    STA tmp_13                          ; 8068  85 13
    STA obj_screen_y,X                  ; 806A  9d e0 05
    LDA obj_flags,X                     ; 806D  bd 80 03
    AND #$7F                            ; 8070  29 7f
    STA obj_flags,X                     ; 8072  9d 80 03
    ASL A                               ; 8075  0a
    STA tmp_17                          ; 8076  85 17
    LDA jmp_trampoline                  ; 8078  a5 10
    BEQ ObjSpriteDraw_Next              ; 807A  f0 12
    LDA obj_flags,X                     ; 807C  bd 80 03
    AND #$04                            ; 807F  29 04
    BEQ loc_28_808B                     ; 8081  f0 08
    TXA                                 ; 8083  8a
    CLC                                 ; 8084  18
    ADC obj_draw_rot                    ; 8085  6d ff 07
    LSR A                               ; 8088  4a
    BCS ObjSpriteDraw_Next              ; 8089  b0 03
loc_28_808B:
    JSR ObjDrawMeta                     ; 808B  20 2e 82
ObjSpriteDraw_Next:  ; loop tail (INC $11 / DEC $1B count 32)
    INC tmp_11                          ; 808E  e6 11
    DEC tmp_1B                          ; 8090  c6 1b
    BNE loc_28_8030                     ; 8092  d0 9c
    JSR ObjHideUnusedOam                ; 8094  20 a5 80
    JMP OrbitSprites                    ; 8097  4c d6 80
ObjSpriteDraw_Offscreen:  ; sets $0380;X bit7 (offscreen) and skips
    LDA obj_flags,X                     ; 809A  bd 80 03
    ORA #$80                            ; 809D  09 80
    STA obj_flags,X                     ; 809F  9d 80 03
    JMP ObjSpriteDraw_Next              ; 80A2  4c 8e 80
ObjHideUnusedOam:  ; Y=$F0 for OAM from $10 to $FF; if $0710 bit7 clears palette bits of obj1's 6 (4 if $0381 b6) sprites at $07EF
    LDX jmp_trampoline                  ; 80A5  a6 10
    BEQ loc_28_80B4                     ; 80A7  f0 0b
    LDA #$F0                            ; 80A9  a9 f0
loc_28_80AB:
    STA oam_y,X                         ; 80AB  9d 00 02
    INX                                 ; 80AE  e8
    INX                                 ; 80AF  e8
    INX                                 ; 80B0  e8
    INX                                 ; 80B1  e8
    BNE loc_28_80AB                     ; 80B2  d0 f7
loc_28_80B4:
    LDA obj1_flash                      ; 80B4  ad 10 07
    BPL loc_28_80D5                     ; 80B7  10 1c
    LDY #$06                            ; 80B9  a0 06
    LDA obj_flags_s01                   ; 80BB  ad 81 03
    ASL A                               ; 80BE  0a
    BPL loc_28_80C3                     ; 80BF  10 02
    LDY #$04                            ; 80C1  a0 04
loc_28_80C3:
    LDX obj1_oam_ofs                    ; 80C3  ae ef 07
loc_28_80C6:
    LDA oam_attr,X                      ; 80C6  bd 02 02
    AND #$FC                            ; 80C9  29 fc
    STA oam_attr,X                      ; 80CB  9d 02 02
    INX                                 ; 80CE  e8
    INX                                 ; 80CF  e8
    INX                                 ; 80D0  e8
    INX                                 ; 80D1  e8
    DEY                                 ; 80D2  88
    BNE loc_28_80C6                     ; 80D3  d0 f1
loc_28_80D5:
    RTS                                 ; 80D5  60
OrbitSprites:  ; 8 sprites at OAM $E0-$FC around obj1 ($05C1/$05E1): $06C1!=0 burst (tile $29) else $06C0>>3 orbit (sine $81BA)
    LDA orbit_burst_timer               ; 80D6  ad c1 06
    BNE loc_28_8145                     ; 80D9  d0 6a
    LDA orbit_level                     ; 80DB  ad c0 06
    AND #$F8                            ; 80DE  29 f8
    BNE loc_28_80E3                     ; 80E0  d0 01
    RTS                                 ; 80E2  60
loc_28_80E3:
    LDA #$07                            ; 80E3  a9 07
    STA jmp_trampoline                  ; 80E5  85 10
    LDX #$E0                            ; 80E7  a2 e0
loc_28_80E9:
    LDA jmp_trampoline                  ; 80E9  a5 10
    AND #$06                            ; 80EB  29 06
    ASL A                               ; 80ED  0a
    ASL A                               ; 80EE  0a
    ADC tmp_08                          ; 80EF  65 08
    AND #$1F                            ; 80F1  29 1f
    TAY                                 ; 80F3  a8
    LDA jmp_trampoline                  ; 80F4  a5 10
    LSR A                               ; 80F6  4a
    LDA OrbitSineTable,Y                ; 80F7  b9 ba 81
    BCS loc_28_8101                     ; 80FA  b0 05
    EOR #$FF                            ; 80FC  49 ff
    CLC                                 ; 80FE  18
    ADC #$01                            ; 80FF  69 01
loc_28_8101:
    SEC                                 ; 8101  38
    SBC #$04                            ; 8102  e9 04
    STA tmp_11                          ; 8104  85 11
    CLC                                 ; 8106  18
    ADC $05C1                           ; 8107  6d c1 05
    STA tmp_12                          ; 810A  85 12
    ROR A                               ; 810C  6a
    EOR tmp_11                          ; 810D  45 11
    BMI loc_28_813C                     ; 810F  30 2b
    LDA tmp_12                          ; 8111  a5 12
    STA oam_x,X                         ; 8113  9d 03 02
    TYA                                 ; 8116  98
    CLC                                 ; 8117  18
    ADC #$02                            ; 8118  69 02
    AND #$1F                            ; 811A  29 1f
    TAY                                 ; 811C  a8
    LDA OrbitSineTable,Y                ; 811D  b9 ba 81
    CLC                                 ; 8120  18
    ADC $05E1                           ; 8121  6d e1 05
    SEC                                 ; 8124  38
    SBC #$0C                            ; 8125  e9 0c
    STA oam_y,X                         ; 8127  9d 00 02
    LDA orbit_level                     ; 812A  ad c0 06
    LSR A                               ; 812D  4a
    LSR A                               ; 812E  4a
    LSR A                               ; 812F  4a
    TAY                                 ; 8130  a8
    LDA OrbitTileTable,Y                ; 8131  b9 da 81
    STA oam_tile,X                      ; 8134  9d 01 02
    LDA #$01                            ; 8137  a9 01
    STA oam_attr,X                      ; 8139  9d 02 02
loc_28_813C:
    INX                                 ; 813C  e8
    INX                                 ; 813D  e8
    INX                                 ; 813E  e8
    INX                                 ; 813F  e8
    DEC jmp_trampoline                  ; 8140  c6 10
    BPL loc_28_80E9                     ; 8142  10 a5
    RTS                                 ; 8144  60
loc_28_8145:
    LDA orbit_burst_timer               ; 8145  ad c1 06
    BEQ loc_28_81A4                     ; 8148  f0 5a
    DEC orbit_burst_timer               ; 814A  ce c1 06
    CMP #$0E                            ; 814D  c9 0e
    BCS loc_28_81A4                     ; 814F  b0 53
    LDA #$07                            ; 8151  a9 07
    STA jmp_trampoline                  ; 8153  85 10
    LDX #$E0                            ; 8155  a2 e0
loc_28_8157:
    LDA jmp_trampoline                  ; 8157  a5 10
    ASL A                               ; 8159  0a
    ASL A                               ; 815A  0a
    TAY                                 ; 815B  a8
    JSR BurstRadius                     ; 815C  20 a5 81
    SEC                                 ; 815F  38
    SBC #$04                            ; 8160  e9 04
    STA tmp_11                          ; 8162  85 11
    CLC                                 ; 8164  18
    ADC $05C1                           ; 8165  6d c1 05
    STA tmp_12                          ; 8168  85 12
    ROR A                               ; 816A  6a
    EOR tmp_11                          ; 816B  45 11
    BMI loc_28_819C                     ; 816D  30 2d
    LDA tmp_12                          ; 816F  a5 12
    STA oam_x,X                         ; 8171  9d 03 02
    LDA jmp_trampoline                  ; 8174  a5 10
    SEC                                 ; 8176  38
    ROL A                               ; 8177  2a
    ASL A                               ; 8178  0a
    TAY                                 ; 8179  a8
    JSR BurstRadius                     ; 817A  20 a5 81
    SEC                                 ; 817D  38
    SBC #$0C                            ; 817E  e9 0c
    STA tmp_12                          ; 8180  85 12
    CLC                                 ; 8182  18
    ADC $05E1                           ; 8183  6d e1 05
    STA tmp_11                          ; 8186  85 11
    ROR A                               ; 8188  6a
    EOR tmp_12                          ; 8189  45 12
    BMI loc_28_819C                     ; 818B  30 0f
    LDA tmp_11                          ; 818D  a5 11
    STA oam_y,X                         ; 818F  9d 00 02
    LDA #$29                            ; 8192  a9 29
    STA oam_tile,X                      ; 8194  9d 01 02
    LDA #$01                            ; 8197  a9 01
    STA oam_attr,X                      ; 8199  9d 02 02
loc_28_819C:
    INX                                 ; 819C  e8
    INX                                 ; 819D  e8
    INX                                 ; 819E  e8
    INX                                 ; 819F  e8
    DEC jmp_trampoline                  ; 81A0  c6 10
    BPL loc_28_8157                     ; 81A2  10 b3
loc_28_81A4:
    RTS                                 ; 81A4  60
BurstRadius:  ; Y=ray*4(+2 for Y): radius $81FE[$81DE[Y]|$06C1] negated if $81DF[Y] bit7
    LDA $81DF,Y                         ; 81A5  b9 df 81
    ASL A                               ; 81A8  0a
    LDA BurstDirTable,Y                 ; 81A9  b9 de 81
    ORA orbit_burst_timer               ; 81AC  0d c1 06
    TAY                                 ; 81AF  a8
    LDA BurstRadiusTable,Y              ; 81B0  b9 fe 81
    BCC loc_28_81B9                     ; 81B3  90 04
    EOR #$FF                            ; 81B5  49 ff
    ADC #$00                            ; 81B7  69 00
loc_28_81B9:
    RTS                                 ; 81B9  60
OrbitSineTable:  ; 32 x s8 sine (radius $21) for OrbitSprites
    .db $DF,$E0,$E2,$E5,$E9,$EF,$F5,$FC,$04,$0B,$11,$17,$1B,$1E,$20,$21; 28:81BA
    .db $21,$20,$1E,$1B,$17,$11,$0B,$04,$FC,$F5,$EF,$E9,$E5,$E2,$E0,$DF; 28:81CA
OrbitTileTable:  ; tile per $06C0>>3: 29 29 2A 32 20
    .db $29,$29,$2A,$32                 ; 28:81DA
BurstDirTable:  ; 8 rays x (Xidx;Xsign;Yidx;Ysign): idx 00/10/20 selects radius row
    .db $20,$00,$00,$FF,$10,$00,$10,$FF,$00,$00,$20,$00,$10,$00,$10,$00; 28:81DE
    .db $20,$00,$00,$00,$10,$FF,$10,$00,$00,$FF,$20,$00,$10,$FF,$10,$FF; 28:81EE
BurstRadiusTable:  ; 3 rows x 16 radii (row0 diagonal 05..60; row1 04..44; row2 zeros) indexed by $06C1
    .db $05,$12,$1E,$29,$33,$3C,$44,$4B,$51,$56,$5A,$5D,$5F,$60,$FF,$FF; 28:81FE
    .db $04,$0D,$15,$1D,$24,$2A,$30,$35,$39,$3D,$40,$42,$43,$44,$FF,$FF; 28:820E
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$FF,$FF; 28:821E
ObjDrawMeta:  ; draw obj X metasprite: optional overlay $0580;X first if $0380 b1; $0380 b6 -> Y+3
    LDA tmp_17                          ; 822E  a5 17
    ASL A                               ; 8230  0a
    BCC loc_28_8239                     ; 8231  90 06
    LDA tmp_13                          ; 8233  a5 13
    ADC #$02                            ; 8235  69 02
    STA tmp_13                          ; 8237  85 13
loc_28_8239:
    LDA tmp_17                          ; 8239  a5 17
    AND #$04                            ; 823B  29 04
    BEQ ObjDrawMetaMain                 ; 823D  f0 29
    LDA obj_meta_id,X                   ; 823F  bd 00 03
    PHA                                 ; 8242  48
    LDA obj_flags,X                     ; 8243  bd 80 03
    PHA                                 ; 8246  48
    AND #$DF                            ; 8247  29 df
    ORA #$01                            ; 8249  09 01
    STA obj_flags,X                     ; 824B  9d 80 03
    LDA obj_overlay_id,X                ; 824E  bd 80 05
    STA obj_meta_id,X                   ; 8251  9d 00 03
    TXA                                 ; 8254  8a
    PHA                                 ; 8255  48
    JSR ObjDrawMetaMain                 ; 8256  20 68 82
    PLA                                 ; 8259  68
    TAX                                 ; 825A  aa
    PLA                                 ; 825B  68
    STA obj_flags,X                     ; 825C  9d 80 03
    PLA                                 ; 825F  68
    STA obj_meta_id,X                   ; 8260  9d 00 03
    LDA jmp_trampoline                  ; 8263  a5 10
    BNE ObjDrawMetaMain                 ; 8265  d0 01
    RTS                                 ; 8267  60
ObjDrawMetaMain:  ; id=$0300;X(+$0360;X unless $0380 b0) -> ObjMetaPtrLo/Hi; frame=($04E0;X>>3)&mask; emits OAM
    CPX #$01                            ; 8268  e0 01
    BNE loc_28_8271                     ; 826A  d0 05
    LDA jmp_trampoline                  ; 826C  a5 10
    STA obj1_oam_ofs                    ; 826E  8d ef 07
loc_28_8271:
    LDA #$00                            ; 8271  a9 00
    STA tmp_1A                          ; 8273  85 1a
    STA tmp_1C                          ; 8275  85 1c
    LDA obj_flags,X                     ; 8277  bd 80 03
    LSR A                               ; 827A  4a
    LDA obj_meta_id,X                   ; 827B  bd 00 03
    BCS loc_28_8283                     ; 827E  b0 03
    ADC obj_meta_dir,X                  ; 8280  7d 60 03
loc_28_8283:
    ASL A                               ; 8283  0a
    TAY                                 ; 8284  a8
    BCS loc_28_8293                     ; 8285  b0 0c
    LDA ObjMetaPtrTableA,Y              ; 8287  b9 5c 84
    STA tmp_15                          ; 828A  85 15
    LDA $845D,Y                         ; 828C  b9 5d 84
    STA tmp_16                          ; 828F  85 16
    BCC loc_28_829D                     ; 8291  90 0a
loc_28_8293:
    LDA ObjMetaPtrTableB,Y              ; 8293  b9 5c 85
    STA tmp_15                          ; 8296  85 15
    LDA $855D,Y                         ; 8298  b9 5d 85
    STA tmp_16                          ; 829B  85 16
loc_28_829D:
    LDY #$00                            ; 829D  a0 00
    LDA (tmp_15),Y                      ; 829F  b1 15
    BEQ loc_28_82B8                     ; 82A1  f0 15
    BPL loc_28_82B9                     ; 82A3  10 14
    INY                                 ; 82A5  c8
    LDA (tmp_15),Y                      ; 82A6  b1 15
    PHA                                 ; 82A8  48
    INY                                 ; 82A9  c8
    LDA (tmp_15),Y                      ; 82AA  b1 15
    STA tmp_16                          ; 82AC  85 16
    PLA                                 ; 82AE  68
    STA tmp_15                          ; 82AF  85 15
    LDA #$80                            ; 82B1  a9 80
    STA tmp_1C                          ; 82B3  85 1c
    JMP loc_28_829D                     ; 82B5  4c 9d 82
loc_28_82B8:
    RTS                                 ; 82B8  60
loc_28_82B9:
    STA tmp_17                          ; 82B9  85 17
    STA tmp_18                          ; 82BB  85 18
    INY                                 ; 82BD  c8
    LDA obj_anim,X                      ; 82BE  bd e0 04
    LSR A                               ; 82C1  4a
    LSR A                               ; 82C2  4a
    LSR A                               ; 82C3  4a
    AND (tmp_15),Y                      ; 82C4  31 15
    STA tmp_1D                          ; 82C6  85 1d
    LDA #$00                            ; 82C8  a9 00
    LSR tmp_1D                          ; 82CA  46 1d
    BCC loc_28_82D1                     ; 82CC  90 03
    CLC                                 ; 82CE  18
    ADC tmp_18                          ; 82CF  65 18
loc_28_82D1:
    ASL tmp_18                          ; 82D1  06 18
    LSR tmp_1D                          ; 82D3  46 1d
    BCC loc_28_82DA                     ; 82D5  90 03
    CLC                                 ; 82D7  18
    ADC tmp_18                          ; 82D8  65 18
loc_28_82DA:
    ASL tmp_18                          ; 82DA  06 18
    LSR tmp_1D                          ; 82DC  46 1d
    BCC loc_28_82E3                     ; 82DE  90 03
    CLC                                 ; 82E0  18
    ADC tmp_18                          ; 82E1  65 18
loc_28_82E3:
    ASL tmp_18                          ; 82E3  06 18
    LSR tmp_1D                          ; 82E5  46 1d
    BCC loc_28_82EC                     ; 82E7  90 03
    CLC                                 ; 82E9  18
    ADC tmp_18                          ; 82EA  65 18
loc_28_82EC:
    ASL A                               ; 82EC  0a
    BCC loc_28_82F3                     ; 82ED  90 04
    INC tmp_16                          ; 82EF  e6 16
    INC tmp_16                          ; 82F1  e6 16
loc_28_82F3:
    ASL A                               ; 82F3  0a
    BCC loc_28_82F8                     ; 82F4  90 02
    INC tmp_16                          ; 82F6  e6 16
loc_28_82F8:
    CLC                                 ; 82F8  18
    ADC tmp_15                          ; 82F9  65 15
    STA tmp_15                          ; 82FB  85 15
    BCC loc_28_8301                     ; 82FD  90 02
    INC tmp_16                          ; 82FF  e6 16
loc_28_8301:
    LDA obj_flags,X                     ; 8301  bd 80 03
    ASL A                               ; 8304  0a
    STA tmp_18                          ; 8305  85 18
    AND #$20                            ; 8307  29 20
    STA tmp_19                          ; 8309  85 19
    LDA obj_tile_ofs,X                  ; 830B  bd 20 03
    CLC                                 ; 830E  18
    ADC tmp_1A                          ; 830F  65 1a
    STA tmp_1A                          ; 8311  85 1a
    LDA obj_flags,X                     ; 8313  bd 80 03
    AND #$20                            ; 8316  29 20
    ASL A                               ; 8318  0a
    CLC                                 ; 8319  18
    ADC tmp_1A                          ; 831A  65 1a
    STA tmp_1A                          ; 831C  85 1a
    LDY #$00                            ; 831E  a0 00
    LDA obj_flags2,X                    ; 8320  bd 40 03
    BPL loc_28_8327                     ; 8323  10 02
    LDY tmp_1E                          ; 8325  a4 1e
loc_28_8327:
    STY tmp_1D                          ; 8327  84 1d
    LDX jmp_trampoline                  ; 8329  a6 10
    LDY #$02                            ; 832B  a0 02
    BIT tmp_1C                          ; 832D  24 1c
    BMI MetaEmitHFlip                   ; 832F  30 4f
MetaEmitNormal:  ; 4-byte entries dx;dy;attr;tile; dx=$80 ends; X wrap skips entry
    LDA (tmp_15),Y                      ; 8331  b1 15
    CMP #$80                            ; 8333  c9 80
    BEQ loc_28_836B                     ; 8335  f0 34
    CLC                                 ; 8337  18
    ADC tmp_12                          ; 8338  65 12
    STA oam_x,X                         ; 833A  9d 03 02
    ROR A                               ; 833D  6a
    EOR (tmp_15),Y                      ; 833E  51 15
    BMI loc_28_836E                     ; 8340  30 2c
    INY                                 ; 8342  c8
    LDA (tmp_15),Y                      ; 8343  b1 15
    CLC                                 ; 8345  18
    ADC tmp_13                          ; 8346  65 13
    STA oam_y,X                         ; 8348  9d 00 02
    INY                                 ; 834B  c8
    LDA (tmp_15),Y                      ; 834C  b1 15
    INY                                 ; 834E  c8
    ORA tmp_19                          ; 834F  05 19
    STA oam_attr,X                      ; 8351  9d 02 02
    AND #$04                            ; 8354  29 04
    BNE loc_28_8379                     ; 8356  d0 21
loc_28_8358:
    LDA (tmp_15),Y                      ; 8358  b1 15
    CLC                                 ; 835A  18
    ADC tmp_1A                          ; 835B  65 1a
    STA oam_tile,X                      ; 835D  9d 01 02
    INY                                 ; 8360  c8
    INX                                 ; 8361  e8
    INX                                 ; 8362  e8
    INX                                 ; 8363  e8
    INX                                 ; 8364  e8
    BEQ loc_28_836B                     ; 8365  f0 04
    DEC tmp_17                          ; 8367  c6 17
    BNE MetaEmitNormal                  ; 8369  d0 c6
loc_28_836B:
    STX jmp_trampoline                  ; 836B  86 10
    RTS                                 ; 836D  60
loc_28_836E:
    DEC tmp_17                          ; 836E  c6 17
    BEQ loc_28_836B                     ; 8370  f0 f9
    INY                                 ; 8372  c8
    INY                                 ; 8373  c8
    INY                                 ; 8374  c8
    INY                                 ; 8375  c8
    JMP MetaEmitNormal                  ; 8376  4c 31 83
loc_28_8379:
    BIT tmp_18                          ; 8379  24 18
    BPL loc_28_8358                     ; 837B  10 db
    STX jmp_trampoline                  ; 837D  86 10
    RTS                                 ; 837F  60
MetaEmitHFlip:  ; used after FF redirect: X=-dx-8; attr^=$40
    LDA (tmp_15),Y                      ; 8380  b1 15
    CMP #$80                            ; 8382  c9 80
    BEQ loc_28_83C3                     ; 8384  f0 3d
    EOR #$FF                            ; 8386  49 ff
    SEC                                 ; 8388  38
    SBC #$08                            ; 8389  e9 08
    STA tmp_1C                          ; 838B  85 1c
    CLC                                 ; 838D  18
    ADC tmp_12                          ; 838E  65 12
    STA oam_x,X                         ; 8390  9d 03 02
    ROR A                               ; 8393  6a
    EOR tmp_1C                          ; 8394  45 1c
    BMI loc_28_83C6                     ; 8396  30 2e
    INY                                 ; 8398  c8
    LDA (tmp_15),Y                      ; 8399  b1 15
    CLC                                 ; 839B  18
    ADC tmp_13                          ; 839C  65 13
    STA oam_y,X                         ; 839E  9d 00 02
    INY                                 ; 83A1  c8
    LDA (tmp_15),Y                      ; 83A2  b1 15
    INY                                 ; 83A4  c8
    ORA tmp_19                          ; 83A5  05 19
    EOR #$40                            ; 83A7  49 40
    STA oam_attr,X                      ; 83A9  9d 02 02
    AND #$04                            ; 83AC  29 04
    BNE loc_28_83D1                     ; 83AE  d0 21
loc_28_83B0:
    LDA (tmp_15),Y                      ; 83B0  b1 15
    CLC                                 ; 83B2  18
    ADC tmp_1A                          ; 83B3  65 1a
    STA oam_tile,X                      ; 83B5  9d 01 02
    INY                                 ; 83B8  c8
    INX                                 ; 83B9  e8
    INX                                 ; 83BA  e8
    INX                                 ; 83BB  e8
    INX                                 ; 83BC  e8
    BEQ loc_28_83C3                     ; 83BD  f0 04
    DEC tmp_17                          ; 83BF  c6 17
    BNE MetaEmitHFlip                   ; 83C1  d0 bd
loc_28_83C3:
    STX jmp_trampoline                  ; 83C3  86 10
    RTS                                 ; 83C5  60
loc_28_83C6:
    DEC tmp_17                          ; 83C6  c6 17
    BEQ loc_28_83C3                     ; 83C8  f0 f9
    INY                                 ; 83CA  c8
    INY                                 ; 83CB  c8
    INY                                 ; 83CC  c8
    INY                                 ; 83CD  c8
    JMP MetaEmitHFlip                   ; 83CE  4c 80 83
loc_28_83D1:
    BIT tmp_18                          ; 83D1  24 18
    BPL loc_28_83B0                     ; 83D3  10 db
    STX jmp_trampoline                  ; 83D5  86 10
    RTS                                 ; 83D7  60
ObjDrawOrderOfs:  ; 4 x u8 (00 20 40 60) offset into ObjDrawOrderTable
    .db $00,$20,$40,$60                 ; 28:83D8
ObjDrawOrderTable:  ; 4 x 32 slot permutations (flicker order); 0=04..1F;00; 3=02 01 1F..03 00
    .db $04,$05,$06,$07,$02,$01,$03,$08,$09,$0A,$0B,$0C,$0D,$0E,$0F,$10; 28:83DC
    .db $11,$12,$13,$14,$15,$16,$17,$18,$19,$1A,$1B,$1C,$1D,$1E,$1F,$00; 28:83EC
    .db $0A,$0B,$08,$09,$02,$01,$00,$0E,$0F,$0C,$0D,$1A,$1B,$18,$19,$1E; 28:83FC
    .db $1F,$1C,$1D,$03,$06,$07,$04,$05,$12,$13,$10,$11,$16,$17,$14,$15; 28:840C
    .db $02,$01,$15,$14,$17,$16,$11,$10,$13,$12,$05,$04,$07,$06,$03,$1D; 28:841C
    .db $1C,$1F,$1E,$19,$18,$1B,$1A,$0D,$0C,$0F,$0E,$09,$08,$0B,$0A,$00; 28:842C
    .db $02,$01,$1F,$1E,$1D,$1C,$1B,$1A,$19,$18,$17,$16,$15,$14,$13,$12; 28:843C
    .db $11,$10,$0F,$0E,$0D,$0C,$0B,$0A,$09,$08,$07,$06,$05,$04,$03,$00; 28:844C
ObjMetaPtrTableA:  ; ptr16[128] metasprite ids $00-$7F
    .db $8E,$86,$C0,$86,$5C,$86,$F2,$86,$4B,$87,$59,$88,$CF,$87,$53,$88; 28:845C
    .db $47,$87,$3D,$87,$49,$87,$3A,$87,$07,$87,$19,$87,$F5,$86,$33,$87; 28:846C
    .db $B1,$87,$BF,$88,$35,$88,$56,$88,$9D,$8C,$A7,$8C,$B1,$8C,$BB,$8C; 28:847C
    .db $16,$89,$48,$89,$7A,$89,$AC,$89,$04,$89,$AF,$89,$0F,$8D,$0F,$8D; 28:848C
    .db $BE,$8C,$F5,$8C,$DB,$8C,$D8,$8C,$21,$8D,$2F,$8D,$21,$8D,$2F,$8D; 28:849C
    .db $F3,$89,$05,$8A,$9B,$8B,$D1,$89,$69,$91,$9B,$91,$CD,$91,$FF,$91; 28:84AC
    .db $02,$92,$34,$92,$66,$92,$98,$92,$9B,$92,$CD,$92,$FF,$92,$31,$93; 28:84BC
    .db $34,$93,$66,$93,$98,$93,$CA,$93,$9A,$94,$BC,$94,$DE,$94,$00,$95; 28:84CC
    .db $03,$95,$25,$95,$47,$95,$69,$95,$CD,$93,$FF,$93,$31,$94,$63,$94; 28:84DC
    .db $6C,$95,$8E,$95,$B0,$95,$D2,$95,$63,$91,$66,$94,$80,$94,$D5,$95; 28:84EC
    .db $07,$96,$3B,$96,$C7,$9B,$3D,$8D,$5D,$96,$60,$96,$92,$96,$CC,$96; 28:84FC
    .db $FE,$96,$30,$97,$62,$97,$94,$97,$FE,$9A,$40,$9B,$82,$9B,$C4,$9B; 28:850C
    .db $EB,$99,$35,$9A,$7F,$9A,$C9,$9A,$97,$97,$B9,$97,$03,$98,$4D,$98; 28:851C
    .db $DF,$98,$41,$99,$9F,$99,$B1,$99,$21,$9C,$63,$9C,$A5,$9C,$E7,$9C; 28:852C
    .db $EA,$9C,$24,$9D,$76,$9D,$A8,$9D,$AB,$9D,$ED,$9D,$2F,$9E,$71,$9E; 28:853C
    .db $63,$A0,$AD,$A0,$F7,$A0,$41,$A1,$44,$A1,$6A,$A1,$90,$A1,$B6,$A1; 28:854C
ObjMetaPtrTableB:  ; ptr16[128] metasprite ids $80-$FF (bank 29 $A000 range); id $9A = $0000 (reads zero page; s54 meta_ptr_check.py)
    .db $D5,$A4,$EB,$A4,$01,$A5,$17,$A5,$F2,$A5,$FC,$A5,$06,$A6,$10,$A6; 28:855C
    .db $A9,$B0,$EC,$B1,$CE,$B0,$AA,$B1,$50,$9F,$92,$9F,$D4,$9F,$16,$A0; 28:856C
    .db $74,$9E,$A2,$9E,$19,$A0,$01,$A2,$B9,$A1,$DF,$A1,$EF,$B1,$B3,$A2; 28:857C
    .db $CC,$9A,$BF,$A6,$00,$00,$35,$A3,$B3,$A3,$09,$A4,$CB,$A4,$1A,$A5; 28:858C
    .db $3C,$A5,$5E,$A5,$B0,$A5,$13,$A6,$35,$A6,$57,$A6,$5D,$A6,$77,$A8; 28:859C
    .db $F1,$A6,$03,$A7,$41,$A7,$53,$A7,$B5,$A7,$21,$96,$81,$A8,$EB,$A8; 28:85AC
    .db $2D,$A9,$8F,$A9,$C1,$A9,$77,$AA,$F5,$AA,$67,$AC,$E9,$AC,$2B,$AD; 28:85BC
    .db $7D,$AE,$C7,$AE,$41,$AF,$A3,$AF,$35,$B0,$97,$B0,$42,$B1,$4C,$B1; 28:85CC
    .db $66,$B1,$88,$B1,$CB,$B0,$57,$B2,$45,$B2,$5A,$B2,$74,$B2,$A6,$B2; 28:85DC
    .db $A9,$B2,$CB,$B2,$D9,$B2,$DB,$B3,$7D,$B4,$AF,$B4,$01,$B5,$C3,$B5; 28:85EC
    .db $05,$B6,$97,$B6,$E5,$B6,$41,$B7,$A3,$B7,$D9,$B7,$3B,$B8,$45,$B8; 28:85FC
    .db $87,$B8,$C9,$B8,$0B,$B9,$15,$B9,$47,$B9,$A9,$B9,$FB,$B9,$A1,$BA; 28:860C
    .db $CF,$BA,$59,$BB,$BB,$BB,$09,$BC,$23,$BC,$55,$BC,$5B,$BC,$5E,$BC; 28:861C
    .db $61,$BC,$7F,$BC,$9D,$BC,$5F,$BD,$F1,$BD,$33,$BE,$FF,$B6,$00,$B1; 28:862C
    .db $8F,$BA,$2D,$BA,$6F,$8D,$71,$8E,$B3,$8E,$35,$8F,$4B,$8F,$61,$8F; 28:863C
    .db $77,$8F,$8D,$8F,$D7,$8F,$E5,$8F,$EF,$8F,$11,$90,$1B,$90,$41,$90; 28:864C
    .db $06,$01,$F8,$E9,$00,$1A,$00,$E9,$00,$1B,$F8,$F1,$00,$00,$00,$F1; 28:865C
    .db $00,$01,$F8,$F9,$04,$20,$00,$F9,$04,$21,$F8,$E9,$00,$1A,$00,$E9; 28:866C
    .db $00,$1B,$00,$F1,$40,$00,$F8,$F1,$40,$01,$00,$F9,$44,$20,$F8,$F9; 28:867C
    .db $44,$21,$06,$01,$F8,$E9,$00,$1E,$00,$E9,$00,$1F,$F8,$F1,$00,$06; 28:868C
    .db $00,$F1,$00,$07,$F8,$F9,$04,$26,$00,$F9,$04,$27,$F8,$E9,$00,$1E; 28:869C
    .db $00,$E9,$00,$1F,$00,$F1,$40,$06,$F8,$F1,$40,$07,$00,$F9,$44,$26; 28:86AC
    .db $F8,$F9,$44,$27,$06,$01,$00,$E9,$40,$1C,$F8,$E9,$40,$1D,$00,$F1; 28:86BC
    .db $40,$02,$F8,$F1,$40,$03,$00,$F9,$44,$22,$F8,$F9,$44,$23,$00,$EA; 28:86CC
    .db $40,$1C,$F8,$EA,$40,$1D,$00,$F2,$40,$02,$F8,$F2,$40,$03,$00,$FA; 28:86DC
    .db $44,$24,$F8,$FA,$44,$25,$FF,$C0,$86,$02,$01,$00,$F0,$00,$70,$00; 28:86EC
    .db $F8,$80,$70,$00,$F1,$00,$70,$00,$F9,$80,$70,$02,$01,$F8,$F2,$00; 28:86FC
    .db $72,$F8,$FA,$00,$73,$F8,$F1,$00,$72,$F8,$F9,$00,$73,$03,$01,$02; 28:870C
    .db $F0,$40,$71,$02,$F8,$C0,$71,$FD,$F2,$40,$04,$02,$F1,$40,$71,$02; 28:871C
    .db $F9,$C0,$71,$FD,$F3,$40,$04,$FF,$19,$87,$F6,$F9,$80,$71,$FF,$3D; 28:872C
    .db $87,$01,$01,$FD,$F2,$40,$04,$FA,$F3,$40,$05,$00,$00,$00,$00,$08; 28:873C
    .db $03,$F8,$EC,$00,$1E,$00,$EC,$00,$1F,$FC,$E5,$C0,$53,$F8,$F1,$00; 28:874C
    .db $16,$00,$F1,$00,$17,$F8,$F9,$04,$36,$00,$F9,$04,$27,$80,$80,$80; 28:875C
    .db $80,$F8,$E9,$00,$3C,$00,$E9,$00,$3D,$FD,$E2,$00,$59,$FC,$DA,$C0; 28:876C
    .db $53,$F8,$F1,$00,$18,$00,$F1,$00,$19,$F8,$F9,$04,$38,$00,$F9,$04; 28:877C
    .db $27,$F8,$EC,$00,$1E,$00,$EC,$00,$1F,$03,$F6,$40,$50,$03,$FE,$40; 28:878C
    .db $51,$F8,$F1,$00,$14,$00,$F1,$00,$15,$F8,$F9,$04,$34,$00,$F9,$04; 28:879C
    .db $35,$80,$80,$80,$80,$02,$03,$F4,$F0,$00,$71,$F4,$F8,$80,$71,$F8; 28:87AC
    .db $F1,$00,$70,$F8,$F9,$80,$70,$F3,$EE,$00,$71,$F3,$F6,$80,$71,$80; 28:87BC
    .db $80,$80,$80,$08,$03,$F8,$E9,$00,$1A,$00,$E9,$00,$1B,$F8,$F9,$00; 28:87CC
    .db $56,$F8,$01,$00,$57,$F8,$F1,$00,$0A,$00,$F1,$00,$0B,$F8,$F9,$04; 28:87DC
    .db $28,$00,$F9,$04,$2B,$F8,$EC,$00,$3A,$00,$EC,$00,$3B,$FC,$FE,$00; 28:87EC
    .db $52,$FC,$06,$00,$53,$F8,$F1,$00,$0C,$00,$F1,$00,$0D,$F8,$F9,$04; 28:87FC
    .db $2C,$00,$F9,$04,$2D,$F8,$E9,$00,$1A,$00,$E9,$00,$1B,$F4,$F1,$00; 28:880C
    .db $50,$F4,$F9,$00,$51,$F8,$F1,$00,$08,$00,$F1,$00,$09,$F8,$F9,$04; 28:881C
    .db $28,$00,$F9,$04,$21,$80,$80,$80,$80,$02,$03,$04,$EE,$40,$71,$04; 28:882C
    .db $F6,$C0,$71,$04,$EB,$40,$71,$04,$F3,$C0,$71,$04,$EF,$40,$71,$04; 28:883C
    .db $F7,$C0,$71,$80,$80,$80,$80,$FF,$59,$88,$FF,$BF,$88,$08,$03,$00; 28:884C
    .db $EC,$40,$1C,$F8,$EC,$40,$1D,$02,$F4,$40,$54,$00,$F1,$40,$10,$F8; 28:885C
    .db $F1,$40,$11,$00,$F9,$44,$30,$F8,$F9,$44,$31,$80,$80,$80,$80,$02; 28:886C
    .db $EB,$40,$1C,$FA,$EB,$40,$1D,$0F,$F2,$40,$54,$07,$F2,$40,$55,$00; 28:887C
    .db $F1,$40,$12,$F8,$F1,$40,$13,$00,$F9,$44,$30,$F8,$F9,$44,$33,$FF; 28:888C
    .db $ED,$40,$1C,$F7,$ED,$40,$1D,$F3,$FA,$40,$5A,$00,$F1,$40,$0E,$F8; 28:889C
    .db $F1,$40,$0F,$00,$F9,$44,$2E,$F8,$F9,$44,$2F,$80,$80,$80,$80,$80; 28:88AC
    .db $80,$80,$80,$02,$03,$F8,$F2,$00,$70,$F8,$FA,$80,$70,$F4,$EF,$00; 28:88BC
    .db $71,$F4,$F7,$80,$71,$FA,$F3,$00,$70,$FA,$FB,$80,$70,$80,$80,$80; 28:88CC
    .db $80,$01,$00,$FC,$F4,$01,$29,$02,$00,$FC,$F0,$01,$3F,$FC,$F8,$01; 28:88DC
    .db $3F,$02,$00,$00,$F4,$41,$39,$F8,$F4,$41,$39,$02,$00,$FC,$08,$81; 28:88EC
    .db $3F,$FC,$00,$81,$3F,$FF,$ED,$88,$04,$00,$F8,$F0,$01,$69,$00,$F0; 28:88FC
    .db $01,$6A,$F8,$F8,$01,$6B,$00,$F8,$01,$6C,$06,$01,$F8,$00,$01,$6D; 28:890C
    .db $00,$00,$01,$6D,$F8,$08,$01,$6E,$00,$08,$01,$6E,$F8,$10,$01,$6F; 28:891C
    .db $00,$10,$01,$6F,$F8,$00,$41,$6D,$00,$00,$41,$6D,$F8,$08,$41,$6E; 28:892C
    .db $00,$08,$41,$6E,$F8,$10,$41,$6F,$00,$10,$41,$6F,$06,$01,$E0,$F0; 28:893C
    .db $01,$6F,$E8,$F0,$01,$6E,$F0,$F0,$01,$6D,$E0,$F8,$01,$6F,$E8,$F8; 28:894C
    .db $01,$6E,$F0,$F8,$01,$6D,$E0,$F0,$41,$6F,$E8,$F0,$41,$6E,$F0,$F0; 28:895C
    .db $41,$6D,$E0,$F8,$41,$6F,$E8,$F8,$41,$6E,$F0,$F8,$41,$6D,$06,$01; 28:896C
    .db $F8,$D8,$01,$6F,$00,$D8,$01,$6F,$F8,$E0,$01,$6E,$00,$E0,$01,$6E; 28:897C
    .db $F8,$E8,$01,$6D,$00,$E8,$01,$6D,$F8,$D8,$41,$6F,$00,$D8,$41,$6F; 28:898C
    .db $F8,$E0,$41,$6E,$00,$E0,$41,$6E,$F8,$E8,$41,$6D,$00,$E8,$41,$6D; 28:899C
    .db $FF,$48,$89,$04,$01,$F8,$F0,$01,$64,$00,$F0,$01,$65,$F8,$F8,$01; 28:89AC
    .db $66,$00,$F8,$01,$67,$00,$F0,$41,$64,$F8,$F0,$41,$65,$00,$F8,$41; 28:89BC
    .db $66,$F8,$F8,$41,$67,$02,$03,$FC,$F4,$01,$6C,$80,$80,$80,$80,$FC; 28:89CC
    .db $F4,$01,$6B,$80,$80,$80,$80,$FC,$F4,$01,$68,$80,$80,$80,$80,$F8; 28:89DC
    .db $F4,$01,$64,$00,$F4,$01,$65,$04,$00,$F8,$F0,$01,$66,$00,$F0,$01; 28:89EC
    .db $68,$F8,$F8,$01,$67,$00,$F8,$01,$69,$0E,$07,$F8,$E0,$01,$6C,$08; 28:89FC
    .db $E8,$01,$6E,$F0,$F0,$01,$6D,$00,$F8,$41,$6F,$80,$80,$80,$80,$80; 28:8A0C
    .db $80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80; 28:8A1C
    .db $80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80; 28:8A2C
    .db $80,$80,$80,$F8,$E0,$01,$6C,$00,$E0,$01,$6E,$F0,$E8,$01,$6C,$08; 28:8A3C
    .db $E8,$01,$6E,$F0,$F0,$01,$6D,$08,$F0,$01,$6F,$F0,$F8,$01,$6D,$F8; 28:8A4C
    .db $F8,$41,$6D,$00,$F8,$41,$6F,$08,$F8,$01,$6F,$80,$80,$80,$80,$80; 28:8A5C
    .db $80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$F8,$E0,$01,$6B,$00; 28:8A6C
    .db $E0,$01,$6A,$F0,$E8,$01,$66,$F8,$E8,$41,$6C,$00,$E8,$41,$6E,$08; 28:8A7C
    .db $E8,$01,$68,$F0,$F0,$01,$6B,$F8,$F0,$01,$6D,$00,$F0,$01,$6F,$08; 28:8A8C
    .db $F0,$01,$6B,$F0,$F8,$01,$67,$F8,$F8,$41,$6A,$00,$F8,$41,$68,$08; 28:8A9C
    .db $F8,$01,$69,$F8,$E0,$01,$6A,$00,$E0,$01,$6B,$F0,$E8,$01,$66,$F8; 28:8AAC
    .db $E8,$01,$6C,$00,$E8,$01,$6A,$08,$E8,$01,$68,$F0,$F0,$01,$6A,$F8; 28:8ABC
    .db $F0,$01,$6B,$00,$F0,$01,$6F,$08,$F0,$01,$6A,$F0,$F8,$01,$67,$F8; 28:8ACC
    .db $F8,$01,$6B,$00,$F8,$01,$68,$08,$F8,$01,$69,$F8,$E0,$01,$6B,$00; 28:8ADC
    .db $E0,$01,$6A,$F0,$E8,$01,$66,$F8,$E8,$41,$6A,$00,$E8,$41,$6B,$08; 28:8AEC
    .db $E8,$01,$68,$F0,$F0,$01,$6B,$F8,$F0,$01,$6A,$00,$F0,$01,$6B,$08; 28:8AFC
    .db $F0,$01,$6B,$F0,$F8,$01,$67,$F8,$F8,$41,$6A,$00,$F8,$41,$68,$08; 28:8B0C
    .db $F8,$01,$69,$F4,$E8,$01,$66,$FC,$E8,$01,$6A,$04,$E8,$01,$68,$F4; 28:8B1C
    .db $F0,$01,$6A,$FC,$F0,$01,$6B,$04,$F0,$01,$68,$F4,$F8,$01,$67,$FC; 28:8B2C
    .db $F8,$01,$6B,$04,$F8,$01,$69,$80,$80,$80,$80,$80,$80,$80,$80,$80; 28:8B3C
    .db $80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$F8,$F0,$01,$66,$00; 28:8B4C
    .db $F0,$01,$68,$F8,$F8,$01,$67,$00,$F8,$01,$69,$80,$80,$80,$80,$80; 28:8B5C
    .db $80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80; 28:8B6C
    .db $80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80,$80; 28:8B7C
    .db $80,$80,$80,$F8,$F8,$01,$64,$00,$F8,$01,$65,$80,$80,$80,$80,$04; 28:8B8C
    .db $0F,$DA,$0A,$01,$5B,$22,$E4,$01,$5B,$1E,$13,$01,$5B,$80,$80,$80; 28:8B9C
    .db $80,$DC,$07,$01,$5B,$20,$E3,$01,$5B,$1C,$11,$01,$5B,$80,$80,$80; 28:8BAC
    .db $80,$DE,$05,$01,$5B,$1E,$E3,$01,$5B,$1A,$10,$01,$5B,$80,$80,$80; 28:8BBC
    .db $80,$E0,$00,$01,$5B,$1C,$E4,$01,$5B,$18,$0C,$01,$5B,$80,$80,$80; 28:8BCC
    .db $80,$E8,$D5,$01,$5B,$E2,$FA,$01,$5B,$1A,$E6,$01,$5B,$16,$09,$01; 28:8BDC
    .db $5B,$EA,$D4,$01,$5B,$E4,$F7,$01,$5B,$18,$E8,$01,$5B,$14,$05,$01; 28:8BEC
    .db $5B,$EC,$D8,$01,$5B,$E6,$F9,$01,$5B,$16,$E7,$01,$5B,$12,$02,$01; 28:8BFC
    .db $5B,$EE,$D6,$01,$5B,$E8,$FF,$01,$5B,$14,$E6,$01,$5B,$10,$00,$01; 28:8C0C
    .db $5B,$F0,$D7,$01,$5B,$EA,$F7,$01,$5B,$12,$E5,$01,$5B,$0E,$FE,$01; 28:8C1C
    .db $5B,$F2,$D0,$01,$5B,$EC,$F0,$01,$5B,$10,$E5,$01,$5B,$0C,$FD,$01; 28:8C2C
    .db $5B,$F4,$E0,$01,$5B,$EE,$EB,$01,$5B,$0E,$E5,$01,$5B,$0A,$FD,$01; 28:8C3C
    .db $5B,$F6,$DA,$01,$5B,$F0,$E9,$01,$5B,$0C,$E5,$01,$5B,$08,$FC,$01; 28:8C4C
    .db $5B,$F8,$D8,$01,$5B,$F2,$EA,$01,$5B,$0A,$E6,$01,$5B,$06,$FC,$01; 28:8C5C
    .db $5B,$FA,$D9,$01,$5B,$F4,$EB,$01,$5B,$08,$E8,$01,$5B,$04,$FB,$01; 28:8C6C
    .db $5B,$FC,$DD,$01,$5B,$F6,$F0,$01,$5B,$06,$EC,$01,$5B,$02,$FB,$01; 28:8C7C
    .db $5B,$FE,$E8,$01,$5B,$F8,$F5,$01,$5B,$04,$F0,$01,$5B,$00,$FB,$01; 28:8C8C
    .db $5B,$02,$00,$FC,$F0,$01,$60,$FC,$F8,$01,$61,$02,$00,$F8,$F4,$41; 28:8C9C
    .db $63,$00,$F4,$41,$62,$02,$00,$FC,$F8,$81,$60,$FC,$F0,$81,$61,$FF; 28:8CAC
    .db $A7,$8C,$06,$00,$F8,$E8,$01,$66,$00,$E8,$41,$66,$F0,$E8,$01,$65; 28:8CBC
    .db $08,$E8,$41,$65,$F8,$E0,$01,$64,$00,$E0,$41,$64,$FF,$F5,$8C,$06; 28:8CCC
    .db $00,$F8,$00,$81,$66,$00,$00,$C1,$66,$F0,$00,$81,$65,$08,$00,$C1; 28:8CDC
    .db $65,$F8,$08,$81,$64,$00,$08,$C1,$64,$06,$00,$08,$F0,$41,$69,$08; 28:8CEC
    .db $F8,$C1,$69,$08,$00,$C1,$67,$08,$E8,$41,$67,$10,$F0,$41,$64,$10; 28:8CFC
    .db $F8,$C1,$64,$04,$00,$F8,$F0,$01,$6D,$00,$F0,$41,$6D,$F8,$F8,$81; 28:8D0C
    .db $6D,$00,$F8,$C1,$6D,$03,$00,$FC,$F0,$01,$62,$FC,$F8,$01,$63,$FC; 28:8D1C
    .db $00,$01,$64,$03,$00,$F8,$F4,$01,$65,$00,$F4,$01,$66,$08,$F4,$01; 28:8D2C
    .db $67,$06,$01,$F8,$F0,$00,$A0,$00,$F0,$00,$A1,$00,$F0,$00,$A1,$F8; 28:8D3C
    .db $F8,$04,$A2,$00,$F8,$04,$A3,$00,$F8,$04,$A3,$F8,$F0,$00,$A4,$00; 28:8D4C
    .db $F0,$00,$A5,$00,$F0,$00,$A5,$F8,$F8,$04,$A6,$00,$F8,$04,$A7,$00; 28:8D5C
    .db $F8,$04,$A7,$10,$03,$93,$40,$42,$A8,$9B,$38,$42,$A8,$A3,$38,$02; 28:8D6C
    .db $AA,$AB,$38,$42,$A9,$E3,$58,$42,$A9,$DB,$50,$02,$A9,$E3,$48,$42; 28:8D7C
    .db $A8,$E3,$40,$02,$A8,$13,$58,$02,$A9,$13,$50,$C2,$A8,$0B,$48,$02; 28:8D8C
    .db $A8,$1B,$48,$42,$A9,$63,$40,$02,$A8,$5B,$38,$02,$A8,$80,$80,$80; 28:8D9C
    .db $80,$80,$80,$80,$80,$AB,$30,$42,$A8,$B3,$28,$42,$A9,$B3,$20,$42; 28:8DAC
    .db $A8,$BB,$18,$42,$A8,$D3,$40,$42,$A9,$D3,$38,$42,$A8,$DB,$38,$02; 28:8DBC
    .db $A8,$DB,$30,$42,$A8,$13,$48,$02,$A8,$0B,$40,$42,$A8,$13,$40,$C2; 28:8DCC
    .db $A8,$13,$38,$42,$B4,$5B,$30,$42,$A9,$53,$30,$02,$AA,$4B,$28,$02; 28:8DDC
    .db $A8,$43,$30,$43,$A8,$CB,$20,$42,$A9,$CB,$18,$42,$A8,$C3,$10,$42; 28:8DEC
    .db $A8,$CB,$10,$02,$A8,$DB,$28,$42,$A8,$E3,$28,$02,$A8,$E3,$30,$02; 28:8DFC
    .db $AA,$EB,$28,$42,$A9,$1B,$38,$02,$A9,$13,$30,$42,$A8,$1B,$30,$02; 28:8E0C
    .db $A8,$1B,$28,$42,$A9,$43,$28,$42,$A8,$43,$20,$02,$A8,$3B,$20,$02; 28:8E1C
    .db $AA,$33,$18,$C2,$A8,$D3,$10,$42,$A8,$D3,$08,$02,$A8,$D3,$00,$42; 28:8E2C
    .db $A8,$DB,$F8,$42,$A8,$E3,$20,$02,$A8,$E3,$18,$42,$A8,$E3,$10,$C2; 28:8E3C
    .db $A8,$EB,$10,$42,$A8,$13,$20,$C2,$A8,$0B,$20,$02,$A9,$13,$18,$42; 28:8E4C
    .db $A9,$0B,$10,$02,$A8,$2B,$10,$02,$A9,$2B,$08,$C2,$A8,$23,$00,$02; 28:8E5C
    .db $A8,$1B,$F8,$C2,$A8,$02,$07,$FF,$F0,$03,$A0,$FF,$F8,$03,$A0,$FF; 28:8E6C
    .db $F1,$03,$A3,$FC,$F7,$03,$A3,$FF,$F2,$03,$A2,$FA,$F7,$03,$A2,$FF; 28:8E7C
    .db $F2,$03,$A4,$F9,$F5,$03,$A4,$F8,$F7,$03,$A1,$00,$F7,$03,$A1,$F9; 28:8E8C
    .db $F3,$83,$A4,$FF,$F7,$83,$A4,$FA,$F1,$43,$A2,$FF,$F7,$43,$A2,$FC; 28:8E9C
    .db $F0,$83,$A3,$FF,$F7,$83,$A3,$04,$07,$FC,$E0,$03,$FE,$FC,$E8,$03; 28:8EAC
    .db $FB,$FC,$F0,$03,$FB,$FC,$F8,$03,$FB,$10,$E8,$03,$FE,$0A,$EE,$03; 28:8EBC
    .db $FC,$04,$F4,$03,$FC,$FE,$FA,$03,$FC,$18,$FC,$03,$FE,$10,$FC,$03; 28:8ECC
    .db $FD,$08,$FC,$03,$FD,$00,$FC,$03,$FD,$10,$10,$03,$FE,$0A,$0A,$43; 28:8EDC
    .db $FC,$04,$04,$43,$FC,$FE,$FE,$43,$FC,$FC,$18,$03,$FE,$FC,$10,$03; 28:8EEC
    .db $FB,$FC,$08,$03,$FB,$FC,$00,$03,$FB,$E8,$10,$03,$FE,$EE,$0A,$03; 28:8EFC
    .db $FC,$F4,$04,$03,$FC,$FA,$FE,$03,$FC,$E0,$FC,$03,$FE,$E8,$FC,$03; 28:8F0C
    .db $FD,$F0,$FC,$03,$FD,$F8,$FC,$03,$FD,$E8,$E8,$03,$FE,$EE,$EE,$43; 28:8F1C
    .db $FC,$F4,$F4,$43,$FC,$FA,$FA,$43,$FC,$05,$00,$FC,$EC,$03,$FE,$FC; 28:8F2C
    .db $F6,$03,$FB,$FC,$00,$03,$FB,$FC,$0A,$03,$FB,$FC,$14,$03,$FB,$05; 28:8F3C
    .db $00,$12,$FC,$03,$FE,$08,$FC,$03,$FD,$FE,$FC,$03,$FD,$F4,$FC,$03; 28:8F4C
    .db $FD,$EA,$FC,$03,$FD,$05,$00,$FC,$12,$03,$FE,$FC,$08,$03,$FB,$FC; 28:8F5C
    .db $FE,$03,$FB,$FC,$F4,$03,$FB,$FC,$EA,$03,$FB,$05,$00,$EC,$FC,$03; 28:8F6C
    .db $FE,$F6,$FC,$03,$FD,$00,$FC,$03,$FD,$0A,$FC,$03,$FD,$14,$FC,$03; 28:8F7C
    .db $FD,$09,$01,$F0,$F0,$03,$99,$08,$F0,$43,$99,$F0,$F8,$03,$9A,$F8; 28:8F8C
    .db $F8,$03,$9B,$00,$F8,$43,$9B,$08,$F8,$43,$9A,$80,$80,$80,$80,$80; 28:8F9C
    .db $80,$80,$80,$80,$80,$80,$80,$F4,$EC,$01,$A0,$FC,$EC,$01,$A1,$04; 28:8FAC
    .db $EC,$41,$A0,$F4,$F4,$01,$A2,$FC,$F4,$01,$A3,$04,$F4,$41,$A2,$F4; 28:8FBC
    .db $FC,$81,$A0,$FC,$FC,$81,$A1,$04,$FC,$C1,$A0,$03,$00,$FC,$E8,$03; 28:8FCC
    .db $D8,$FC,$F0,$03,$D6,$FC,$F8,$03,$D5,$02,$00,$01,$EF,$01,$AC,$01; 28:8FDC
    .db $F7,$01,$AD,$02,$03,$01,$EF,$02,$AB,$01,$F7,$82,$AB,$01,$EF,$02; 28:8FEC
    .db $AA,$01,$F7,$82,$AA,$01,$EF,$02,$A9,$01,$F7,$82,$A9,$80,$80,$80; 28:8FFC
    .db $80,$80,$80,$80,$80,$01,$01,$FC,$F7,$00,$F9,$FC,$F7,$03,$F9,$03; 28:900C
    .db $02,$FC,$E8,$80,$53,$FC,$F0,$80,$52,$FC,$F8,$80,$AD,$FC,$E8,$00; 28:901C
    .db $AD,$FC,$F0,$00,$52,$FC,$F8,$00,$53,$FC,$F0,$00,$AD,$FC,$F8,$00; 28:902C
    .db $52,$80,$80,$80,$80,$09,$07,$F3,$E8,$01,$A4,$F3,$E8,$00,$A5,$05; 28:903C
    .db $E8,$01,$A5,$FC,$F0,$00,$A6,$F3,$F8,$01,$A7,$05,$F8,$00,$A7,$EC; 28:904C
    .db $00,$01,$A8,$0C,$00,$00,$A8,$80,$80,$80,$80,$E6,$E8,$01,$A4,$E6; 28:905C
    .db $E8,$00,$A5,$12,$E8,$01,$A5,$EC,$F0,$00,$A6,$0C,$F0,$01,$A6,$F3; 28:906C
    .db $F8,$00,$A7,$05,$F8,$01,$A7,$04,$00,$00,$A8,$80,$80,$80,$80,$E6; 28:907C
    .db $E8,$01,$A4,$E6,$E8,$00,$A5,$12,$E8,$01,$A5,$E4,$F0,$00,$A6,$14; 28:908C
    .db $F0,$01,$A6,$E6,$F8,$00,$A7,$12,$F8,$01,$A7,$EC,$00,$00,$A8,$0C; 28:909C
    .db $00,$01,$A8,$F3,$E8,$01,$A4,$F3,$E8,$00,$A5,$05,$E8,$01,$A5,$EC; 28:90AC
    .db $F0,$00,$A6,$0C,$F0,$01,$A6,$E6,$F8,$00,$A7,$12,$F8,$01,$A7,$E4; 28:90BC
    .db $00,$00,$A8,$14,$00,$01,$A8,$05,$E8,$01,$A4,$F3,$E8,$01,$A5,$05; 28:90CC
    .db $E8,$00,$A5,$FC,$F0,$00,$A6,$F3,$F8,$00,$A7,$05,$F8,$01,$A7,$EC; 28:90DC
    .db $00,$00,$A8,$0C,$00,$01,$A8,$80,$80,$80,$80,$12,$E8,$01,$A4,$E6; 28:90EC
    .db $E8,$01,$A5,$12,$E8,$00,$A5,$EC,$F0,$01,$A6,$0C,$F0,$00,$A6,$F3; 28:90FC
    .db $F8,$01,$A7,$05,$F8,$00,$A7,$04,$00,$00,$A8,$80,$80,$80,$80,$12; 28:910C
    .db $E8,$01,$A4,$E6,$E8,$01,$A5,$12,$E8,$00,$A5,$E4,$F0,$01,$A6,$14; 28:911C
    .db $F0,$00,$A6,$E6,$F8,$01,$A7,$12,$F8,$00,$A7,$EC,$00,$01,$A8,$0C; 28:912C
    .db $00,$00,$A8,$05,$E8,$01,$A4,$F3,$E8,$01,$A5,$05,$E8,$00,$A5,$EC; 28:913C
    .db $F0,$01,$A6,$0C,$F0,$00,$A6,$E6,$F8,$01,$A7,$12,$F8,$00,$A7,$E4; 28:914C
    .db $00,$01,$A8,$14,$00,$00,$A8,$00,$00,$00,$00,$00,$00,$06,$01,$F8; 28:915C
    .db $E9,$02,$83,$00,$E9,$42,$83,$F8,$F1,$02,$84,$00,$F1,$02,$85,$F8; 28:916C
    .db $F9,$06,$8C,$00,$F9,$06,$8D,$F8,$E9,$02,$83,$00,$E9,$42,$83,$F8; 28:917C
    .db $F1,$42,$85,$00,$F1,$42,$84,$F8,$F9,$46,$8D,$00,$F9,$46,$8C,$06; 28:918C
    .db $01,$F8,$E9,$42,$87,$00,$E9,$42,$86,$F8,$F1,$42,$89,$00,$F1,$42; 28:919C
    .db $88,$F8,$F9,$46,$8F,$00,$F9,$46,$8E,$F8,$E9,$42,$87,$00,$E9,$42; 28:91AC
    .db $86,$F8,$F1,$42,$8B,$00,$F1,$42,$8A,$F8,$F9,$46,$B9,$00,$F9,$46; 28:91BC
    .db $B8,$06,$01,$F8,$E9,$02,$80,$00,$E9,$42,$80,$F8,$F1,$02,$81,$00; 28:91CC
    .db $F1,$02,$82,$F8,$F9,$06,$8C,$00,$F9,$06,$8D,$F8,$E9,$02,$80,$00; 28:91DC
    .db $E9,$42,$80,$F8,$F1,$42,$82,$00,$F1,$42,$81,$F8,$F9,$46,$8D,$00; 28:91EC
    .db $F9,$46,$8C,$FF,$9B,$91,$06,$01,$F8,$E9,$03,$93,$00,$E9,$43,$93; 28:91FC
    .db $F8,$F1,$03,$94,$00,$F1,$03,$95,$F8,$F9,$07,$9C,$00,$F9,$07,$9D; 28:920C
    .db $F8,$E9,$03,$93,$00,$E9,$43,$93,$F8,$F1,$43,$95,$00,$F1,$43,$94; 28:921C
    .db $F8,$F9,$47,$9D,$00,$F9,$47,$9C,$06,$01,$F8,$E9,$43,$97,$00,$E9; 28:922C
    .db $43,$96,$F8,$F1,$43,$99,$00,$F1,$43,$98,$F8,$F9,$47,$9F,$00,$F9; 28:923C
    .db $47,$9E,$F8,$E9,$43,$97,$00,$E9,$43,$96,$F8,$F1,$43,$9B,$00,$F1; 28:924C
    .db $43,$9A,$F8,$F9,$47,$BB,$00,$F9,$47,$BA,$06,$01,$F8,$E9,$03,$90; 28:925C
    .db $00,$E9,$43,$90,$F8,$F1,$03,$91,$00,$F1,$03,$92,$F8,$F9,$07,$9C; 28:926C
    .db $00,$F9,$07,$9D,$F8,$E9,$03,$90,$00,$E9,$43,$90,$F8,$F1,$43,$92; 28:927C
    .db $00,$F1,$43,$91,$F8,$F9,$47,$9D,$00,$F9,$47,$9C,$FF,$34,$92,$06; 28:928C
    .db $01,$F8,$E9,$02,$A3,$00,$E9,$42,$A3,$F8,$F1,$02,$A4,$00,$F1,$02; 28:929C
    .db $A5,$F8,$F9,$06,$AC,$00,$F9,$06,$AD,$F8,$E9,$02,$A3,$00,$E9,$42; 28:92AC
    .db $A3,$F8,$F1,$42,$A5,$00,$F1,$42,$A4,$F8,$F9,$46,$AD,$00,$F9,$46; 28:92BC
    .db $AC,$06,$01,$F8,$E9,$42,$A7,$00,$E9,$42,$A6,$F8,$F1,$42,$A9,$00; 28:92CC
    .db $F1,$42,$A8,$F8,$F9,$46,$AF,$00,$F9,$46,$AE,$F8,$E9,$42,$A7,$00; 28:92DC
    .db $E9,$42,$A6,$F8,$F1,$42,$AB,$00,$F1,$42,$AA,$F8,$F9,$46,$BD,$00; 28:92EC
    .db $F9,$46,$BC,$06,$01,$F8,$E9,$02,$A0,$00,$E9,$42,$A0,$F8,$F1,$02; 28:92FC
    .db $A1,$00,$F1,$02,$A2,$F8,$F9,$06,$AC,$00,$F9,$06,$AD,$F8,$E9,$02; 28:930C
    .db $A0,$00,$E9,$42,$A0,$F8,$F1,$42,$A2,$00,$F1,$42,$A1,$F8,$F9,$46; 28:931C
    .db $AD,$00,$F9,$46,$AC,$FF,$CD,$92,$06,$01,$F8,$E9,$03,$83,$00,$E9; 28:932C
    .db $43,$83,$F8,$F1,$03,$B2,$00,$F1,$03,$B3,$F8,$F9,$07,$8C,$00,$F9; 28:933C
    .db $07,$8D,$F8,$E9,$03,$83,$00,$E9,$43,$83,$F8,$F1,$43,$B3,$00,$F1; 28:934C
    .db $43,$B2,$F8,$F9,$47,$8D,$00,$F9,$47,$8C,$06,$01,$F8,$E9,$43,$87; 28:935C
    .db $00,$E9,$43,$86,$F8,$F1,$43,$B5,$00,$F1,$43,$B4,$F8,$F9,$47,$8F; 28:936C
    .db $00,$F9,$47,$8E,$F8,$E9,$43,$87,$00,$E9,$43,$86,$F8,$F1,$43,$B7; 28:937C
    .db $00,$F1,$43,$B6,$F8,$F9,$47,$B9,$00,$F9,$47,$B8,$06,$01,$F8,$E9; 28:938C
    .db $03,$80,$00,$E9,$43,$80,$F8,$F1,$03,$B0,$00,$F1,$03,$B1,$F8,$F9; 28:939C
    .db $07,$8C,$00,$F9,$07,$8D,$F8,$E9,$03,$80,$00,$E9,$43,$80,$F8,$F1; 28:93AC
    .db $43,$B1,$00,$F1,$43,$B0,$F8,$F9,$47,$8D,$00,$F9,$47,$8C,$FF,$66; 28:93BC
    .db $93,$06,$01,$F8,$E8,$00,$83,$00,$E8,$40,$83,$F8,$F0,$00,$B2,$00; 28:93CC
    .db $F0,$00,$B3,$F8,$F8,$04,$8C,$00,$F8,$04,$8D,$F8,$E8,$00,$83,$00; 28:93DC
    .db $E8,$40,$83,$F8,$F0,$40,$B3,$00,$F0,$40,$B2,$F8,$F8,$44,$8D,$00; 28:93EC
    .db $F8,$44,$8C,$06,$01,$F8,$E8,$40,$87,$00,$E8,$40,$86,$F8,$F0,$40; 28:93FC
    .db $B5,$00,$F0,$40,$B4,$F8,$F8,$44,$8F,$00,$F8,$44,$8E,$F8,$E8,$40; 28:940C
    .db $87,$00,$E8,$40,$86,$F8,$F0,$40,$B7,$00,$F0,$40,$B6,$F8,$F8,$44; 28:941C
    .db $B9,$00,$F8,$44,$B8,$06,$01,$F8,$E8,$00,$80,$00,$E8,$40,$80,$F8; 28:942C
    .db $F0,$00,$B0,$00,$F0,$00,$B1,$F8,$F8,$04,$8C,$00,$F8,$04,$8D,$F8; 28:943C
    .db $E8,$00,$80,$00,$E8,$40,$80,$F8,$F0,$40,$B1,$00,$F0,$40,$B0,$F8; 28:944C
    .db $F8,$44,$8D,$00,$F8,$44,$8C,$FF,$FF,$93,$06,$00,$F8,$E8,$02,$80; 28:945C
    .db $00,$E8,$02,$81,$F8,$F0,$02,$82,$00,$F0,$02,$83,$F8,$F8,$06,$84; 28:946C
    .db $00,$F8,$06,$85,$06,$00,$F8,$E8,$03,$90,$00,$E8,$03,$91,$F8,$F0; 28:947C
    .db $03,$96,$00,$F0,$03,$97,$F8,$F8,$07,$94,$00,$F8,$07,$95,$04,$01; 28:948C
    .db $F8,$F0,$02,$89,$00,$F0,$42,$89,$F8,$F8,$06,$92,$00,$F8,$06,$93; 28:949C
    .db $F8,$F0,$02,$89,$00,$F0,$42,$89,$F8,$F8,$46,$93,$00,$F8,$46,$92; 28:94AC
    .db $04,$01,$F8,$F0,$42,$8D,$00,$F0,$42,$8C,$F8,$F8,$46,$95,$00,$F8; 28:94BC
    .db $46,$94,$F8,$F0,$42,$8D,$00,$F0,$42,$8C,$F8,$F8,$46,$97,$00,$F8; 28:94CC
    .db $46,$96,$04,$01,$F8,$F0,$02,$88,$00,$F0,$42,$88,$F8,$F8,$06,$90; 28:94DC
    .db $00,$F8,$06,$91,$F8,$F0,$02,$88,$00,$F0,$42,$88,$F8,$F8,$46,$91; 28:94EC
    .db $00,$F8,$46,$90,$FF,$BC,$94,$04,$01,$F8,$F0,$02,$8B,$00,$F0,$42; 28:94FC
    .db $8B,$F8,$F8,$06,$92,$00,$F8,$06,$93,$F8,$F0,$02,$8B,$00,$F0,$42; 28:950C
    .db $8B,$F8,$F8,$46,$93,$00,$F8,$46,$92,$04,$01,$F8,$F0,$42,$8F,$00; 28:951C
    .db $F0,$42,$8E,$F8,$F8,$46,$95,$00,$F8,$46,$94,$F8,$F0,$42,$8F,$00; 28:952C
    .db $F0,$42,$8E,$F8,$F8,$46,$97,$00,$F8,$46,$96,$04,$01,$F8,$F0,$02; 28:953C
    .db $8A,$00,$F0,$42,$8A,$F8,$F8,$06,$90,$00,$F8,$06,$91,$F8,$F0,$02; 28:954C
    .db $8A,$00,$F0,$42,$8A,$F8,$F8,$46,$91,$00,$F8,$46,$90,$FF,$25,$95; 28:955C
    .db $04,$01,$F8,$F0,$02,$B4,$00,$F0,$02,$B5,$F8,$F8,$06,$B6,$00,$F8; 28:956C
    .db $06,$B7,$F8,$F0,$42,$B5,$00,$F0,$42,$B4,$F8,$F8,$46,$B7,$00,$F8; 28:957C
    .db $46,$B6,$04,$01,$F8,$F0,$42,$B9,$00,$F0,$42,$B8,$F8,$F8,$46,$BB; 28:958C
    .db $00,$F8,$46,$BA,$F8,$F0,$42,$9F,$00,$F0,$42,$9E,$F8,$F8,$46,$BF; 28:959C
    .db $00,$F8,$46,$BE,$04,$01,$F8,$F0,$02,$B0,$00,$F0,$02,$B1,$F8,$F8; 28:95AC
    .db $06,$B2,$00,$F8,$06,$B3,$F8,$F0,$42,$B1,$00,$F0,$42,$B0,$F8,$F8; 28:95BC
    .db $46,$B3,$00,$F8,$46,$B2,$FF,$8E,$95,$06,$01,$F4,$F0,$03,$90,$FC; 28:95CC
    .db $F0,$03,$91,$04,$F0,$03,$92,$F4,$F8,$03,$93,$FC,$F8,$03,$94,$04; 28:95DC
    .db $F8,$03,$95,$F4,$F0,$03,$90,$FC,$F0,$03,$9C,$04,$F0,$03,$92,$F4; 28:95EC
    .db $F8,$03,$93,$FC,$F8,$03,$9D,$04,$F8,$03,$95,$06,$00,$F4,$F0,$02; 28:95FC
    .db $A0,$FC,$F0,$02,$A1,$04,$F0,$02,$A2,$F4,$F8,$02,$A3,$FC,$F8,$02; 28:960C
    .db $A4,$04,$F8,$02,$A5,$06,$00,$F4,$F0,$02,$86,$FC,$F0,$02,$87,$04; 28:961C
    .db $F0,$02,$88,$F4,$F8,$02,$89,$FC,$F8,$02,$8A,$04,$F8,$02,$8B,$04; 28:962C
    .db $01,$F8,$F0,$02,$98,$00,$F0,$02,$99,$F8,$F8,$06,$9C,$00,$F8,$06; 28:963C
    .db $9D,$F8,$F0,$02,$9A,$00,$F0,$42,$9B,$F8,$F8,$46,$9C,$00,$F8,$46; 28:964C
    .db $9D,$FF,$21,$96,$06,$01,$F8,$E8,$02,$82,$00,$E8,$42,$82,$F8,$F0; 28:965C
    .db $02,$A5,$00,$F0,$02,$A6,$F8,$F8,$06,$BC,$00,$F8,$06,$BD,$F8,$E8; 28:966C
    .db $02,$82,$00,$E8,$42,$82,$F8,$F0,$02,$A8,$00,$F0,$02,$A9,$F8,$F8; 28:967C
    .db $06,$AE,$00,$F8,$06,$AF,$07,$01,$F8,$E8,$02,$82,$00,$E8,$42,$82; 28:968C
    .db $F8,$F0,$02,$A8,$00,$F0,$02,$A9,$F8,$F8,$02,$AE,$00,$F8,$02,$AF; 28:969C
    .db $80,$80,$80,$80,$F8,$E8,$02,$A0,$00,$E8,$42,$A0,$F8,$F0,$02,$A1; 28:96AC
    .db $00,$F0,$02,$A2,$F8,$F8,$02,$A3,$00,$F8,$02,$A4,$F8,$00,$02,$A7; 28:96BC
    .db $06,$01,$F8,$E8,$02,$83,$00,$E8,$42,$83,$F8,$F0,$02,$A5,$00,$F0; 28:96CC
    .db $02,$A6,$F8,$F8,$06,$AE,$00,$F8,$06,$AF,$F8,$E8,$02,$A0,$00,$E8; 28:96DC
    .db $42,$A0,$F8,$F0,$02,$AA,$00,$F0,$02,$AB,$F8,$F8,$06,$AC,$00,$F8; 28:96EC
    .db $06,$AD,$06,$01,$F8,$E8,$03,$92,$00,$E8,$03,$93,$F8,$F0,$03,$94; 28:96FC
    .db $00,$F0,$03,$95,$F8,$F8,$07,$90,$00,$F8,$07,$91,$F8,$E8,$03,$92; 28:970C
    .db $00,$E8,$03,$93,$F8,$F0,$43,$95,$00,$F0,$43,$94,$F8,$F8,$47,$91; 28:971C
    .db $00,$F8,$47,$90,$06,$01,$F8,$E9,$43,$97,$00,$E9,$43,$96,$F8,$F1; 28:972C
    .db $43,$99,$00,$F1,$43,$98,$F8,$F9,$47,$9B,$00,$F9,$47,$9A,$F8,$E8; 28:973C
    .db $43,$97,$00,$E8,$43,$96,$F8,$F0,$43,$9D,$00,$F0,$43,$9C,$F8,$F8; 28:974C
    .db $47,$9F,$00,$F8,$47,$9E,$06,$01,$F8,$E8,$03,$8C,$00,$E8,$03,$8D; 28:975C
    .db $F8,$F0,$03,$8E,$00,$F0,$03,$8F,$F8,$F8,$07,$90,$00,$F8,$07,$91; 28:976C
    .db $F8,$E8,$03,$8C,$00,$E8,$03,$8D,$F8,$F0,$43,$8F,$00,$F0,$43,$8E; 28:977C
    .db $F8,$F8,$47,$91,$00,$F8,$47,$90,$FF,$30,$97,$04,$01,$F8,$F0,$02; 28:978C
    .db $A0,$00,$F0,$02,$A1,$F8,$F8,$06,$A2,$00,$F8,$06,$A3,$F8,$F0,$02; 28:979C
    .db $A4,$00,$F0,$02,$A5,$F8,$F8,$06,$A6,$00,$F8,$06,$A7,$09,$01,$F4; 28:97AC
    .db $E8,$03,$A8,$FC,$E8,$03,$A9,$04,$E8,$03,$AA,$F4,$F0,$03,$AB,$FC; 28:97BC
    .db $F0,$03,$AC,$04,$F0,$03,$AD,$F4,$F8,$03,$AE,$FC,$F8,$03,$AF,$04; 28:97CC
    .db $F8,$03,$B0,$F4,$E8,$43,$AA,$FC,$E8,$03,$A9,$04,$E8,$43,$A8,$F4; 28:97DC
    .db $F0,$43,$AD,$FC,$F0,$03,$AC,$04,$F0,$43,$AB,$F4,$F8,$43,$B0,$FC; 28:97EC
    .db $F8,$43,$AF,$04,$F8,$43,$AE,$09,$01,$F4,$E8,$02,$B1,$FC,$E8,$02; 28:97FC
    .db $B2,$04,$E8,$42,$B1,$F4,$F0,$02,$B3,$FC,$F0,$02,$B4,$04,$F0,$42; 28:980C
    .db $B3,$F4,$F8,$02,$B5,$FC,$F8,$02,$B6,$04,$F8,$02,$B7,$F4,$E8,$02; 28:981C
    .db $B8,$FC,$E8,$02,$B9,$04,$E8,$42,$B8,$F4,$F0,$02,$BA,$FC,$F0,$02; 28:982C
    .db $BB,$04,$F0,$42,$BA,$F4,$F8,$02,$BC,$FC,$F8,$02,$BD,$04,$F8,$02; 28:983C
    .db $BE,$09,$03,$F4,$E8,$02,$B3,$FC,$E8,$02,$B4,$04,$E8,$42,$B3,$F4; 28:984C
    .db $F0,$02,$B5,$FC,$F0,$02,$B6,$04,$F0,$42,$B5,$F4,$F8,$02,$B7,$FC; 28:985C
    .db $F8,$02,$B8,$04,$F8,$42,$B7,$F4,$E8,$02,$B9,$FC,$E8,$42,$B4,$04; 28:986C
    .db $E8,$42,$BC,$F4,$F0,$02,$BA,$FC,$F0,$02,$B6,$04,$F0,$42,$BD,$F4; 28:987C
    .db $F8,$02,$BB,$FC,$F8,$02,$B8,$04,$F8,$42,$BE,$F4,$E8,$02,$B3,$FC; 28:988C
    .db $E8,$02,$B4,$04,$E8,$42,$B3,$F4,$F0,$02,$B5,$FC,$F0,$02,$B6,$04; 28:989C
    .db $F0,$42,$B5,$F4,$F8,$02,$B7,$FC,$F8,$02,$B8,$04,$F8,$42,$B7,$F4; 28:98AC
    .db $E8,$02,$BC,$FC,$E8,$42,$B4,$04,$E8,$42,$B9,$F4,$F0,$02,$BD,$FC; 28:98BC
    .db $F0,$02,$B6,$04,$F0,$42,$BA,$F4,$F8,$02,$BE,$FC,$F8,$02,$B8,$04; 28:98CC
    .db $F8,$42,$BB,$06,$03,$F4,$F0,$42,$88,$FC,$F0,$42,$87,$04,$F0,$42; 28:98DC
    .db $8F,$F4,$F8,$42,$8B,$FC,$F8,$42,$8A,$04,$F8,$42,$89,$F4,$F0,$42; 28:98EC
    .db $B1,$FC,$F0,$42,$B0,$04,$F0,$42,$AB,$F4,$F8,$42,$8E,$FC,$F8,$42; 28:98FC
    .db $8D,$04,$F8,$42,$8C,$F4,$F0,$02,$8F,$FC,$F0,$02,$87,$04,$F0,$02; 28:990C
    .db $88,$F4,$F8,$02,$89,$FC,$F8,$02,$8A,$04,$F8,$02,$8B,$F4,$F0,$02; 28:991C
    .db $AB,$FC,$F0,$02,$B0,$04,$F0,$02,$B1,$F4,$F8,$02,$8C,$FC,$F8,$02; 28:992C
    .db $8D,$04,$F8,$02,$8E,$06,$03,$F1,$E9,$01,$B2,$F9,$E9,$01,$B3,$FF; 28:993C
    .db $E9,$41,$B3,$08,$E9,$41,$B2,$F9,$F1,$01,$B4,$FF,$F1,$41,$B4,$F0; 28:994C
    .db $F1,$01,$B5,$F8,$F1,$01,$B6,$00,$F1,$41,$B6,$08,$F1,$41,$B5,$80; 28:995C
    .db $80,$80,$80,$80,$80,$80,$80,$F9,$ED,$81,$B4,$FF,$ED,$C1,$B4,$F1; 28:996C
    .db $F5,$81,$B2,$F9,$F5,$81,$B3,$FF,$F5,$C1,$B3,$08,$F5,$C1,$B2,$F0; 28:997C
    .db $F1,$01,$B5,$F8,$F1,$01,$B6,$00,$F1,$41,$B6,$08,$F1,$41,$B5,$80; 28:998C
    .db $80,$80,$80,$02,$01,$F8,$F8,$03,$B1,$00,$F8,$03,$B2,$F8,$F8,$43; 28:999C
    .db $B2,$00,$F8,$43,$B1,$07,$01,$FC,$E8,$02,$97,$F4,$F0,$02,$98,$FC; 28:99AC
    .db $F0,$02,$99,$04,$F0,$02,$9A,$F4,$F8,$02,$9B,$FC,$F8,$02,$9C,$04; 28:99BC
    .db $F8,$02,$9D,$FC,$E8,$02,$97,$F4,$F0,$02,$98,$FC,$F0,$02,$9E,$04; 28:99CC
    .db $F0,$02,$9A,$F4,$F8,$02,$9B,$FC,$F8,$02,$9C,$04,$F8,$02,$9D,$09; 28:99DC
    .db $01,$F4,$E8,$02,$9A,$FC,$E8,$02,$9B,$04,$E8,$02,$9C,$F4,$F0,$02; 28:99EC
    .db $9D,$FC,$F0,$02,$9E,$04,$F0,$02,$9F,$F4,$F8,$02,$A0,$FC,$F8,$02; 28:99FC
    .db $A1,$04,$F8,$02,$A2,$F4,$E8,$42,$9C,$FC,$E8,$02,$A3,$04,$E8,$42; 28:9A0C
    .db $9A,$F4,$F0,$02,$A4,$FC,$F0,$02,$9E,$04,$F0,$02,$A5,$F4,$F8,$42; 28:9A1C
    .db $A2,$FC,$F8,$02,$A6,$04,$F8,$42,$A0,$09,$01,$F4,$E8,$42,$A9,$FC; 28:9A2C
    .db $E8,$42,$A8,$04,$E8,$42,$A7,$F4,$F0,$42,$AC,$FC,$F0,$42,$AB,$04; 28:9A3C
    .db $F0,$42,$AA,$F4,$F8,$42,$AF,$FC,$F8,$42,$AE,$04,$F8,$42,$AD,$F4; 28:9A4C
    .db $E8,$42,$B2,$FC,$E8,$42,$B1,$04,$E8,$42,$B0,$F4,$F0,$42,$B5,$FC; 28:9A5C
    .db $F0,$42,$B4,$04,$F0,$42,$B3,$F4,$F8,$42,$B8,$FC,$F8,$42,$B7,$04; 28:9A6C
    .db $F8,$42,$B6,$09,$01,$F4,$E8,$02,$8C,$FC,$E8,$02,$8D,$04,$E8,$02; 28:9A7C
    .db $8E,$F4,$F0,$02,$8F,$FC,$F0,$02,$90,$04,$F0,$02,$91,$F4,$F8,$02; 28:9A8C
    .db $92,$FC,$F8,$02,$93,$04,$F8,$02,$94,$F4,$E8,$42,$8E,$FC,$E8,$02; 28:9A9C
    .db $95,$04,$E8,$42,$8C,$F4,$F0,$02,$96,$FC,$F0,$02,$97,$04,$F0,$02; 28:9AAC
    .db $98,$F4,$F8,$42,$94,$FC,$F8,$02,$99,$04,$F8,$42,$92,$FF,$35,$9A; 28:9ABC
    .db $06,$01,$F8,$E8,$03,$A0,$00,$E8,$43,$A0,$F8,$F0,$03,$A1,$00,$F0; 28:9ACC
    .db $43,$A1,$F8,$F8,$03,$A2,$00,$F8,$03,$A3,$F8,$E8,$03,$A4,$00,$E8; 28:9ADC
    .db $43,$A4,$F8,$F0,$03,$A5,$00,$F0,$43,$A5,$F8,$F8,$03,$A6,$00,$F8; 28:9AEC
    .db $03,$A7,$08,$01,$F8,$E8,$03,$92,$00,$E8,$03,$93,$F8,$F0,$03,$94; 28:9AFC
    .db $00,$F0,$03,$95,$F8,$F8,$07,$90,$00,$F8,$07,$91,$F8,$20,$03,$3E; 28:9B0C
    .db $00,$20,$03,$3E,$F8,$E8,$03,$92,$00,$E8,$03,$93,$F8,$F0,$43,$95; 28:9B1C
    .db $00,$F0,$43,$94,$F8,$F8,$47,$91,$00,$F8,$47,$90,$F8,$20,$03,$3E; 28:9B2C
    .db $00,$20,$03,$3E,$08,$01,$F8,$E9,$43,$97,$00,$E9,$43,$96,$F8,$F1; 28:9B3C
    .db $43,$99,$00,$F1,$43,$98,$F8,$F9,$47,$9B,$00,$F9,$47,$9A,$F8,$20; 28:9B4C
    .db $03,$3E,$00,$20,$03,$3E,$F8,$E8,$43,$97,$00,$E8,$43,$96,$F8,$F0; 28:9B5C
    .db $43,$9D,$00,$F0,$43,$9C,$F8,$F8,$47,$9F,$00,$F8,$47,$9E,$F8,$20; 28:9B6C
    .db $03,$3E,$00,$20,$03,$3E,$08,$01,$F8,$E8,$03,$8C,$00,$E8,$03,$8D; 28:9B7C
    .db $F8,$F0,$03,$8E,$00,$F0,$03,$8F,$F8,$F8,$07,$90,$00,$F8,$07,$91; 28:9B8C
    .db $F8,$20,$03,$3E,$00,$20,$03,$3E,$F8,$E8,$03,$8C,$00,$E8,$03,$8D; 28:9B9C
    .db $F8,$F0,$43,$8F,$00,$F0,$43,$8E,$F8,$F8,$47,$91,$00,$F8,$47,$90; 28:9BAC
    .db $F8,$20,$03,$3E,$00,$20,$03,$3E,$FF,$40,$9B,$0B,$01,$F4,$E8,$02; 28:9BBC
    .db $B1,$FC,$E8,$02,$B2,$04,$E8,$42,$B1,$F4,$F0,$02,$B3,$FC,$F0,$02; 28:9BCC
    .db $B4,$04,$F0,$42,$B3,$F4,$F8,$02,$B5,$FC,$F8,$02,$B6,$04,$F8,$02; 28:9BDC
    .db $B7,$F8,$08,$02,$3E,$00,$08,$42,$3E,$F4,$E8,$02,$B8,$FC,$E8,$02; 28:9BEC
    .db $B9,$04,$E8,$42,$B8,$F4,$F0,$02,$BA,$FC,$F0,$02,$BB,$04,$F0,$42; 28:9BFC
    .db $BA,$F4,$F8,$02,$BC,$FC,$F8,$02,$BD,$04,$F8,$02,$BE,$F8,$08,$02; 28:9C0C
    .db $3E,$00,$08,$42,$3E,$08,$01,$F8,$E8,$03,$92,$00,$E8,$03,$93,$F8; 28:9C1C
    .db $F0,$03,$94,$00,$F0,$03,$95,$F8,$F8,$07,$90,$00,$F8,$07,$91,$80; 28:9C2C
    .db $80,$80,$80,$80,$80,$80,$80,$00,$E7,$03,$C7,$00,$EF,$03,$C8,$F8; 28:9C3C
    .db $E8,$03,$92,$00,$E8,$03,$EA,$F8,$F0,$03,$94,$00,$F0,$03,$EB,$F8; 28:9C4C
    .db $F8,$07,$90,$00,$F8,$07,$91,$08,$01,$F8,$E8,$43,$97,$00,$E8,$43; 28:9C5C
    .db $96,$F8,$F0,$43,$99,$00,$F0,$43,$98,$F8,$F8,$47,$9B,$00,$F8,$47; 28:9C6C
    .db $9A,$80,$80,$80,$80,$80,$80,$80,$80,$FC,$DF,$43,$C7,$FC,$F3,$43; 28:9C7C
    .db $C8,$F8,$E8,$43,$EC,$00,$E8,$43,$96,$F8,$F0,$43,$ED,$00,$F0,$43; 28:9C8C
    .db $9C,$F8,$F8,$47,$9B,$00,$F8,$47,$EE,$08,$01,$F8,$E8,$03,$8C,$00; 28:9C9C
    .db $E8,$03,$8D,$F8,$F0,$03,$8E,$00,$F0,$03,$8F,$F8,$F8,$07,$90,$00; 28:9CAC
    .db $F8,$07,$91,$80,$80,$80,$80,$80,$80,$80,$80,$F5,$DF,$03,$C7,$F5; 28:9CBC
    .db $E7,$03,$C8,$F8,$E8,$03,$E8,$00,$E8,$03,$8D,$F8,$F0,$03,$E9,$00; 28:9CCC
    .db $F0,$03,$8F,$F8,$F8,$07,$90,$00,$F8,$07,$91,$FF,$63,$9C,$07,$01; 28:9CDC
    .db $F8,$E8,$03,$92,$00,$E8,$03,$AE,$F8,$F0,$03,$BC,$00,$F0,$03,$BD; 28:9CEC
    .db $F8,$F8,$07,$90,$00,$F8,$07,$91,$80,$80,$80,$80,$00,$E0,$03,$AD; 28:9CFC
    .db $F8,$E8,$03,$92,$00,$E8,$03,$AA,$F8,$F0,$03,$BC,$00,$F0,$03,$BB; 28:9D0C
    .db $F8,$F8,$07,$90,$00,$F8,$07,$91,$0A,$01,$F4,$E8,$43,$97,$FC,$E8; 28:9D1C
    .db $43,$AF,$F4,$F0,$43,$BA,$FC,$F0,$43,$B9,$04,$F0,$43,$B3,$F4,$F8; 28:9D2C
    .db $47,$9B,$FC,$F8,$47,$B5,$04,$F8,$47,$B4,$80,$80,$80,$80,$80,$80; 28:9D3C
    .db $80,$80,$FE,$EF,$43,$EF,$06,$EF,$43,$EE,$F4,$E8,$43,$97,$FC,$E8; 28:9D4C
    .db $43,$AF,$F4,$F0,$43,$B8,$FC,$F0,$43,$B7,$04,$F0,$43,$B3,$F4,$F8; 28:9D5C
    .db $47,$9B,$FC,$F8,$47,$B5,$04,$F8,$47,$B4,$06,$01,$F8,$E8,$03,$A8; 28:9D6C
    .db $00,$E8,$03,$8D,$F8,$F0,$03,$A7,$00,$F0,$03,$B1,$F8,$F8,$07,$BE; 28:9D7C
    .db $00,$F8,$07,$91,$F8,$E8,$03,$A8,$00,$E8,$03,$8D,$F8,$F0,$03,$A6; 28:9D8C
    .db $00,$F0,$03,$B1,$F8,$F8,$07,$B2,$00,$F8,$07,$91,$FF,$24,$9D,$08; 28:9D9C
    .db $01,$FD,$DB,$83,$53,$FE,$E0,$03,$59,$F8,$E8,$03,$92,$00,$E8,$03; 28:9DAC
    .db $93,$F8,$F0,$03,$87,$00,$F0,$03,$88,$F8,$F8,$07,$8A,$00,$F8,$07; 28:9DBC
    .db $84,$03,$F3,$43,$50,$03,$FB,$43,$51,$F8,$E8,$03,$92,$00,$E8,$03; 28:9DCC
    .db $93,$F8,$F0,$03,$85,$00,$F0,$03,$86,$F8,$F8,$47,$91,$00,$F8,$07; 28:9DDC
    .db $84,$08,$01,$07,$F1,$43,$55,$0F,$F1,$43,$54,$F8,$E8,$43,$97,$00; 28:9DEC
    .db $E8,$43,$96,$F8,$F0,$43,$A2,$00,$F0,$43,$A1,$F8,$F8,$47,$9F,$00; 28:9DFC
    .db $F8,$47,$9E,$F4,$F6,$43,$5A,$F8,$E8,$43,$97,$00,$E8,$43,$96,$F8; 28:9E0C
    .db $F0,$43,$A0,$00,$F0,$43,$8B,$F8,$F8,$47,$9B,$00,$F8,$47,$9A,$80; 28:9E1C
    .db $80,$80,$80,$08,$01,$FA,$F8,$03,$52,$FA,$00,$03,$53,$F8,$E8,$03; 28:9E2C
    .db $8C,$00,$E8,$03,$8D,$F8,$F0,$03,$A5,$00,$F0,$03,$83,$F8,$F8,$07; 28:9E3C
    .db $89,$00,$F8,$07,$84,$F4,$EF,$03,$50,$F4,$F7,$03,$51,$F8,$E8,$03; 28:9E4C
    .db $8C,$00,$E8,$03,$8D,$F8,$F0,$03,$A3,$00,$F0,$03,$A4,$F8,$F8,$47; 28:9E5C
    .db $91,$00,$F8,$07,$84,$FF,$ED,$9D,$06,$01,$F8,$E8,$00,$B3,$00,$E8; 28:9E6C
    .db $40,$B3,$F8,$F0,$00,$B4,$00,$F0,$40,$B4,$F8,$F8,$00,$B5,$00,$F8; 28:9E7C
    .db $40,$B5,$F8,$F0,$00,$B6,$00,$F0,$40,$B6,$F8,$F8,$00,$B7,$00,$F8; 28:9E8C
    .db $40,$B7,$80,$80,$80,$80,$0B,$03,$F4,$E8,$03,$D4,$FC,$E8,$03,$D9; 28:9E9C
    .db $04,$E8,$03,$D6,$F4,$F0,$03,$D7,$FC,$F0,$03,$AC,$04,$F0,$03,$D8; 28:9EAC
    .db $F4,$F8,$03,$CC,$FC,$F8,$03,$CD,$04,$F8,$03,$CE,$80,$80,$80,$80; 28:9EBC
    .db $80,$80,$80,$80,$F4,$E8,$03,$D4,$FC,$E8,$03,$D5,$04,$E8,$03,$D6; 28:9ECC
    .db $F4,$F0,$03,$D7,$FC,$F0,$03,$AC,$04,$F0,$03,$D8,$F4,$F8,$03,$CC; 28:9EDC
    .db $FC,$F8,$03,$CD,$04,$F8,$03,$CE,$80,$80,$80,$80,$80,$80,$80,$80; 28:9EEC
    .db $F8,$E3,$03,$DE,$00,$E3,$03,$DF,$F4,$E8,$03,$CF,$FC,$E8,$03,$D0; 28:9EFC
    .db $04,$E8,$03,$D1,$F4,$F0,$03,$D2,$FC,$F0,$03,$AC,$04,$F0,$03,$D3; 28:9F0C
    .db $F4,$F8,$03,$CC,$FC,$F8,$03,$CD,$04,$F8,$03,$CE,$F4,$E8,$03,$C7; 28:9F1C
    .db $FC,$E8,$03,$C8,$04,$E8,$03,$C9,$F4,$F0,$03,$CA,$FC,$F0,$03,$AC; 28:9F2C
    .db $04,$F0,$03,$CB,$F4,$F8,$03,$CC,$FC,$F8,$03,$CD,$04,$F8,$03,$CE; 28:9F3C
    .db $80,$80,$80,$80,$08,$01,$F8,$E8,$03,$92,$00,$E8,$03,$93,$F8,$F0; 28:9F4C
    .db $03,$94,$00,$F0,$03,$95,$F8,$F8,$03,$90,$00,$F8,$03,$91,$80,$80; 28:9F5C
    .db $80,$80,$80,$80,$80,$80,$00,$E3,$83,$BD,$00,$EB,$83,$BC,$F8,$E8; 28:9F6C
    .db $03,$92,$00,$E8,$03,$B7,$F8,$F0,$03,$94,$00,$F0,$03,$B8,$F8,$F8; 28:9F7C
    .db $03,$90,$00,$F8,$03,$91,$08,$01,$F8,$E8,$43,$97,$00,$E8,$43,$96; 28:9F8C
    .db $F8,$F0,$43,$99,$00,$F0,$43,$98,$F8,$F8,$43,$9B,$00,$F8,$43,$9A; 28:9F9C
    .db $80,$80,$80,$80,$80,$80,$80,$80,$F4,$E8,$43,$BF,$FC,$E8,$43,$BE; 28:9FAC
    .db $F8,$E8,$43,$B9,$00,$E8,$43,$96,$F8,$F0,$43,$BA,$00,$F0,$43,$9C; 28:9FBC
    .db $F8,$F8,$43,$9B,$00,$F8,$43,$BB,$08,$01,$F8,$E8,$03,$8C,$00,$E8; 28:9FCC
    .db $03,$8D,$F8,$F0,$03,$8E,$00,$F0,$03,$8F,$F8,$F8,$03,$90,$00,$F8; 28:9FDC
    .db $03,$91,$80,$80,$80,$80,$80,$80,$80,$80,$F6,$E5,$03,$BC,$F6,$ED; 28:9FEC
    .db $03,$BD,$F8,$E8                 ; 28:9FFC
