; re-c bank 14  file 0x1C010  CPU $8000-$9FFF  (tools/disasm_all.py)

tmp_20 = $20
tmp_21 = $21
tmp_22 = $22
tmp_23 = $23
tmp_24 = $24
tmp_25 = $25
tmp_26 = $26
tmp_27 = $27
tmp_28 = $28
tmp_29 = $29
tmp_2F = $2F
area_id = $6C
obj_x_lo = $70
obj_flags = $0380
player_hp_max = $03C0
player_hp = $03C1
player_level = $0421
obj_active_s0D = $04AD
obj_spawn_b2 = $0540
obj_spawn_param = $0560
obj_freeze = $05A0
obj_phase_s0D = $060D
obj_act_timer_s03 = $0643
mp_cur = $0708
mp_max = $0709
obj1_flash = $0710
equip_sword = $0711
use_item_sel = $0715
equip_ball = $0718
telepathy_rng = $07C0
boss_cam_lock = $07D7
inv_select = $6428
inventory = $6430
inv_643F = $643F
story_flags = $6480
npc_state = $6580
EventTriggerTable = $A17A
PlaySound = $C125

    .org $8000
NpcScript:  ; $23 = NPC param; returns $20/$21 msg id and $22 action
    LDA tmp_23                          ; 8000  a5 23
    ASL A                               ; 8002  0a
    TAX                                 ; 8003  aa
    BCS loc_14_8011                     ; 8004  b0 0b
    LDA NpcScriptPtrsLo,X               ; 8006  bd 5d 89
    STA tmp_24                          ; 8009  85 24
    LDA $895E,X                         ; 800B  bd 5e 89
    JMP loc_14_8019                     ; 800E  4c 19 80
loc_14_8011:
    LDA NpcScriptPtrsHi,X               ; 8011  bd 5d 8a
    STA tmp_24                          ; 8014  85 24
    LDA $8A5E,X                         ; 8016  bd 5e 8a
loc_14_8019:
    STA tmp_25                          ; 8019  85 25
    LDY #$00                            ; 801B  a0 00
loc_14_801D:
    JSR NpcFlagTest                     ; 801D  20 35 81
    BEQ loc_14_8025                     ; 8020  f0 03
    JMP NpcResult                       ; 8022  4c 78 80
loc_14_8025:
    JSR sub_14_808F                     ; 8025  20 8f 80
    LDA tmp_26                          ; 8028  a5 26
    BPL loc_14_801D                     ; 802A  10 f1
loc_14_802C:
    LDA (tmp_24),Y                      ; 802C  b1 24
    INY                                 ; 802E  c8
    CMP #$FF                            ; 802F  c9 ff
    BEQ loc_14_8046                     ; 8031  f0 13
    CMP area_id                         ; 8033  c5 6c
    BEQ loc_14_803B                     ; 8035  f0 04
    INY                                 ; 8037  c8
    JMP loc_14_802C                     ; 8038  4c 2c 80
loc_14_803B:
    LDA (tmp_24),Y                      ; 803B  b1 24
    LDX tmp_23                          ; 803D  a6 23
    STA npc_state,X                     ; 803F  9d 80 65
    INY                                 ; 8042  c8
    JMP loc_14_802C                     ; 8043  4c 2c 80
loc_14_8046:
    TYA                                 ; 8046  98
    CLC                                 ; 8047  18
    ADC tmp_24                          ; 8048  65 24
    STA tmp_24                          ; 804A  85 24
    LDA #$00                            ; 804C  a9 00
    ADC tmp_25                          ; 804E  65 25
    STA tmp_25                          ; 8050  85 25
    LDX tmp_23                          ; 8052  a6 23
    LDY npc_state,X                     ; 8054  bc 80 65
loc_14_8057:
    JSR NpcFlagTest                     ; 8057  20 35 81
    BEQ loc_14_8071                     ; 805A  f0 15
    JSR NpcResult                       ; 805C  20 78 80
    LDX tmp_23                          ; 805F  a6 23
    LDA (tmp_24),Y                      ; 8061  b1 24
    STA npc_state,X                     ; 8063  9d 80 65
    LDA tmp_26                          ; 8066  a5 26
    AND #$40                            ; 8068  29 40
    BEQ loc_14_8070                     ; 806A  f0 04
    INY                                 ; 806C  c8
    JSR NpcFlagApply                    ; 806D  20 12 81
loc_14_8070:
    RTS                                 ; 8070  60
loc_14_8071:
    JSR sub_14_808E                     ; 8071  20 8e 80
    LDA tmp_26                          ; 8074  a5 26
    BPL loc_14_8057                     ; 8076  10 df
NpcResult:  ; r0 r1 -> $22 = r0>>3; $21 = r0&7; $20 = r1
    LDA (tmp_24),Y                      ; 8078  b1 24
    PHA                                 ; 807A  48
    AND #$FF                            ; 807B  29 ff
    LSR A                               ; 807D  4a
    LSR A                               ; 807E  4a
    LSR A                               ; 807F  4a
    STA tmp_22                          ; 8080  85 22
    PLA                                 ; 8082  68
    AND #$07                            ; 8083  29 07
    STA tmp_21                          ; 8085  85 21
    INY                                 ; 8087  c8
    LDA (tmp_24),Y                      ; 8088  b1 24
    STA tmp_20                          ; 808A  85 20
    INY                                 ; 808C  c8
    RTS                                 ; 808D  60
sub_14_808E:
    INY                                 ; 808E  c8
sub_14_808F:
    INY                                 ; 808F  c8
    INY                                 ; 8090  c8
    LDA tmp_26                          ; 8091  a5 26
    AND #$40                            ; 8093  29 40
    BEQ loc_14_809F                     ; 8095  f0 08
loc_14_8097:
    LDA (tmp_24),Y                      ; 8097  b1 24
    INY                                 ; 8099  c8
    INY                                 ; 809A  c8
    AND #$40                            ; 809B  29 40
    BEQ loc_14_8097                     ; 809D  f0 f8
loc_14_809F:
    RTS                                 ; 809F  60
NpcHidden:  ; $23=param; $20!=0 -> NPC absent (called 31:E34B spawn handler)
    LDA #$00                            ; 80A0  a9 00
    STA tmp_20                          ; 80A2  85 20
    TAY                                 ; 80A4  a8
    LDA tmp_23                          ; 80A5  a5 23
    ASL A                               ; 80A7  0a
    TAX                                 ; 80A8  aa
    BCS loc_14_80B6                     ; 80A9  b0 0b
    LDA NpcPresenceLo,X                 ; 80AB  bd e0 85
    STA tmp_24                          ; 80AE  85 24
    LDA $85E1,X                         ; 80B0  bd e1 85
    JMP loc_14_80BE                     ; 80B3  4c be 80
loc_14_80B6:
    LDA NpcPresenceHi,X                 ; 80B6  bd e0 86
    STA tmp_24                          ; 80B9  85 24
    LDA $86E1,X                         ; 80BB  bd e1 86
loc_14_80BE:
    STA tmp_25                          ; 80BE  85 25
loc_14_80C0:
    LDA (tmp_24),Y                      ; 80C0  b1 24
    INY                                 ; 80C2  c8
    CMP #$FF                            ; 80C3  c9 ff
    BEQ loc_14_80E2                     ; 80C5  f0 1b
    CMP area_id                         ; 80C7  c5 6c
    BNE loc_14_80D9                     ; 80C9  d0 0e
loc_14_80CB:
    JSR NpcFlagTest                     ; 80CB  20 35 81
    BNE loc_14_80D3                     ; 80CE  d0 03
    INC tmp_20                          ; 80D0  e6 20
    RTS                                 ; 80D2  60
loc_14_80D3:
    LDA tmp_26                          ; 80D3  a5 26
    BPL loc_14_80CB                     ; 80D5  10 f4
    BMI loc_14_80C0                     ; 80D7  30 e7
loc_14_80D9:
    LDA (tmp_24),Y                      ; 80D9  b1 24
    INY                                 ; 80DB  c8
    INY                                 ; 80DC  c8
    ROL A                               ; 80DD  2a
    BCC loc_14_80D9                     ; 80DE  90 f9
    BCS loc_14_80C0                     ; 80E0  b0 de
loc_14_80E2:
    RTS                                 ; 80E2  60
ItemHook2:  ; item ids >=$80; table 14:A17A; applies set-list
    LDA tmp_23                          ; 80E3  a5 23
    ASL A                               ; 80E5  0a
    CMP #$86                            ; 80E6  c9 86
    BCC loc_14_80EA                     ; 80E8  90 00
loc_14_80EA:
    TAX                                 ; 80EA  aa
    LDA #$FF                            ; 80EB  a9 ff
    STA tmp_23                          ; 80ED  85 23
    TAY                                 ; 80EF  a8
    INY                                 ; 80F0  c8
    LDA EventTriggerTable,X             ; 80F1  bd 7a a1
    STA tmp_24                          ; 80F4  85 24
    LDA $A17B,X                         ; 80F6  bd 7b a1
    STA tmp_25                          ; 80F9  85 25
loc_14_80FB:
    JSR NpcFlagTest                     ; 80FB  20 35 81
    BNE loc_14_8107                     ; 80FE  d0 07
    LDA #$00                            ; 8100  a9 00
    STA tmp_20                          ; 8102  85 20
    STA tmp_21                          ; 8104  85 21
    RTS                                 ; 8106  60
loc_14_8107:
    LDA tmp_26                          ; 8107  a5 26
    BPL loc_14_80FB                     ; 8109  10 f0
    LDA #$00                            ; 810B  a9 00
    STA tmp_23                          ; 810D  85 23
    JSR NpcResult                       ; 810F  20 78 80
NpcFlagApply:  ; set-list: bit7 clear else set; stop at bit6
    JSR NpcFlagRef                      ; 8112  20 48 81
    LDA tmp_26                          ; 8115  a5 26
    BMI loc_14_8124                     ; 8117  30 0b
    LDA story_flags,X                   ; 8119  bd 80 64
    ORA tmp_28                          ; 811C  05 28
    STA story_flags,X                   ; 811E  9d 80 64
    JMP loc_14_812E                     ; 8121  4c 2e 81
loc_14_8124:
    LDA tmp_28                          ; 8124  a5 28
    EOR #$FF                            ; 8126  49 ff
    AND story_flags,X                   ; 8128  3d 80 64
    STA story_flags,X                   ; 812B  9d 80 64
loc_14_812E:
    LDA tmp_26                          ; 812E  a5 26
    AND #$40                            ; 8130  29 40
    BEQ NpcFlagApply                    ; 8132  f0 de
    RTS                                 ; 8134  60
NpcFlagTest:  ; Z clear = cond true (b0 bit5 = invert)
    JSR NpcFlagRef                      ; 8135  20 48 81
    LDA tmp_26                          ; 8138  a5 26
    AND #$20                            ; 813A  29 20
    PHP                                 ; 813C  08
    LDA story_flags,X                   ; 813D  bd 80 64
    PLP                                 ; 8140  28
    BEQ loc_14_8145                     ; 8141  f0 02
    EOR #$FF                            ; 8143  49 ff
loc_14_8145:
    AND tmp_28                          ; 8145  25 28
    RTS                                 ; 8147  60
NpcFlagRef:  ; 2-byte flag ref -> X byte index into $6480 + $28 mask + $26 flags
    LDA (tmp_24),Y                      ; 8148  b1 24
    STA tmp_26                          ; 814A  85 26
    STA tmp_27                          ; 814C  85 27
    INY                                 ; 814E  c8
    LDA (tmp_24),Y                      ; 814F  b1 24
    INY                                 ; 8151  c8
    PHA                                 ; 8152  48
    AND #$07                            ; 8153  29 07
    TAX                                 ; 8155  aa
    LDA $8167,X                         ; 8156  bd 67 81
    STA tmp_28                          ; 8159  85 28
    PLA                                 ; 815B  68
    LSR tmp_27                          ; 815C  46 27
    ROR A                               ; 815E  6a
    LSR tmp_27                          ; 815F  46 27
    ROR A                               ; 8161  6a
    LSR tmp_27                          ; 8162  46 27
    ROR A                               ; 8164  6a
    TAX                                 ; 8165  aa
    RTS                                 ; 8166  60
    .db $01,$02,$04,$08,$10,$20,$40,$80 ; 14:8167
Telepathy:  ; MP<8 -> fail row; else MP-=8; random row; msg word 14:9A2C[(row*4+sage)*2]
    SEC                                 ; 816F  38
    LDA mp_cur                          ; 8170  ad 08 07
    SBC #$08                            ; 8173  e9 08
    BCS loc_14_818B                     ; 8175  b0 14
    LDX #$00                            ; 8177  a2 00
loc_14_8179:
    TXA                                 ; 8179  8a
    ASL A                               ; 817A  0a
    ASL A                               ; 817B  0a
    ORA tmp_23                          ; 817C  05 23
    ASL A                               ; 817E  0a
    TAX                                 ; 817F  aa
    LDA TelepathyMsgTable,X             ; 8180  bd 2c 9a
    STA tmp_21                          ; 8183  85 21
    LDA $9A2D,X                         ; 8185  bd 2d 9a
    STA tmp_20                          ; 8188  85 20
    RTS                                 ; 818A  60
loc_14_818B:
    STA mp_cur                          ; 818B  8d 08 07
    JSR sub_14_8222                     ; 818E  20 22 82
    TAX                                 ; 8191  aa
    LDA TelepathyClass,X                ; 8192  bd 1a 82
    BNE loc_14_81AF                     ; 8195  d0 18
    CLC                                 ; 8197  18
    LDA #$20                            ; 8198  a9 20
    ADC mp_cur                          ; 819A  6d 08 07
    BCS loc_14_81A4                     ; 819D  b0 05
    CMP mp_max                          ; 819F  cd 09 07
    BCC loc_14_81A7                     ; 81A2  90 03
loc_14_81A4:
    LDA mp_max                          ; 81A4  ad 09 07
loc_14_81A7:
    STA mp_cur                          ; 81A7  8d 08 07
    LDX #$01                            ; 81AA  a2 01
    JMP loc_14_8179                     ; 81AC  4c 79 81
loc_14_81AF:
    CMP #$01                            ; 81AF  c9 01
    BNE loc_14_81B8                     ; 81B1  d0 05
    LDX #$02                            ; 81B3  a2 02
    JMP loc_14_8179                     ; 81B5  4c 79 81
loc_14_81B8:
    AND #$01                            ; 81B8  29 01
    STA tmp_29                          ; 81BA  85 29
    LDX area_id                         ; 81BC  a6 6c
    LDA TelepathyHintClass,X            ; 81BE  bd f4 98
    STA tmp_28                          ; 81C1  85 28
    TAX                                 ; 81C3  aa
    LDA TelepathyLevelGate,X            ; 81C4  bd 13 82
    CMP player_level                    ; 81C7  cd 21 04
    BCC TelepathyHint                   ; 81CA  90 05
    LDX #$03                            ; 81CC  a2 03
    JMP loc_14_8179                     ; 81CE  4c 79 81
TelepathyHint:  ; walk hint list; end -> fixed row 3
    LDA tmp_28                          ; 81D1  a5 28
    ASL A                               ; 81D3  0a
    ASL A                               ; 81D4  0a
    ORA tmp_23                          ; 81D5  05 23
    ASL A                               ; 81D7  0a
    TAX                                 ; 81D8  aa
    LDA TelepathyHintLists,X            ; 81D9  bd f4 99
    STA tmp_24                          ; 81DC  85 24
    LDA $99F5,X                         ; 81DE  bd f5 99
    STA tmp_25                          ; 81E1  85 25
    LDY #$00                            ; 81E3  a0 00
loc_14_81E5:
    JSR NpcFlagTest                     ; 81E5  20 35 81
    BNE loc_14_8200                     ; 81E8  d0 16
    LDA tmp_26                          ; 81EA  a5 26
    AND #$40                            ; 81EC  29 40
    BEQ loc_14_81F6                     ; 81EE  f0 06
    LDA tmp_29                          ; 81F0  a5 29
    BEQ loc_14_81F6                     ; 81F2  f0 02
    INY                                 ; 81F4  c8
    INY                                 ; 81F5  c8
loc_14_81F6:
    LDA (tmp_24),Y                      ; 81F6  b1 24
    STA tmp_21                          ; 81F8  85 21
    INY                                 ; 81FA  c8
    LDA (tmp_24),Y                      ; 81FB  b1 24
    STA tmp_20                          ; 81FD  85 20
    RTS                                 ; 81FF  60
loc_14_8200:
    INY                                 ; 8200  c8
    INY                                 ; 8201  c8
    LDA tmp_26                          ; 8202  a5 26
    AND #$40                            ; 8204  29 40
    BEQ loc_14_820A                     ; 8206  f0 02
    INY                                 ; 8208  c8
    INY                                 ; 8209  c8
loc_14_820A:
    LDA tmp_26                          ; 820A  a5 26
    BPL loc_14_81E5                     ; 820C  10 d7
    LDX #$03                            ; 820E  a2 03
    JMP loc_14_8179                     ; 8210  4c 79 81
TelepathyLevelGate:  ; 7 bytes 02..0E: hint only if player_level > gate
    .db $02,$04,$06,$08,$0A,$0C,$0E     ; 14:8213
TelepathyClass:  ; 8 bytes: class for random value 0-7
    .db $00,$01,$02,$03,$02,$02,$03,$03 ; 14:821A
sub_14_8222:
    LDA telepathy_rng                   ; 8222  ad c0 07
    AND #$3F                            ; 8225  29 3f
    INC telepathy_rng                   ; 8227  ee c0 07
    TAY                                 ; 822A  a8
    LDA TelepathyRandSeq,Y              ; 822B  b9 2f 82
    RTS                                 ; 822E  60
TelepathyRandSeq:  ; 64 bytes 0-7 indexed by $07C0&$3F
    .db $07,$01,$06,$02,$07,$05,$03,$06,$04,$05,$03,$00,$00,$02,$04,$06; 14:822F
    .db $03,$06,$01,$01,$07,$04,$03,$05,$04,$06,$02,$04,$05,$01,$00,$00; 14:823F
    .db $00,$03,$07,$07,$02,$00,$03,$05,$04,$01,$03,$04,$01,$06,$02,$00; 14:824F
    .db $06,$06,$02,$01,$00,$02,$07,$05,$02,$04,$07,$03,$04,$01,$05,$00; 14:825F
ItemGetHook:  ; hook 0: store item $23 via 14:9B00 record; then result + set-list; chain next item
    LDA tmp_23                          ; 826F  a5 23
    TAY                                 ; 8271  a8
    ORA #$80                            ; 8272  09 80
    STA tmp_23                          ; 8274  85 23
    ASL A                               ; 8276  0a
    TAX                                 ; 8277  aa
    LDA ItemGetTable,X                  ; 8278  bd 00 9b
    STA tmp_24                          ; 827B  85 24
    LDA $9B01,X                         ; 827D  bd 01 9b
    STA tmp_25                          ; 8280  85 25
    LDA ItemStoreValue,Y                ; 8282  b9 66 9d
    STA tmp_29                          ; 8285  85 29
    JSR InvStoreItem                    ; 8287  20 a0 82
    LDA tmp_23                          ; 828A  a5 23
    BPL loc_14_828F                     ; 828C  10 01
    RTS                                 ; 828E  60
loc_14_828F:
    LDY #$02                            ; 828F  a0 02
    JSR NpcResult                       ; 8291  20 78 80
    JSR NpcFlagApply                    ; 8294  20 12 81
    LDA (tmp_24),Y                      ; 8297  b1 24
    BMI loc_14_829F                     ; 8299  30 04
    STA tmp_23                          ; 829B  85 23
    BPL ItemGetHook                     ; 829D  10 d0
loc_14_829F:
    RTS                                 ; 829F  60
InvStoreItem:  ; count bit7 = fixed equip slot else first $FF slot; Crystalis merge; bracelet unequips ball
    LDY #$00                            ; 82A0  a0 00
    LDA (tmp_24),Y                      ; 82A2  b1 24
    TAX                                 ; 82A4  aa
    INY                                 ; 82A5  c8
    LDA (tmp_24),Y                      ; 82A6  b1 24
    TAY                                 ; 82A8  a8
    BPL loc_14_8308                     ; 82A9  10 5d
    LDA tmp_23                          ; 82AB  a5 23
    AND #$7F                            ; 82AD  29 7f
    STA tmp_23                          ; 82AF  85 23
    LDA tmp_29                          ; 82B1  a5 29
    CMP #$04                            ; 82B3  c9 04
    BNE loc_14_82DE                     ; 82B5  d0 27
    LDA #$04                            ; 82B7  a9 04
    STA inventory                       ; 82B9  8d 30 64
    LDA #$05                            ; 82BC  a9 05
    STA equip_sword                     ; 82BE  8d 11 07
    LDA #$FF                            ; 82C1  a9 ff
    STA $6431                           ; 82C3  8d 31 64
    STA $6432                           ; 82C6  8d 32 64
    STA $6433                           ; 82C9  8d 33 64
    STA $643C                           ; 82CC  8d 3c 64
    STA $643D                           ; 82CF  8d 3d 64
    STA $643E                           ; 82D2  8d 3e 64
    STA inv_643F                        ; 82D5  8d 3f 64
    LDA #$00                            ; 82D8  a9 00
    STA inv_select                      ; 82DA  8d 28 64
    RTS                                 ; 82DD  60
loc_14_82DE:
    LDA tmp_29                          ; 82DE  a5 29
    STA inventory,X                     ; 82E0  9d 30 64
    CMP #$06                            ; 82E3  c9 06
    BEQ loc_14_82F4                     ; 82E5  f0 0d
    CMP #$08                            ; 82E7  c9 08
    BEQ loc_14_82F4                     ; 82E9  f0 09
    CMP #$0A                            ; 82EB  c9 0a
    BEQ loc_14_82F4                     ; 82ED  f0 05
    CMP #$0C                            ; 82EF  c9 0c
    BEQ loc_14_82F4                     ; 82F1  f0 01
    RTS                                 ; 82F3  60
loc_14_82F4:
    SEC                                 ; 82F4  38
    SBC #$04                            ; 82F5  e9 04
    LSR A                               ; 82F7  4a
    CMP equip_ball                      ; 82F8  cd 18 07
    BNE loc_14_8307                     ; 82FB  d0 0a
    LDA #$FF                            ; 82FD  a9 ff
    STA $642B                           ; 82FF  8d 2b 64
    LDA #$00                            ; 8302  a9 00
    STA equip_ball                      ; 8304  8d 18 07
loc_14_8307:
    RTS                                 ; 8307  60
loc_14_8308:
    LDA inventory,X                     ; 8308  bd 30 64
    CMP #$FF                            ; 830B  c9 ff
    BEQ loc_14_8314                     ; 830D  f0 05
    INX                                 ; 830F  e8
    DEY                                 ; 8310  88
    BNE loc_14_8308                     ; 8311  d0 f5
    RTS                                 ; 8313  60
loc_14_8314:
    LDA tmp_29                          ; 8314  a5 29
    STA inventory,X                     ; 8316  9d 30 64
    LDA #$00                            ; 8319  a9 00
    STA tmp_23                          ; 831B  85 23
    RTS                                 ; 831D  60
ItemUseHook:  ; hook 1: handler 14:8399[item] + record 14:9BE2[item]; then result + set-list; removes item unless $36
    LDA tmp_23                          ; 831E  a5 23
    ASL A                               ; 8320  0a
    TAX                                 ; 8321  aa
    LDA ItemUseTable,X                  ; 8322  bd e2 9b
    STA tmp_24                          ; 8325  85 24
    LDA $9BE3,X                         ; 8327  bd e3 9b
    STA tmp_25                          ; 832A  85 25
    LDA ItemUseHandlers,X               ; 832C  bd 99 83
    STA tmp_26                          ; 832F  85 26
    LDA $839A,X                         ; 8331  bd 9a 83
    STA tmp_27                          ; 8334  85 27
    LDY #$00                            ; 8336  a0 00
    JSR sub_14_8351                     ; 8338  20 51 83
    LDA tmp_23                          ; 833B  a5 23
    CMP #$FF                            ; 833D  c9 ff
    BEQ loc_14_8350                     ; 833F  f0 0f
    JSR NpcResult                       ; 8341  20 78 80
    JSR NpcFlagApply                    ; 8344  20 12 81
    LDA tmp_23                          ; 8347  a5 23
    CMP #$36                            ; 8349  c9 36
    BEQ loc_14_8350                     ; 834B  f0 03
    JSR InvRemoveItem                   ; 834D  20 54 83
loc_14_8350:
    RTS                                 ; 8350  60
sub_14_8351:
    JMP (tmp_26)                        ; 8351  6c 26 00
InvRemoveItem:  ; remove item $23 from its inventory range (shift down + $FF); $642A+(X>>3)=$FF; $0715=0
    LDA tmp_23                          ; 8354  a5 23
    ASL A                               ; 8356  0a
    TAX                                 ; 8357  aa
    LDA ItemGetTable,X                  ; 8358  bd 00 9b
    STA tmp_24                          ; 835B  85 24
    LDA $9B01,X                         ; 835D  bd 01 9b
    STA tmp_25                          ; 8360  85 25
    LDY #$00                            ; 8362  a0 00
    LDA (tmp_24),Y                      ; 8364  b1 24
    TAX                                 ; 8366  aa
    INY                                 ; 8367  c8
    LDA (tmp_24),Y                      ; 8368  b1 24
    TAY                                 ; 836A  a8
loc_14_836B:
    LDA inventory,X                     ; 836B  bd 30 64
    CMP tmp_23                          ; 836E  c5 23
    BEQ loc_14_8377                     ; 8370  f0 05
    INX                                 ; 8372  e8
    DEY                                 ; 8373  88
    BNE loc_14_836B                     ; 8374  d0 f5
    RTS                                 ; 8376  60
loc_14_8377:
    CPY #$01                            ; 8377  c0 01
    BEQ loc_14_8384                     ; 8379  f0 09
    LDA $6431,X                         ; 837B  bd 31 64
    STA inventory,X                     ; 837E  9d 30 64
    DEY                                 ; 8381  88
    BNE loc_14_8377                     ; 8382  d0 f3
loc_14_8384:
    LDA #$FF                            ; 8384  a9 ff
    STA inventory,X                     ; 8386  9d 30 64
    TXA                                 ; 8389  8a
    LSR A                               ; 838A  4a
    LSR A                               ; 838B  4a
    LSR A                               ; 838C  4a
    TAX                                 ; 838D  aa
    LDA #$FF                            ; 838E  a9 ff
    STA $642A,X                         ; 8390  9d 2a 64
    LDA #$00                            ; 8393  a9 00
    STA use_item_sel                    ; 8395  8d 15 07
    RTS                                 ; 8398  60
ItemUseHandlers:  ; 80 words (items 00-4F); $84DB = cannot use
    .dw UseFail                         ; 14:8399
    .dw UseFail                         ; 14:839B
    .dw UseFail                         ; 14:839D
    .dw UseFail                         ; 14:839F
    .dw sub_14_8491                     ; 14:83A1
    .dw UseFail                         ; 14:83A3
    .dw UseFail                         ; 14:83A5
    .dw UseFail                         ; 14:83A7
    .dw UseFail                         ; 14:83A9
    .dw UseFail                         ; 14:83AB
    .dw UseFail                         ; 14:83AD
    .dw UseFail                         ; 14:83AF
    .dw UseFail                         ; 14:83B1
    .dw UseFail                         ; 14:83B3
    .dw UseFail                         ; 14:83B5
    .dw UseFail                         ; 14:83B7
    .dw UseFail                         ; 14:83B9
    .dw UseFail                         ; 14:83BB
    .dw UseFail                         ; 14:83BD
    .dw UseFail                         ; 14:83BF
    .dw UseFail                         ; 14:83C1
    .dw UseFail                         ; 14:83C3
    .dw UseFail                         ; 14:83C5
    .dw UseFail                         ; 14:83C7
    .dw UseFail                         ; 14:83C9
    .dw UseFail                         ; 14:83CB
    .dw UseFail                         ; 14:83CD
    .dw UseFail                         ; 14:83CF
    .dw UseFail                         ; 14:83D1
    .dw sub_14_84E0                     ; 14:83D3
    .dw sub_14_8524                     ; 14:83D5
    .dw sub_14_852F                     ; 14:83D7
    .dw sub_14_853A                     ; 14:83D9
    .dw sub_14_8507                     ; 14:83DB
    .dw sub_14_851D                     ; 14:83DD
    .dw sub_14_854A                     ; 14:83DF
    .dw sub_14_8564                     ; 14:83E1
    .dw sub_14_8565                     ; 14:83E3
    .dw sub_14_8450                     ; 14:83E5
    .dw sub_14_84B3                     ; 14:83E7
    .dw sub_14_859E                     ; 14:83E9
    .dw UseFail                         ; 14:83EB
    .dw UseFail                         ; 14:83ED
    .dw UseFail                         ; 14:83EF
    .dw UseFail                         ; 14:83F1
    .dw UseFail                         ; 14:83F3
    .dw UseFail                         ; 14:83F5
    .dw UseFail                         ; 14:83F7
    .dw UseFail                         ; 14:83F9
    .dw sub_14_84A9                     ; 14:83FB
    .dw sub_14_845F                     ; 14:83FD
    .dw sub_14_8491                     ; 14:83FF
    .dw sub_14_8491                     ; 14:8401
    .dw sub_14_8491                     ; 14:8403
    .dw sub_14_856B                     ; 14:8405
    .dw sub_14_845F                     ; 14:8407
    .dw UseFail                         ; 14:8409
    .dw sub_14_8585                     ; 14:840B
    .dw sub_14_84D0                     ; 14:840D
    .dw sub_14_8439                     ; 14:840F
    .dw sub_14_8491                     ; 14:8411
    .dw sub_14_8491                     ; 14:8413
    .dw UseBowCheck                     ; 14:8415
    .dw UseBowCheck                     ; 14:8417
    .dw sub_14_8442                     ; 14:8419
    .dw UseFail                         ; 14:841B
    .dw UseFail                         ; 14:841D
    .dw UseFail                         ; 14:841F
    .dw UseFail                         ; 14:8421
    .dw UseFail                         ; 14:8423
    .dw UseFail                         ; 14:8425
    .dw UseFail                         ; 14:8427
    .dw UseFail                         ; 14:8429
    .dw UseFail                         ; 14:842B
    .dw UseFail                         ; 14:842D
    .dw UseFail                         ; 14:842F
    .dw UseFail                         ; 14:8431
    .dw UseFail                         ; 14:8433
    .dw UseFail                         ; 14:8435
    .dw UseFail                         ; 14:8437
sub_14_8439:
    LDA area_id                         ; 8439  a5 6c
    CMP #$62                            ; 843B  c9 62
    BEQ loc_14_845C                     ; 843D  f0 1d
    JMP sub_14_8491                     ; 843F  4c 91 84
sub_14_8442:
    JSR UseBowCheck                     ; 8442  20 51 84
    LDA obj_phase_s0D                   ; 8445  ad 0d 06
    BEQ loc_14_845C                     ; 8448  f0 12
    LDA area_id                         ; 844A  a5 6c
    CMP #$A6                            ; 844C  c9 a6
    BNE loc_14_845C                     ; 844E  d0 0c
sub_14_8450:
    RTS                                 ; 8450  60
UseBowCheck:  ; Bow of Moon/Sun use: target object in 0D-1F and $07D7 in 1..7F
    JSR UseFindSlotObj                  ; 8451  20 69 84
    LDA boss_cam_lock                   ; 8454  ad d7 07
    BEQ loc_14_845C                     ; 8457  f0 03
    BMI loc_14_845C                     ; 8459  30 01
    RTS                                 ; 845B  60
loc_14_845C:
    JMP UseFail                         ; 845C  4c db 84
sub_14_845F:
    LDA (tmp_24),Y                      ; 845F  b1 24
    STA tmp_26                          ; 8461  85 26
    INY                                 ; 8463  c8
    CMP area_id                         ; 8464  c5 6c
    BNE UseFail                         ; 8466  d0 73
    RTS                                 ; 8468  60
UseFindSlotObj:  ; record param/b2 matches an object in slots 0D-1F
    LDA (tmp_24),Y                      ; 8469  b1 24
    INY                                 ; 846B  c8
    STA tmp_26                          ; 846C  85 26
    LDA (tmp_24),Y                      ; 846E  b1 24
    INY                                 ; 8470  c8
    STA tmp_27                          ; 8471  85 27
    LDX #$0D                            ; 8473  a2 0d
loc_14_8475:
    LDA obj_flags,X                     ; 8475  bd 80 03
    BMI loc_14_8489                     ; 8478  30 0f
    LDA obj_spawn_param,X               ; 847A  bd 60 05
    CMP tmp_26                          ; 847D  c5 26
    BNE loc_14_8489                     ; 847F  d0 08
    LDA obj_spawn_b2,X                  ; 8481  bd 40 05
    CMP tmp_27                          ; 8484  c5 27
    BNE loc_14_8489                     ; 8486  d0 01
    RTS                                 ; 8488  60
loc_14_8489:
    INX                                 ; 8489  e8
    CPX #$20                            ; 848A  e0 20
    BNE loc_14_8475                     ; 848C  d0 e7
    JMP UseFail                         ; 848E  4c db 84
sub_14_8491:
    JSR UseTouchMatch                   ; 8491  20 97 84
    BNE UseFail                         ; 8494  d0 45
    RTS                                 ; 8496  60
UseTouchMatch:  ; record param/b2 == touched spawn ($0643); Z set = match
    LDX obj_act_timer_s03               ; 8497  ae 43 06
    LDA (tmp_24),Y                      ; 849A  b1 24
    INY                                 ; 849C  c8
    CMP obj_spawn_param,X               ; 849D  dd 60 05
    BNE loc_14_84A8                     ; 84A0  d0 06
    LDA (tmp_24),Y                      ; 84A2  b1 24
    INY                                 ; 84A4  c8
    CMP obj_spawn_b2,X                  ; 84A5  dd 40 05
loc_14_84A8:
    RTS                                 ; 84A8  60
sub_14_84A9:
    JSR UseTouchMatch                   ; 84A9  20 97 84
    BNE loc_14_84AF                     ; 84AC  d0 01
    RTS                                 ; 84AE  60
loc_14_84AF:
    LDY #$06                            ; 84AF  a0 06
    BNE sub_14_8491                     ; 84B1  d0 de
sub_14_84B3:
    INY                                 ; 84B3  c8
    LDA area_id                         ; 84B4  a5 6c
    CMP #$1A                            ; 84B6  c9 1a
    BNE UseFail                         ; 84B8  d0 21
    LDA $038D                           ; 84BA  ad 8d 03
    BMI UseFail                         ; 84BD  30 1c
    LDA obj_active_s0D                  ; 84BF  ad ad 04
    BEQ UseFail                         ; 84C2  f0 17
    LDA obj_x_lo                        ; 84C4  a5 70
    CMP #$0A                            ; 84C6  c9 0a
    BCC UseFail                         ; 84C8  90 11
    LDA #$E2                            ; 84CA  a9 e2
    STA obj_active_s0D                  ; 84CC  8d ad 04
    RTS                                 ; 84CF  60
sub_14_84D0:
    JSR UseTouchMatch                   ; 84D0  20 97 84
    BNE UseFail                         ; 84D3  d0 06
    LDA #$FF                            ; 84D5  a9 ff
    STA $62F2                           ; 84D7  8d f2 62
    RTS                                 ; 84DA  60
UseFail:  ; $23=$FF (item cannot be used here)
    LDA #$FF                            ; 84DB  a9 ff
    STA tmp_23                          ; 84DD  85 23
    RTS                                 ; 84DF  60
sub_14_84E0:
    JSR UseTouchMatch                   ; 84E0  20 97 84
    BNE loc_14_84E6                     ; 84E3  d0 01
    RTS                                 ; 84E5  60
loc_14_84E6:
    LDY #$06                            ; 84E6  a0 06
    CLC                                 ; 84E8  18
    LDA #$20                            ; 84E9  a9 20
    ADC player_hp                       ; 84EB  6d c1 03
    BCC loc_14_84F2                     ; 84EE  90 02
    LDA #$FF                            ; 84F0  a9 ff
loc_14_84F2:
    CMP player_hp_max                   ; 84F2  cd c0 03
    BCC loc_14_84FA                     ; 84F5  90 03
    LDA player_hp_max                   ; 84F7  ad c0 03
loc_14_84FA:
    STA player_hp                       ; 84FA  8d c1 03
    STY tmp_2F                          ; 84FD  84 2f
    LDA #$25                            ; 84FF  a9 25
    JSR PlaySound                       ; 8501  20 25 c1
    LDY tmp_2F                          ; 8504  a4 2f
    RTS                                 ; 8506  60
sub_14_8507:
    CLC                                 ; 8507  18
    LDA mp_cur                          ; 8508  ad 08 07
    ADC #$20                            ; 850B  69 20
    BCC loc_14_8511                     ; 850D  90 02
    LDA #$FF                            ; 850F  a9 ff
loc_14_8511:
    CMP mp_max                          ; 8511  cd 09 07
    BCC loc_14_8519                     ; 8514  90 03
    LDA mp_max                          ; 8516  ad 09 07
loc_14_8519:
    STA mp_cur                          ; 8519  8d 08 07
    RTS                                 ; 851C  60
sub_14_851D:
    LDA mp_max                          ; 851D  ad 09 07
    STA mp_cur                          ; 8520  8d 08 07
    RTS                                 ; 8523  60
sub_14_8524:
    LDA obj1_flash                      ; 8524  ad 10 07
    AND #$0F                            ; 8527  29 0f
    CMP #$03                            ; 8529  c9 03
    BNE UseFail                         ; 852B  d0 ae
    BEQ loc_14_8553                     ; 852D  f0 24
sub_14_852F:
    LDA obj1_flash                      ; 852F  ad 10 07
    AND #$0F                            ; 8532  29 0f
    CMP #$01                            ; 8534  c9 01
    BNE UseFail                         ; 8536  d0 a3
    BEQ loc_14_8553                     ; 8538  f0 19
sub_14_853A:
    LDA obj1_flash                      ; 853A  ad 10 07
    AND #$0F                            ; 853D  29 0f
    CMP #$02                            ; 853F  c9 02
    BNE UseFail                         ; 8541  d0 98
    LDA #$00                            ; 8543  a9 00
    STA obj_freeze                      ; 8545  8d a0 05
    BEQ loc_14_8553                     ; 8548  f0 09
sub_14_854A:
    LDA obj1_flash                      ; 854A  ad 10 07
    AND #$0F                            ; 854D  29 0f
    CMP #$04                            ; 854F  c9 04
    BNE UseFail                         ; 8551  d0 88
loc_14_8553:
    LDA obj1_flash                      ; 8553  ad 10 07
    AND #$F0                            ; 8556  29 f0
    STA obj1_flash                      ; 8558  8d 10 07
    STY tmp_2F                          ; 855B  84 2f
    LDA #$25                            ; 855D  a9 25
    JSR PlaySound                       ; 855F  20 25 c1
    LDY tmp_2F                          ; 8562  a4 2f
sub_14_8564:
    RTS                                 ; 8564  60
sub_14_8565:
    JSR UseTouchMatch                   ; 8565  20 97 84
    BNE loc_14_8579                     ; 8568  d0 0f
    RTS                                 ; 856A  60
sub_14_856B:
    LDA area_id                         ; 856B  a5 6c
    CMP #$60                            ; 856D  c9 60
    BEQ loc_14_857C                     ; 856F  f0 0b
    CMP #$64                            ; 8571  c9 64
    BEQ loc_14_857C                     ; 8573  f0 07
    CMP #$68                            ; 8575  c9 68
    BEQ loc_14_857C                     ; 8577  f0 03
loc_14_8579:
    JMP UseFail                         ; 8579  4c db 84
loc_14_857C:
    JSR NpcFlagTest                     ; 857C  20 35 81
    BNE loc_14_8584                     ; 857F  d0 03
    JMP UseFail                         ; 8581  4c db 84
loc_14_8584:
    RTS                                 ; 8584  60
sub_14_8585:
    LDX #$18                            ; 8585  a2 18
loc_14_8587:
    LDA inv_643F,X                      ; 8587  bd 3f 64
    CMP #$38                            ; 858A  c9 38
    BEQ loc_14_8594                     ; 858C  f0 06
    DEX                                 ; 858E  ca
    BPL loc_14_8587                     ; 858F  10 f6
    JMP UseFail                         ; 8591  4c db 84
loc_14_8594:
    LDA #$3A                            ; 8594  a9 3a
    STA inv_643F,X                      ; 8596  9d 3f 64
    LDA #$68                            ; 8599  a9 68
    JMP PlaySound                       ; 859B  4c 25 c1
sub_14_859E:
    JSR UseTouchMatch                   ; 859E  20 97 84
    BNE loc_14_85A4                     ; 85A1  d0 01
    RTS                                 ; 85A3  60
loc_14_85A4:
    LDY #$06                            ; 85A4  a0 06
    JSR UseTouchMatch                   ; 85A6  20 97 84
    BNE loc_14_85AC                     ; 85A9  d0 01
    RTS                                 ; 85AB  60
loc_14_85AC:
    LDY #$0C                            ; 85AC  a0 0c
    JSR UseTouchMatch                   ; 85AE  20 97 84
    BNE loc_14_85B4                     ; 85B1  d0 01
    RTS                                 ; 85B3  60
loc_14_85B4:
    LDY #$12                            ; 85B4  a0 12
    JSR UseTouchMatch                   ; 85B6  20 97 84
    BNE loc_14_85BC                     ; 85B9  d0 01
    RTS                                 ; 85BB  60
loc_14_85BC:
    JMP UseFail                         ; 85BC  4c db 84
ChestPresence:  ; s72: $23=chest param; $20=1 if any cond of ChestPresencePtrs[p] true (NpcFlagTest; b7=last) else 0 -> SpawnH2 clears obj_active
    LDA #$00                            ; 85BF  a9 00
    STA tmp_20                          ; 85C1  85 20
    TAY                                 ; 85C3  a8
    LDA tmp_23                          ; 85C4  a5 23
    ASL A                               ; 85C6  0a
    TAX                                 ; 85C7  aa
    LDA ChestPresencePtrs,X             ; 85C8  bd 82 9c
    STA tmp_24                          ; 85CB  85 24
    LDA $9C83,X                         ; 85CD  bd 83 9c
    STA tmp_25                          ; 85D0  85 25
loc_14_85D2:
    JSR NpcFlagTest                     ; 85D2  20 35 81
    BEQ loc_14_85DA                     ; 85D5  f0 03
    INC tmp_20                          ; 85D7  e6 20
    RTS                                 ; 85D9  60
loc_14_85DA:
    LDA tmp_26                          ; 85DA  a5 26
    BPL loc_14_85D2                     ; 85DC  10 f4
    RTS                                 ; 85DE  60
    .db $36                             ; 14:85DF
NpcPresenceLo:  ; ptr16[128] NpcHidden lists for params 00-7F
    .db $80,$87,$80,$87,$80,$87,$80,$87,$80,$87,$80,$87,$80,$87,$80,$87; 14:85E0
    .db $80,$87,$9F,$87,$9F,$87,$AE,$87,$A7,$87,$BA,$87,$C8,$87,$C8,$87; 14:85F0
    .db $C8,$87,$C1,$87,$C8,$87,$80,$87,$CF,$87,$D8,$87,$DE,$87,$80,$87; 14:8600
    .db $80,$87,$80,$87,$80,$87,$F2,$87,$80,$87,$80,$87,$80,$87,$F6,$87; 14:8610
    .db $21,$89,$21,$89,$80,$87,$80,$87,$80,$87,$80,$87,$80,$87,$80,$87; 14:8620
    .db $80,$87,$80,$87,$80,$87,$80,$87,$01,$88,$09,$88,$10,$88,$10,$88; 14:8630
    .db $10,$88,$10,$88,$80,$87,$14,$88,$80,$87,$80,$87,$80,$87,$80,$87; 14:8640
    .db $1B,$88,$26,$88,$3A,$88,$81,$87,$30,$88,$80,$87,$80,$87,$80,$87; 14:8650
    .db $80,$87,$80,$87,$80,$87,$80,$87,$40,$88,$9F,$87,$9F,$87,$A3,$87; 14:8660
    .db $80,$87,$80,$87,$B5,$87,$80,$87,$80,$87,$80,$87,$47,$88,$47,$88; 14:8670
    .db $47,$88,$47,$88,$47,$88,$47,$88,$80,$87,$80,$87,$80,$87,$80,$87; 14:8680
    .db $80,$87,$80,$87,$80,$87,$80,$87,$80,$87,$69,$88,$81,$88,$92,$88; 14:8690
    .db $AB,$88,$80,$87,$B5,$88,$E0,$88,$9B,$87,$E6,$88,$80,$87,$80,$87; 14:86A0
    .db $F8,$88,$80,$87,$EA,$88,$FC,$88,$09,$89,$00,$89,$EE,$88,$80,$87; 14:86B0
    .db $6F,$88,$75,$88,$7B,$88,$F4,$88,$CB,$88,$0F,$89,$60,$88,$80,$87; 14:86C0
    .db $80,$87,$80,$87,$80,$87,$80,$87,$25,$89,$25,$89,$CB,$88,$8F,$87; 14:86D0
NpcPresenceHi:  ; ptr16[128] NpcHidden lists for params 80-FF (bosses $C0-$CC: !F100-!F10C)
    .db $85,$87,$89,$87,$80,$87,$13,$89,$19,$89,$36,$88,$81,$87,$2C,$88; 14:86E0
    .db $EC,$87,$80,$87,$B9,$87,$80,$87,$81,$87,$81,$87,$1D,$89,$81,$87; 14:86F0
    .db $81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87; 14:8700
    .db $81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87; 14:8710
    .db $81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87; 14:8720
    .db $81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87; 14:8730
    .db $81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87; 14:8740
    .db $81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87,$81,$87; 14:8750
    .db $29,$89,$2D,$89,$31,$89,$35,$89,$39,$89,$3D,$89,$41,$89,$45,$89; 14:8760
    .db $49,$89,$4D,$89,$51,$89,$55,$89,$59,$89,$81,$87,$81,$87,$81,$87; 14:8770
    .db $FF,$00,$80,$00,$FF,$8E,$A0,$26,$FF,$8E,$00,$26,$A0,$24,$FF,$65; 14:8780
    .db $A0,$13,$FF,$1B,$A0,$99,$FF,$1B,$80,$99,$FF,$D6,$80,$8B,$FF,$65; 14:8790
    .db $80,$13,$FF,$E8,$80,$13,$FF,$EF,$A0,$24,$72,$A0,$24,$FF,$BF,$A0; 14:87A0
    .db $24,$8E,$A0,$24,$FF,$EF,$A0,$24,$FF,$FF,$C0,$A0,$85,$35,$80,$85; 14:87B0
    .db $FF,$C0,$A0,$84,$32,$80,$84,$FF,$02,$A0,$84,$32,$80,$84,$FF,$C5; 14:87C0
    .db $A0,$3A,$0E,$00,$0F,$A0,$38,$FF,$0E,$00,$3A,$A0,$0F,$FF,$18,$A0; 14:87D0
    .db $50,$57,$20,$51,$00,$35,$A0,$34,$8C,$A0,$27,$FF,$57,$20,$35,$A0; 14:87E0
    .db $51,$FF,$C6,$A0,$48,$FF,$CE,$80,$45,$1A,$00,$52,$20,$53,$A0,$45; 14:87F0
    .db $FF,$28,$20,$66,$00,$48,$A0,$12,$FF,$28,$A0,$5B,$73,$A0,$8C,$FF; 14:8800
    .db $30,$80,$84,$FF,$DF,$A0,$20,$DE,$80,$20,$FF,$DF,$20,$20,$A0,$1F; 14:8810
    .db $E1,$00,$1F,$A0,$1E,$FF,$D8,$00,$20,$A0,$1F,$FF,$54,$A0,$6B,$FF; 14:8820
    .db $54,$00,$6B,$A0,$91,$FF,$55,$A0,$6A,$FF,$55,$00,$6A,$A0,$90,$FF; 14:8830
    .db $E9,$A0,$8F,$E4,$80,$8F,$FF,$8C,$A0,$27,$7E,$A0,$27,$F3,$A0,$27; 14:8840
    .db $F4,$A0,$27,$FF,$7E,$20,$9F,$A0,$27,$FF,$7E,$00,$9F,$A0,$27,$FF; 14:8850
    .db $8C,$80,$27,$7E,$00,$27,$A0,$E9,$FF,$8C,$00,$27,$A0,$E8,$FF,$8C; 14:8860
    .db $00,$27,$A0,$E0,$FF,$8C,$00,$27,$A0,$89,$FF,$8C,$00,$27,$A0,$8A; 14:8870
    .db $FF,$10,$00,$A5,$00,$0B,$A0,$51,$F2,$A0,$27,$AA,$A0,$55,$A6,$80; 14:8880
    .db $5E,$FF,$1E,$02,$F7,$A0,$0E,$21,$A0,$51,$F3,$20,$5F,$A0,$27,$F2; 14:8890
    .db $00,$5F,$A0,$27,$AC,$A0,$56,$A6,$80,$5E,$FF,$1E,$A0,$51,$EC,$A0; 14:88A0
    .db $61,$8C,$A0,$27,$FF,$E1,$00,$1E,$A0,$8F,$F4,$20,$5F,$A0,$27,$F2; 14:88B0
    .db $00,$5F,$A0,$27,$B9,$A0,$57,$A6,$80,$5E,$FF,$62,$00,$A4,$A0,$75; 14:88C0
    .db $EF,$00,$72,$A0,$DA,$F1,$00,$3E,$A0,$63,$BA,$00,$65,$A0,$D8,$FF; 14:88D0
    .db $64,$20,$25,$80,$1E,$FF,$01,$A0,$73,$FF,$6E,$A0,$13,$FF,$F2,$00; 14:88E0
    .db $5F,$A0,$2D,$FF,$A6,$80,$5E,$FF,$61,$A2,$FB,$FF,$62,$A0,$A4,$FF; 14:88F0
    .db $EF,$00,$61,$A0,$72,$8E,$A0,$27,$FF,$F1,$00,$72,$A0,$3E,$FF,$BA; 14:8900
    .db $A0,$65,$FF,$9C,$00,$6C,$A0,$79,$FF,$6E,$A0,$13,$FF,$6E,$A0,$13; 14:8910
    .db $FF,$1C,$80,$45,$FF,$1C,$A0,$45,$FF,$0A,$A1,$00,$FF,$1A,$A1,$01; 14:8920
    .db $FF,$28,$A1,$02,$FF,$43,$A1,$03,$FF,$F2,$A1,$04,$FF,$A9,$A1,$05; 14:8930
    .db $FF,$AC,$A1,$06,$FF,$B9,$A1,$07,$FF,$B6,$A1,$08,$FF,$A0,$A1,$09; 14:8940
    .db $FF,$A0,$A1,$0A,$FF,$9F,$A1,$0B,$FF,$6C,$A1,$0C,$FF; 14:8950
NpcScriptPtrsLo:  ; 128 script ptrs for params $00-$7F
    .db $86,$91,$61,$8D,$9C,$8D,$67,$91,$C1,$90,$C2,$8E,$4F,$91,$70,$90; 14:895D
    .db $1D,$8F,$3F,$93,$5A,$93,$33,$96,$29,$94,$43,$8B,$86,$8B,$BB,$8B; 14:896D
    .db $ED,$8B,$1B,$8C,$4D,$8C,$76,$8C,$C5,$8C,$F2,$8C,$CD,$96,$39,$8D; 14:897D
    .db $43,$8D,$4D,$8D,$57,$8D,$FC,$8C,$08,$8D,$AF,$8D,$E8,$8D,$15,$8E; 14:898D
    .db $3B,$8E,$5F,$8E,$83,$8E,$A7,$8E,$DF,$8E,$FD,$8E,$E9,$8E,$F3,$8E; 14:899D
    .db $13,$8F,$55,$8F,$3A,$8F,$30,$8F,$28,$8D,$79,$8F,$B1,$8C,$BB,$8C; 14:89AD
    .db $9D,$8C,$A7,$8C,$AB,$90,$55,$90,$30,$90,$FE,$90,$19,$91,$34,$91; 14:89BD
    .db $91,$8F,$04,$90,$E5,$91,$FD,$91,$0C,$92,$1B,$92,$7B,$92,$9B,$92; 14:89CD
    .db $BB,$92,$5B,$92,$3B,$92,$DB,$92,$90,$93,$FB,$92,$1D,$93,$75,$93; 14:89DD
    .db $D8,$93,$08,$8B,$4D,$94,$12,$8B,$25,$8B,$2F,$8B,$77,$94,$E8,$94; 14:89ED
    .db $03,$95,$B9,$94,$2A,$95,$D5,$94,$BF,$95,$FD,$95,$6E,$95,$89,$95; 14:89FD
    .db $A4,$95,$69,$96,$4B,$96,$7F,$96,$9C,$96,$1E,$95,$55,$97,$A6,$97; 14:8A0D
    .db $19,$97,$39,$8B,$EF,$97,$B9,$96,$A3,$91,$7A,$8B,$39,$8B,$39,$8B; 14:8A1D
    .db $60,$98,$C3,$96,$39,$8B,$6C,$98,$97,$98,$76,$98,$F4,$8A,$F3,$93; 14:8A2D
    .db $08,$97,$45,$95,$5D,$95,$39,$8B,$36,$98,$AD,$98,$AD,$94,$BD,$93; 14:8A3D
    .db $E2,$95,$0E,$94,$18,$96,$C5,$91,$3B,$8E,$5F,$8E,$36,$98,$FE,$8A; 14:8A4D
NpcScriptPtrsHi:  ; 128 script ptrs for params $80-$FF
    .db $E5,$8A,$E5,$8A,$39,$8B,$D2,$98,$C8,$98,$E5,$91,$FD,$91,$0C,$92; 14:8A5D
    .db $CD,$96,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$B7,$98,$39,$8B; 14:8A6D
    .db $39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B; 14:8A7D
    .db $39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B; 14:8A8D
    .db $39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B; 14:8A9D
    .db $39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B; 14:8AAD
    .db $39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B; 14:8ABD
    .db $39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B,$39,$8B; 14:8ACD
    .db $39,$8B,$39,$8B,$39,$8B,$E5,$98,$80,$00,$00,$00,$FF,$00,$26,$03; 14:8ADD
    .db $D4,$00,$A0,$00,$03,$D3,$00,$80,$00,$00,$00,$FF,$A0,$00,$02,$C1; 14:8AED
    .db $00,$80,$00,$00,$00,$FF,$A0,$00,$01,$A2,$00,$80,$00,$00,$00,$FF; 14:8AFD
    .db $A0,$00,$03,$CF,$00,$80,$00,$00,$00,$FF,$60,$A7,$02,$71,$00,$40; 14:8B0D
    .db $A7,$E0,$00,$01,$4B,$00,$C0,$A7,$80,$00,$00,$00,$FF,$A0,$00,$02; 14:8B1D
    .db $72,$00,$80,$00,$00,$00,$FF,$A0,$00,$02,$70,$00,$80,$00,$00,$00; 14:8B2D
    .db $FF,$A0,$00,$03,$C3,$00,$00,$28,$00,$12,$00,$2A,$00,$13,$00,$2B; 14:8B3D
    .db $00,$12,$80,$29,$00,$12,$C0,$00,$35,$16,$FF,$00,$47,$00,$11,$00; 14:8B4D
    .db $00,$0A,$00,$0F,$00,$60,$0B,$18,$0E,$00,$40,$0B,$A0,$00,$00,$0F; 14:8B5D
    .db $00,$60,$83,$00,$E9,$16,$40,$83,$A0,$00,$00,$EA,$16,$80,$00,$00; 14:8B6D
    .db $00,$FF,$E0,$00,$00,$09,$00,$40,$73,$00,$28,$03,$C2,$00,$2A,$03; 14:8B7D
    .db $C9,$00,$2B,$03,$C2,$80,$29,$03,$C2,$02,$00,$32,$16,$FF,$00,$0D; 14:8B8D
    .db $00,$0D,$00,$00,$0A,$00,$0B,$00,$60,$0C,$00,$0A,$00,$40,$0C,$A0; 14:8B9D
    .db $00,$00,$0B,$00,$00,$0D,$00,$0D,$16,$A0,$00,$00,$E1,$16,$00,$28; 14:8BAD
    .db $03,$C0,$00,$2A,$03,$C7,$00,$2B,$03,$C0,$80,$29,$03,$C0,$02,$00; 14:8BBD
    .db $32,$13,$FF,$00,$0D,$00,$02,$00,$40,$97,$00,$01,$00,$C0,$97,$E0; 14:8BCD
    .db $00,$00,$00,$00,$40,$97,$00,$0D,$00,$02,$13,$A0,$00,$00,$E0,$13; 14:8BDD
    .db $00,$28,$03,$C1,$00,$2A,$03,$C8,$00,$2B,$03,$C1,$80,$29,$03,$C1; 14:8BED
    .db $02,$00,$32,$0F,$FF,$00,$0D,$00,$08,$00,$00,$0A,$00,$07,$00,$A0; 14:8BFD
    .db $00,$00,$06,$00,$00,$0D,$00,$08,$0F,$A0,$00,$00,$E2,$0F,$00,$28; 14:8C0D
    .db $03,$C4,$00,$2A,$03,$CB,$00,$2B,$03,$C4,$80,$29,$03,$C4,$02,$00; 14:8C1D
    .db $32,$13,$FF,$00,$0D,$00,$05,$00,$40,$96,$00,$04,$00,$C0,$96,$E0; 14:8C2D
    .db $00,$00,$03,$00,$40,$96,$00,$0D,$00,$05,$13,$A0,$00,$00,$E3,$13; 14:8C3D
    .db $00,$28,$00,$18,$00,$2A,$03,$CC,$00,$2B,$00,$18,$80,$29,$00,$18; 14:8C4D
    .db $02,$00,$32,$0A,$FF,$00,$0D,$00,$19,$00,$A0,$00,$00,$18,$00,$00; 14:8C5D
    .db $0D,$00,$19,$0A,$A0,$00,$00,$E6,$0A,$00,$28,$00,$14,$00,$2A,$00; 14:8C6D
    .db $14,$00,$2B,$00,$14,$80,$29,$00,$14,$FF,$20,$0E,$00,$14,$00,$20; 14:8C7D
    .db $38,$00,$15,$00,$00,$0D,$00,$17,$00,$E0,$00,$00,$16,$00,$40,$A9; 14:8C8D
    .db $80,$2A,$03,$CF,$FF,$A0,$00,$00,$E7,$00,$80,$2A,$03,$CF,$FF,$A0; 14:8C9D
    .db $00,$00,$E8,$00,$80,$2A,$03,$D0,$FF,$A0,$00,$00,$E7,$00,$80,$2A; 14:8CAD
    .db $03,$CF,$FF,$A0,$00,$00,$E5,$00,$00,$28,$00,$24,$00,$2A,$00,$25; 14:8CBD
    .db $00,$2B,$00,$24,$80,$29,$00,$24,$C5,$0C,$0E,$00,$FF,$60,$88,$18; 14:8CCD
    .db $21,$00,$40,$88,$A0,$00,$00,$23,$00,$60,$A5,$48,$0C,$0C,$40,$A5; 14:8CDD
    .db $A0,$00,$00,$4A,$0C,$80,$00,$00,$00,$FF,$A0,$00,$00,$20,$00,$80; 14:8CED
    .db $00,$00,$00,$FF,$E0,$00,$68,$48,$00,$40,$48,$00,$28,$03,$D0,$00; 14:8CFD
    .db $2A,$03,$C9,$00,$2B,$03,$D0,$80,$29,$03,$D0,$FF,$20,$48,$00,$49; 14:8D0D
    .db $00,$00,$12,$00,$4C,$00,$A0,$00,$00,$49,$00,$80,$00,$00,$00,$FF; 14:8D1D
    .db $60,$12,$20,$AE,$00,$40,$12,$A0,$00,$04,$10,$00,$80,$2A,$03,$C9; 14:8D2D
    .db $FF,$A0,$00,$00,$43,$00,$80,$2A,$03,$C8,$FF,$A0,$00,$00,$44,$00; 14:8D3D
    .db $80,$2A,$03,$CA,$FF,$A0,$00,$00,$45,$00,$80,$2A,$03,$C8,$FF,$A0; 14:8D4D
    .db $00,$00,$47,$00,$80,$00,$00,$00,$C6,$00,$93,$19,$FF,$00,$28,$03; 14:8D5D
    .db $CF,$00,$00,$2A,$03,$CE,$00,$00,$2B,$03,$CF,$00,$00,$29,$03,$CF; 14:8D6D
    .db $00,$A0,$00,$03,$CF,$00,$00,$28,$03,$44,$19,$00,$2A,$03,$CA,$19; 14:8D7D
    .db $00,$2B,$03,$4D,$19,$00,$29,$03,$44,$19,$A0,$00,$03,$44,$19,$80; 14:8D8D
    .db $2A,$03,$C7,$C6,$00,$50,$05,$FF,$A0,$00,$00,$46,$00,$A0,$00,$01; 14:8D9D
    .db $11,$05,$20,$0E,$00,$9A,$00,$28,$03,$D7,$00,$2A,$03,$D7,$00,$2B; 14:8DAD
    .db $03,$D7,$80,$29,$03,$D7,$FF,$00,$43,$00,$99,$00,$40,$A6,$00,$98; 14:8DBD
    .db $00,$40,$A8,$40,$44,$00,$97,$00,$40,$A6,$00,$49,$00,$96,$00,$40; 14:8DCD
    .db $45,$18,$95,$00,$40,$49,$A0,$00,$00,$94,$00,$20,$0E,$00,$9A,$00; 14:8DDD
    .db $28,$00,$9A,$00,$2A,$00,$9A,$00,$2B,$00,$9A,$80,$29,$00,$9A,$FF; 14:8DED
    .db $00,$41,$00,$83,$00,$00,$A0,$00,$82,$00,$40,$45,$18,$81,$00,$40; 14:8DFD
    .db $A0,$E0,$00,$00,$80,$00,$40,$52,$20,$0E,$00,$9A,$00,$28,$00,$9A; 14:8E0D
    .db $00,$2A,$00,$9A,$00,$2B,$00,$9A,$80,$29,$00,$9A,$FF,$00,$44,$00; 14:8E1D
    .db $87,$00,$60,$45,$60,$85,$00,$40,$53,$A0,$00,$00,$86,$00,$20,$0E; 14:8E2D
    .db $00,$9A,$00,$28,$00,$9A,$00,$2A,$00,$9A,$00,$2B,$00,$9A,$80,$29; 14:8E3D
    .db $00,$9A,$FF,$00,$41,$00,$8B,$00,$00,$45,$00,$8A,$00,$A0,$00,$00; 14:8E4D
    .db $89,$00,$20,$0E,$00,$9A,$00,$28,$00,$9A,$00,$2A,$00,$9A,$00,$2B; 14:8E5D
    .db $00,$9A,$80,$29,$00,$9A,$FF,$00,$41,$00,$8F,$00,$00,$45,$00,$8E; 14:8E6D
    .db $00,$A0,$00,$00,$8D,$00,$20,$0E,$00,$9A,$00,$28,$00,$9A,$00,$2A; 14:8E7D
    .db $00,$9A,$00,$2B,$00,$9A,$80,$29,$00,$9A,$FF,$00,$41,$00,$92,$00; 14:8E8D
    .db $00,$45,$00,$91,$00,$A0,$00,$00,$90,$00,$80,$00,$00,$00,$FF,$20; 14:8E9D
    .db $28,$AA,$52,$00,$00,$10,$02,$51,$00,$60,$11,$02,$4E,$00,$40,$11; 14:8EAD
    .db $A0,$00,$02,$4F,$00,$80,$00,$00,$00,$1B,$00,$50,$0A,$FF,$20,$28; 14:8EBD
    .db $02,$42,$00,$A0,$00,$02,$43,$00,$00,$2A,$03,$C8,$0A,$A0,$00,$01; 14:8ECD
    .db $10,$0A,$A0,$28,$02,$44,$FF,$A0,$00,$02,$45,$00,$A0,$28,$02,$4B; 14:8EDD
    .db $FF,$A0,$00,$02,$49,$00,$A0,$28,$02,$4B,$FF,$A0,$00,$02,$4A,$00; 14:8EED
    .db $A0,$28,$02,$46,$FF,$E0,$00,$02,$47,$00,$C0,$00,$A0,$28,$02,$46; 14:8EFD
    .db $FF,$A0,$00,$02,$47,$00,$80,$2A,$00,$A1,$FF,$A0,$00,$00,$A0,$00; 14:8F0D
    .db $80,$2A,$03,$C7,$D5,$00,$18,$05,$FF,$A0,$00,$00,$A8,$00,$A0,$00; 14:8F1D
    .db $00,$40,$05,$80,$2A,$03,$C9,$FF,$A0,$00,$00,$A9,$00,$00,$28,$00; 14:8F2D
    .db $A7,$00,$2A,$03,$C9,$00,$2B,$00,$A7,$80,$29,$00,$A7,$FF,$00,$0D; 14:8F3D
    .db $00,$A6,$00,$A0,$00,$00,$A5,$00,$00,$28,$03,$C1,$00,$2A,$03,$C8; 14:8F4D
    .db $00,$2B,$03,$C1,$80,$29,$03,$C1,$FF,$00,$98,$00,$A4,$00,$40,$12; 14:8F5D
    .db $03,$D9,$00,$40,$98,$E0,$00,$00,$A2,$00,$40,$98,$80,$00,$00,$00; 14:8F6D
    .db $28,$00,$73,$05,$FF,$A0,$00,$08,$B0,$00,$00,$2A,$42,$75,$05,$A0; 14:8F7D
    .db $00,$0A,$74,$05,$80,$00,$00,$00,$FF,$00,$1E,$01,$62,$00,$40,$1F; 14:8F8D
    .db $81,$60,$00,$40,$1E,$40,$16,$01,$4E,$00,$40,$9C,$40,$17,$01,$4D; 14:8F9D
    .db $00,$00,$16,$40,$9C,$00,$92,$01,$4E,$00,$40,$18,$19,$4C,$00,$00; 14:8FAD
    .db $92,$00,$A3,$40,$9C,$40,$19,$01,$4A,$00,$40,$9C,$60,$1A,$71,$44; 14:8FBD
    .db $00,$00,$9C,$00,$19,$40,$1A,$40,$D7,$01,$49,$00,$80,$D7,$80,$D6; 14:8FCD
    .db $80,$D5,$00,$9C,$40,$19,$40,$D6,$01,$48,$00,$00,$D7,$00,$9C,$40; 14:8FDD
    .db $19,$40,$D5,$01,$47,$00,$00,$D6,$00,$9C,$40,$19,$60,$00,$01,$46; 14:8FED
    .db $00,$00,$D5,$00,$9C,$40,$19,$80,$00,$00,$00,$FF,$40,$1B,$01,$41; 14:8FFD
    .db $00,$00,$20,$40,$1F,$40,$17,$01,$43,$00,$C0,$20,$40,$A3,$01,$42; 14:900D
    .db $00,$C0,$20,$40,$1D,$01,$41,$00,$C0,$20,$E0,$00,$01,$40,$00,$80; 14:901D
    .db $20,$40,$1D,$00,$28,$01,$06,$00,$2A,$03,$CD,$00,$2B,$01,$06,$80; 14:902D
    .db $29,$01,$06,$FF,$00,$1E,$01,$07,$00,$00,$1F,$01,$05,$00,$20,$20; 14:903D
    .db $01,$04,$00,$A0,$00,$01,$06,$00,$00,$28,$01,$02,$00,$2A,$03,$CD; 14:904D
    .db $00,$2B,$01,$02,$80,$29,$01,$02,$FF,$20,$20,$01,$03,$00,$A0,$00; 14:905D
    .db $01,$02,$00,$80,$00,$00,$00,$DE,$00,$61,$19,$FF,$00,$28,$01,$00; 14:906D
    .db $00,$00,$2A,$03,$CD,$00,$00,$2B,$01,$00,$00,$00,$29,$01,$00,$00; 14:907D
    .db $A0,$00,$01,$00,$00,$00,$28,$02,$20,$19,$00,$2A,$03,$CD,$19,$00; 14:908D
    .db $2B,$02,$20,$19,$00,$29,$02,$20,$19,$A0,$00,$02,$20,$19,$00,$28; 14:909D
    .db $01,$01,$00,$2A,$03,$CD,$00,$2B,$01,$01,$80,$29,$01,$01,$FF,$A0; 14:90AD
    .db $00,$01,$01,$00,$80,$00,$00,$00,$E0,$00,$93,$19,$1B,$28,$FF,$00; 14:90BD
    .db $28,$01,$08,$00,$00,$2A,$03,$C8,$00,$00,$2B,$01,$08,$00,$00,$29; 14:90CD
    .db $01,$08,$00,$A0,$00,$01,$08,$00,$00,$2B,$03,$4B,$19,$00,$2A,$03; 14:90DD
    .db $C8,$19,$A0,$00,$03,$40,$19,$20,$28,$02,$40,$28,$A0,$00,$02,$41; 14:90ED
    .db $28,$00,$28,$01,$0E,$00,$2A,$03,$C7,$00,$2B,$01,$0E,$80,$29,$01; 14:90FD
    .db $0E,$FF,$20,$1B,$01,$0E,$00,$A0,$00,$01,$0F,$00,$00,$28,$01,$0A; 14:910D
    .db $00,$2A,$03,$C8,$00,$2B,$01,$0A,$80,$29,$01,$0A,$FF,$00,$1E,$01; 14:911D
    .db $0A,$00,$A0,$00,$01,$09,$00,$00,$28,$01,$0A,$00,$2A,$03,$C8,$00; 14:912D
    .db $2B,$01,$0A,$80,$29,$01,$0A,$FF,$00,$1B,$01,$0C,$00,$A0,$00,$01; 14:913D
    .db $0B,$00,$80,$2A,$03,$C8,$50,$00,$93,$05,$FF,$A0,$00,$01,$12,$00; 14:914D
    .db $00,$2B,$03,$4B,$05,$A0,$00,$03,$43,$05,$80,$2A,$03,$C7,$50,$00; 14:915D
    .db $EF,$05,$93,$0A,$FF,$A0,$00,$01,$13,$00,$A0,$00,$03,$CF,$05,$00; 14:916D
    .db $2B,$03,$49,$0A,$A0,$00,$03,$41,$0A,$80,$2A,$03,$C9,$50,$00,$93; 14:917D
    .db $0A,$FF,$00,$1E,$01,$15,$00,$A0,$00,$01,$14,$00,$00,$2B,$03,$4C; 14:918D
    .db $0A,$A0,$00,$03,$42,$0A,$00,$28,$01,$20,$00,$2A,$03,$CA,$00,$2B; 14:919D
    .db $01,$20,$80,$29,$01,$20,$FF,$00,$21,$01,$22,$00,$60,$4A,$01,$20; 14:91AD
    .db $00,$40,$4A,$A0,$00,$01,$23,$00,$00,$28,$01,$25,$00,$2A,$03,$CC; 14:91BD
    .db $00,$2B,$01,$25,$80,$29,$01,$25,$FF,$20,$8B,$01,$26,$00,$00,$21; 14:91CD
    .db $01,$25,$00,$A0,$00,$01,$24,$00,$80,$2A,$03,$C7,$FF,$20,$6A,$03; 14:91DD
    .db $C7,$00,$40,$D9,$01,$A0,$00,$C0,$D9,$E0,$00,$01,$A3,$00,$40,$D9; 14:91ED
    .db $80,$2A,$03,$C8,$FF,$00,$6A,$01,$A2,$00,$A0,$00,$03,$C8,$00,$80; 14:91FD
    .db $2A,$03,$C9,$FF,$00,$6B,$01,$A3,$00,$A0,$00,$03,$C9,$00,$00,$28; 14:920D
    .db $01,$E3,$00,$2A,$01,$E4,$00,$2B,$01,$E3,$80,$29,$01,$E3,$FF,$20; 14:921D
    .db $13,$01,$E0,$00,$00,$8F,$01,$E2,$00,$A0,$00,$01,$E1,$00,$00,$28; 14:922D
    .db $03,$C2,$00,$2A,$03,$C9,$00,$2B,$03,$C2,$80,$29,$03,$C2,$FF,$20; 14:923D
    .db $13,$01,$EF,$00,$00,$8F,$01,$F1,$00,$A0,$00,$01,$F0,$00,$00,$28; 14:924D
    .db $03,$C2,$00,$2A,$03,$C9,$00,$2B,$03,$C2,$80,$29,$03,$C2,$FF,$20; 14:925D
    .db $13,$01,$EC,$00,$00,$8F,$01,$EE,$00,$A0,$00,$01,$ED,$00,$00,$28; 14:926D
    .db $03,$C1,$00,$2A,$03,$C8,$00,$2B,$03,$C1,$80,$29,$03,$C1,$FF,$20; 14:927D
    .db $13,$01,$E5,$00,$00,$8F,$01,$E7,$00,$A0,$00,$01,$E6,$00,$00,$28; 14:928D
    .db $03,$C1,$00,$2A,$03,$C8,$00,$2B,$03,$C1,$80,$29,$03,$C1,$FF,$20; 14:929D
    .db $13,$01,$E8,$00,$00,$8F,$03,$C1,$00,$A0,$00,$01,$E9,$00,$00,$28; 14:92AD
    .db $03,$C2,$00,$2A,$03,$C9,$00,$2B,$03,$C2,$80,$29,$03,$C2,$FF,$20; 14:92BD
    .db $13,$01,$EA,$00,$00,$8F,$03,$C2,$00,$A0,$00,$01,$EB,$00,$00,$28; 14:92CD
    .db $03,$C1,$00,$2A,$03,$C8,$00,$2B,$03,$C1,$80,$29,$03,$C1,$FF,$20; 14:92DD
    .db $13,$01,$F2,$00,$00,$8F,$01,$F4,$00,$A0,$00,$01,$F3,$00,$00,$28; 14:92ED
    .db $03,$C3,$00,$2A,$03,$CA,$00,$2B,$03,$C3,$80,$29,$03,$C3,$FF,$00; 14:92FD
    .db $8F,$03,$C3,$00,$20,$30,$02,$03,$00,$40,$30,$A0,$00,$00,$64,$00; 14:930D
    .db $00,$28,$03,$C2,$00,$2A,$03,$C9,$00,$2B,$03,$C2,$80,$29,$03,$C2; 14:931D
    .db $FF,$00,$8F,$02,$07,$00,$60,$31,$02,$05,$00,$40,$31,$A0,$00,$02; 14:932D
    .db $06,$00,$00,$28,$03,$C0,$00,$2A,$03,$C9,$00,$2B,$03,$C0,$80,$29; 14:933D
    .db $03,$C0,$FF,$00,$8F,$02,$09,$00,$A0,$00,$02,$08,$00,$00,$28,$03; 14:934D
    .db $C1,$00,$2A,$03,$C8,$00,$2B,$03,$C1,$80,$29,$03,$C1,$FF,$00,$8F; 14:935D
    .db $02,$0B,$00,$A0,$00,$02,$0A,$00,$00,$28,$03,$C4,$00,$2A,$03,$CB; 14:936D
    .db $00,$2B,$03,$C4,$80,$29,$03,$C4,$FF,$00,$8F,$02,$0D,$00,$A0,$00; 14:937D
    .db $02,$0C,$00,$00,$28,$01,$F6,$00,$2A,$01,$F7,$00,$2B,$01,$F6,$80; 14:938D
    .db $29,$01,$F6,$E4,$00,$E9,$05,$FF,$A0,$00,$01,$F5,$00,$20,$13,$02; 14:939D
    .db $00,$05,$60,$8D,$1A,$01,$05,$00,$8D,$40,$32,$A0,$00,$02,$02,$05; 14:93AD
    .db $00,$28,$03,$C0,$00,$2A,$03,$C7,$00,$2B,$03,$C0,$80,$29,$03,$C0; 14:93BD
    .db $FF,$00,$24,$02,$68,$00,$A0,$00,$02,$67,$00,$00,$28,$03,$C2,$00; 14:93CD
    .db $2A,$03,$C9,$00,$2B,$03,$C2,$80,$29,$03,$C2,$FF,$00,$24,$02,$6A; 14:93DD
    .db $00,$A0,$00,$02,$69,$00,$00,$28,$03,$C3,$00,$2A,$03,$CA,$00,$2B; 14:93ED
    .db $03,$C3,$80,$29,$03,$C3,$FF,$00,$24,$02,$6C,$00,$A0,$00,$02,$6B; 14:93FD
    .db $00,$00,$28,$03,$C1,$00,$2A,$03,$C8,$00,$2B,$03,$C1,$80,$29,$03; 14:940D
    .db $C1,$FF,$00,$24,$02,$6E,$00,$A0,$00,$02,$6D,$00,$80,$00,$00,$00; 14:941D
    .db $EF,$00,$72,$0A,$F1,$14,$FF,$00,$2A,$03,$D2,$00,$A0,$00,$03,$D4; 14:942D
    .db $00,$00,$2A,$03,$D2,$0A,$A0,$00,$03,$D4,$0A,$A0,$00,$03,$D1,$14; 14:943D
    .db $00,$28,$03,$D3,$00,$2A,$03,$D2,$00,$2B,$03,$D3,$80,$29,$03,$D3; 14:944D
    .db $FF,$A0,$00,$03,$D3,$00,$80,$2A,$03,$D2,$FF,$A0,$00,$02,$70,$00; 14:945D
    .db $80,$00,$00,$00,$FF,$A0,$00,$5A,$71,$00,$00,$28,$02,$80,$00,$2A; 14:946D
    .db $02,$82,$00,$2B,$02,$81,$80,$29,$02,$80,$FF,$00,$26,$02,$84,$00; 14:947D
    .db $A0,$00,$02,$80,$00,$00,$28,$02,$80,$00,$2A,$02,$82,$00,$2B,$02; 14:948D
    .db $81,$80,$29,$02,$80,$FF,$00,$26,$02,$84,$00,$A0,$00,$02,$80,$00; 14:949D
    .db $80,$00,$00,$00,$FF,$E0,$00,$04,$10,$00,$40,$E9,$80,$00,$00,$00; 14:94AD
    .db $FF,$40,$4D,$02,$8F,$00,$80,$4D,$C0,$4C,$40,$4C,$02,$8E,$00,$40; 14:94BD
    .db $4D,$E0,$00,$02,$8E,$00,$40,$4C,$80,$00,$00,$00,$FF,$40,$4B,$02; 14:94CD
    .db $95,$00,$C0,$4B,$E0,$00,$02,$96,$00,$40,$4B,$00,$28,$02,$85,$00; 14:94DD
    .db $2A,$02,$82,$00,$2B,$02,$85,$80,$29,$02,$85,$FF,$00,$2D,$02,$84; 14:94ED
    .db $00,$A0,$00,$02,$85,$00,$00,$28,$02,$86,$00,$2A,$02,$83,$00,$2B; 14:94FD
    .db $02,$86,$80,$29,$02,$86,$FF,$00,$2D,$02,$84,$00,$A0,$00,$02,$86; 14:950D
    .db $00,$80,$00,$00,$00,$FF,$E0,$00,$04,$10,$00,$40,$E8,$00,$28,$02; 14:951D
    .db $A9,$00,$2A,$02,$AA,$00,$2B,$02,$AB,$80,$29,$02,$A9,$FF,$00,$2D; 14:952D
    .db $02,$94,$00,$A0,$00,$02,$93,$00,$80,$00,$00,$00,$FF,$00,$89,$04; 14:953D
    .db $10,$00,$40,$2B,$03,$22,$00,$40,$89,$E0,$00,$03,$21,$00,$40,$89; 14:954D
    .db $80,$00,$00,$00,$FF,$60,$8A,$03,$23,$00,$40,$8A,$A0,$00,$04,$10; 14:955D
    .db $00,$00,$28,$03,$C1,$00,$2A,$03,$C8,$00,$2B,$03,$C1,$80,$29,$03; 14:956D
    .db $C1,$FF,$00,$24,$02,$ED,$00,$A0,$00,$02,$EC,$00,$00,$28,$03,$CF; 14:957D
    .db $00,$2A,$03,$CE,$00,$2B,$03,$CF,$80,$29,$03,$CF,$FF,$00,$24,$02; 14:958D
    .db $EB,$00,$A0,$00,$02,$EA,$00,$00,$28,$03,$C1,$00,$2A,$03,$C8,$00; 14:959D
    .db $2B,$03,$C1,$80,$29,$03,$C1,$FF,$00,$24,$02,$E9,$00,$A0,$00,$02; 14:95AD
    .db $E8,$00,$00,$2A,$03,$C9,$00,$28,$03,$C2,$80,$2B,$03,$C2,$FF,$20; 14:95BD
    .db $24,$02,$E4,$00,$20,$29,$02,$E5,$00,$60,$4F,$1A,$E6,$00,$40,$4F; 14:95CD
    .db $A0,$00,$02,$E7,$00,$00,$28,$03,$C0,$00,$2A,$03,$C7,$00,$2B,$03; 14:95DD
    .db $C0,$80,$29,$03,$C0,$FF,$00,$24,$02,$E1,$00,$A0,$00,$02,$E0,$00; 14:95ED
    .db $00,$28,$03,$C3,$00,$2A,$03,$CA,$00,$2B,$03,$C3,$80,$29,$03,$C3; 14:95FD
    .db $FF,$00,$24,$02,$F0,$00,$A0,$00,$02,$EF,$00,$00,$28,$03,$C1,$00; 14:960D
    .db $2A,$03,$C8,$00,$2B,$03,$C1,$80,$29,$03,$C1,$FF,$00,$24,$02,$E3; 14:961D
    .db $00,$A0,$00,$02,$E2,$00,$80,$00,$00,$00,$8E,$00,$BF,$0A,$FF,$00; 14:962D
    .db $2A,$03,$D2,$00,$A0,$00,$03,$D3,$00,$A0,$00,$03,$D1,$0A,$00,$28; 14:963D
    .db $03,$53,$00,$2A,$03,$53,$80,$29,$03,$53,$FF,$00,$2E,$03,$51,$00; 14:964D
    .db $40,$2B,$1B,$50,$00,$40,$2E,$A0,$00,$03,$4F,$00,$00,$28,$03,$52; 14:965D
    .db $00,$2A,$03,$52,$00,$2B,$03,$52,$80,$29,$03,$52,$FF,$A0,$00,$03; 14:966D
    .db $4E,$00,$00,$28,$03,$C3,$00,$2A,$03,$CA,$00,$2B,$03,$4D,$80,$29; 14:967D
    .db $03,$C3,$FF,$60,$22,$03,$45,$00,$40,$22,$A0,$00,$03,$46,$00,$00; 14:968D
    .db $28,$03,$C5,$00,$2A,$03,$CC,$00,$2B,$03,$C5,$80,$29,$03,$C5,$FF; 14:969D
    .db $60,$23,$03,$48,$00,$40,$23,$A0,$00,$03,$47,$00,$80,$00,$00,$00; 14:96AD
    .db $FF,$A0,$00,$01,$80,$00,$80,$00,$00,$00,$FF,$A0,$00,$C1,$82,$00; 14:96BD
    .db $00,$28,$02,$A1,$00,$2A,$02,$A2,$00,$2B,$02,$A1,$80,$29,$02,$A0; 14:96CD
    .db $18,$00,$57,$05,$8C,$11,$FF,$A0,$00,$00,$41,$00,$40,$35,$C9,$A1; 14:96DD
    .db $05,$40,$34,$A0,$00,$01,$A4,$05,$00,$2D,$02,$89,$11,$60,$36,$02; 14:96ED
    .db $87,$11,$40,$36,$E0,$00,$02,$88,$11,$40,$36,$80,$00,$00,$00,$FF; 14:96FD
    .db $60,$E0,$03,$20,$00,$40,$E0,$A0,$00,$04,$10,$00,$00,$28,$02,$A5; 14:970D
    .db $00,$2A,$02,$A5,$00,$2B,$02,$A6,$80,$29,$02,$A5,$1E,$00,$EC,$0F; 14:971D
    .db $8C,$16,$FF,$00,$41,$00,$65,$00,$02,$F7,$00,$66,$00,$A0,$00,$00; 14:972D
    .db $64,$00,$E0,$00,$12,$6F,$0F,$40,$61,$00,$27,$23,$23,$16,$00,$2D; 14:973D
    .db $02,$8D,$16,$A0,$00,$02,$8C,$16,$00,$28,$02,$AC,$00,$2A,$02,$AC; 14:974D
    .db $00,$2B,$02,$AC,$80,$29,$02,$AC,$10,$00,$F2,$22,$AA,$33,$FF,$00; 14:975D
    .db $0D,$00,$1D,$00,$00,$38,$00,$1C,$00,$00,$39,$00,$1D,$00,$40,$0A; 14:976D
    .db $18,$1B,$00,$40,$39,$60,$3A,$00,$1A,$00,$40,$3A,$A0,$00,$00,$1D; 14:977D
    .db $00,$00,$2D,$02,$C3,$22,$60,$3B,$8A,$97,$22,$40,$3B,$A0,$00,$02; 14:978D
    .db $98,$22,$E0,$00,$BB,$01,$33,$40,$55,$00,$28,$02,$A3,$00,$2A,$02; 14:979D
    .db $A4,$00,$2B,$02,$A3,$80,$29,$02,$A3,$1E,$00,$21,$05,$F3,$16,$F2; 14:97AD
    .db $16,$AC,$27,$FF,$A0,$00,$00,$62,$00,$00,$3F,$00,$AD,$05,$40,$4E; 14:97BD
    .db $18,$AB,$05,$40,$3F,$A0,$00,$00,$AA,$05,$00,$2D,$02,$C5,$16,$00; 14:97CD
    .db $40,$02,$8B,$16,$E0,$00,$02,$8A,$16,$40,$40,$E0,$00,$BB,$03,$27; 14:97DD
    .db $40,$56,$00,$28,$02,$A7,$00,$2A,$02,$A8,$00,$2B,$02,$A7,$80,$29; 14:97ED
    .db $02,$A7,$E1,$00,$F4,$11,$F2,$11,$B9,$27,$FF,$00,$3C,$01,$63,$00; 14:97FD
    .db $60,$1E,$81,$60,$00,$40,$1E,$A0,$00,$01,$62,$00,$00,$2D,$02,$C7; 14:980D
    .db $11,$00,$3B,$02,$92,$11,$60,$3D,$02,$90,$11,$40,$3D,$A0,$00,$02; 14:981D
    .db $91,$11,$E0,$00,$BB,$05,$27,$40,$57,$80,$00,$00,$00,$EF,$00,$F1; 14:982D
    .db $05,$62,$0A,$BA,$16,$FF,$E0,$00,$9A,$66,$00,$A0,$00,$02,$61,$05; 14:983D
    .db $40,$A4,$51,$F8,$0A,$40,$75,$A0,$00,$03,$E0,$0A,$E0,$00,$A3,$07; 14:984D
    .db $16,$40,$D8,$80,$00,$00,$00,$FF,$E0,$00,$02,$21,$00,$40,$9B,$80; 14:985D
    .db $00,$00,$00,$FF,$A0,$00,$03,$E0,$00,$80,$00,$00,$00,$EF,$00,$8E; 14:986D
    .db $0C,$FF,$40,$70,$9A,$66,$00,$40,$72,$A0,$00,$02,$65,$00,$60,$26; 14:987D
    .db $03,$DA,$0C,$40,$76,$A0,$00,$02,$EE,$0C,$80,$00,$00,$00,$FF,$00; 14:988D
    .db $3E,$02,$61,$00,$40,$71,$B2,$66,$00,$40,$3E,$A0,$00,$02,$63,$00; 14:989D
    .db $80,$00,$00,$00,$FF,$A0,$00,$2B,$06,$00,$80,$00,$00,$00,$FF,$60; 14:98AD
    .db $ED,$03,$6C,$00,$40,$ED,$A0,$00,$03,$6D,$00,$80,$00,$00,$00,$FF; 14:98BD
    .db $A0,$00,$02,$0F,$00,$80,$00,$00,$00,$FF,$60,$82,$1B,$61,$00,$00; 14:98CD
    .db $79,$40,$82,$A0,$00,$03,$62,$00,$80,$00,$00,$00,$FF,$00,$17,$D1; 14:98DD
    .db $C3,$00,$A0,$00,$D9,$C0,$00     ; 14:98ED
TelepathyHintClass:  ; 256 bytes: area -> hint class 0-6
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 14:98F4
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$02,$00,$00,$00,$00; 14:9904
    .db $00,$00,$00,$00,$00,$00,$00,$00,$01,$01,$01,$01,$01,$01,$01,$01; 14:9914
    .db $01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$01; 14:9924
    .db $02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02; 14:9934
    .db $02,$02,$02,$02,$02,$02,$02,$02,$06,$06,$06,$06,$06,$06,$06,$06; 14:9944
    .db $03,$03,$03,$03,$02,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03; 14:9954
    .db $03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03; 14:9964
    .db $03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$05; 14:9974
    .db $06,$05,$05,$06,$06,$06,$06,$06,$06,$05,$05,$05,$06,$06,$06,$06; 14:9984
    .db $06,$06,$06,$06,$06,$06,$06,$06,$04,$05,$05,$05,$05,$05,$05,$05; 14:9994
    .db $05,$05,$05,$05,$05,$05,$05,$05,$05,$05,$05,$00,$04,$04,$04,$04; 14:99A4
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 14:99B4
    .db $00,$02,$02,$02,$02,$01,$02,$02,$02,$02,$02,$02,$02,$02,$02,$02; 14:99C4
    .db $02,$02,$02,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03,$03; 14:99D4
    .db $03,$03,$03,$03,$03,$03,$03,$03,$06,$06,$06,$06,$00,$00,$00,$00; 14:99E4
TelepathyHintLists:  ; 28 ptrs [(class*4+sage)]: flagref msg [alt] entries; first FALSE cond wins
    .db $7C,$9A,$4C,$9A,$B4,$9A,$E0,$9A,$82,$9A,$52,$9A,$B4,$9A,$E0,$9A; 14:99F4
    .db $86,$9A,$56,$9A,$B8,$9A,$E0,$9A,$90,$9A,$5C,$9A,$C0,$9A,$E0,$9A; 14:9A04
    .db $90,$9A,$5C,$9A,$C0,$9A,$E0,$9A,$9C,$9A,$6A,$9A,$CC,$9A,$F4,$9A; 14:9A14
    .db $A8,$9A,$72,$9A,$D4,$9A,$F8,$9A ; 14:9A24
TelepathyMsgTable:  ; words (hi lo linear msg id) rows x 4 sages
    .db $04,$0E,$04,$0E,$04,$0E,$04,$0E,$03,$A1,$03,$8D,$03,$AE,$03,$BC; 14:9A2C
    .db $03,$A2,$03,$8E,$03,$AF,$03,$BD,$00,$AC,$02,$73,$03,$4A,$02,$64; 14:9A3C
    .db $40,$4E,$03,$80,$03,$81,$80,$77,$03,$82,$C0,$1E,$03,$83,$03,$84; 14:9A4C
    .db $00,$75,$03,$85,$00,$63,$03,$86,$C0,$27,$03,$87,$03,$88,$00,$55; 14:9A5C
    .db $03,$89,$80,$2E,$03,$8A,$00,$79,$03,$8B,$C0,$5E,$03,$8C,$03,$8A; 14:9A6C
    .db $C0,$41,$03,$93,$03,$94,$80,$77,$03,$95,$40,$7B,$03,$96,$03,$97; 14:9A7C
    .db $80,$7A,$03,$98,$00,$13,$03,$99,$00,$63,$03,$9A,$80,$27,$03,$9B; 14:9A8C
    .db $00,$56,$03,$9D,$00,$65,$03,$9E,$80,$7C,$03,$9C,$00,$2E,$03,$9F; 14:9A9C
    .db $00,$79,$03,$9C,$80,$5E,$03,$A0,$80,$1E,$03,$A3,$00,$8B,$03,$A5; 14:9AAC
    .db $80,$10,$03,$A4,$40,$63,$03,$A7,$03,$A6,$C0,$27,$03,$A8,$03,$A9; 14:9ABC
    .db $00,$57,$03,$AA,$80,$7D,$03,$AB,$00,$7D,$03,$AB,$00,$79,$03,$AC; 14:9ACC
    .db $80,$5E,$03,$AD,$40,$1E,$03,$B4,$03,$B5,$00,$63,$03,$B6,$00,$5F; 14:9ADC
    .db $03,$B7,$C0,$27,$03,$B8,$03,$B9,$80,$65,$03,$B4,$00,$79,$03,$BA; 14:9AEC
    .db $80,$5E,$03,$BB                 ; 14:9AFC
ItemGetTable:  ; 113 words (items 00-70) -> start count r0 r1 set-list next
    .db $3A,$9F,$41,$9F,$48,$9F,$4F,$9F,$56,$9F,$5D,$9F,$83,$9F,$64,$9F; 14:9B00
    .db $8A,$9F,$6F,$9F,$93,$9F,$7C,$9F,$9A,$9F,$2D,$A0,$2D,$A0,$2D,$A0; 14:9B10
    .db $A1,$9F,$2D,$A0,$26,$A0,$2D,$A0,$0F,$9F,$FD,$9E,$FD,$9E,$FD,$9E; 14:9B20
    .db $FD,$9E,$FD,$9E,$FD,$9E,$16,$9F,$04,$9F,$09,$9E,$6B,$9E,$56,$9E; 14:9B30
    .db $87,$9E,$8E,$9E,$B1,$9E,$D4,$9E,$DD,$9E,$A8,$9F,$31,$9F,$AF,$9F; 14:9B40
    .db $B6,$9F,$18,$A0,$F6,$9E,$D9,$9F,$11,$A0,$BD,$9F,$C4,$9F,$E0,$9F; 14:9B50
    .db $18,$A0,$FC,$9F,$1F,$A0,$23,$9F,$1F,$A0,$2A,$9F,$F5,$9F,$1F,$A0; 14:9B60
    .db $EE,$9F,$1F,$A0,$1F,$A0,$CB,$9F,$F4,$9D,$EB,$9E,$1F,$A0,$E7,$9F; 14:9B70
    .db $D2,$9F,$3B,$A0,$42,$A0,$49,$A0,$50,$A0,$34,$A0,$5E,$A0,$57,$A0; 14:9B80
    .db $34,$A0,$FC,$9F,$3A,$9F,$3A,$9F,$3A,$9F,$3A,$9F,$3A,$9F,$3A,$9F; 14:9B90
    .db $10,$9E,$1D,$9F,$17,$9E,$1E,$9E,$25,$9E,$2C,$9E,$33,$9E,$3A,$9E; 14:9BA0
    .db $41,$9E,$48,$9E,$4F,$9E,$02,$9E,$5D,$9E,$64,$9E,$72,$9E,$79,$9E; 14:9BB0
    .db $80,$9E,$95,$9E,$9C,$9E,$A3,$9E,$AA,$9E,$B8,$9E,$BF,$9E,$3A,$9F; 14:9BC0
    .db $3A,$9F,$FB,$9D,$E4,$9E,$C6,$9E,$CD,$9E,$E6,$9D,$ED,$9D,$03,$A0; 14:9BD0
    .db $0A,$A0                         ; 14:9BE0
ItemUseTable:  ; 80 words (items 00-4F) -> use records
    .db $65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0; 14:9BE2
    .db $65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0; 14:9BF2
    .db $65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0; 14:9C02
    .db $65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$77,$A0,$77,$A0; 14:9C12
    .db $73,$A0,$6F,$A0,$6F,$A0,$73,$A0,$84,$A0,$88,$A0,$7B,$A0,$7F,$A0; 14:9C22
    .db $DC,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0; 14:9C32
    .db $65,$A0,$8E,$A0,$9A,$A0,$9F,$A0,$A7,$A0,$AD,$A0,$B3,$A0,$C5,$A0; 14:9C42
    .db $65,$A0,$B9,$A0,$BD,$A0,$CA,$A0,$D0,$A0,$D6,$A0,$F4,$A0,$FA,$A0; 14:9C52
    .db $00,$A1,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0; 14:9C62
    .db $65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0,$65,$A0; 14:9C72
ChestPresencePtrs:  ; s72 (was ItemAuxPtrs): .dw[114] chest param $00-$71 -> 1-cond OR-lists 15:A106-A179 (!item-get flag); tools/chest_presence_dump.py
    .db $10,$A1,$10,$A1,$6C,$A1,$66,$A1,$10,$A1,$64,$A1,$6A,$A1,$10,$A1; 14:9C82
    .db $10,$A1,$10,$A1,$74,$A1,$10,$A1,$62,$A1,$10,$A1,$10,$A1,$10,$A1; 14:9C92
    .db $10,$A1,$10,$A1,$10,$A1,$10,$A1,$54,$A1,$10,$A1,$10,$A1,$10,$A1; 14:9CA2
    .db $10,$A1,$10,$A1,$10,$A1,$50,$A1,$52,$A1,$12,$A1,$28,$A1,$22,$A1; 14:9CB2
    .db $32,$A1,$34,$A1,$3C,$A1,$46,$A1,$48,$A1,$56,$A1,$5E,$A1,$10,$A1; 14:9CC2
    .db $5A,$A1,$10,$A1,$4E,$A1,$76,$A1,$72,$A1,$10,$A1,$10,$A1,$06,$A1; 14:9CD2
    .db $10,$A1,$10,$A1,$10,$A1,$58,$A1,$10,$A1,$5C,$A1,$10,$A1,$10,$A1; 14:9CE2
    .db $10,$A1,$10,$A1,$10,$A1,$60,$A1,$78,$A1,$4C,$A1,$10,$A1,$68,$A1; 14:9CF2
    .db $10,$A1,$10,$A1,$10,$A1,$10,$A1,$10,$A1,$10,$A1,$10,$A1,$10,$A1; 14:9D02
    .db $10,$A1,$10,$A1,$10,$A1,$10,$A1,$10,$A1,$10,$A1,$10,$A1,$10,$A1; 14:9D12
    .db $14,$A1,$10,$A1,$16,$A1,$18,$A1,$0E,$A1,$1A,$A1,$1C,$A1,$1E,$A1; 14:9D22
    .db $0C,$A1,$10,$A1,$20,$A1,$70,$A1,$24,$A1,$26,$A1,$2A,$A1,$2C,$A1; 14:9D32
    .db $2E,$A1,$10,$A1,$36,$A1,$38,$A1,$3A,$A1,$3E,$A1,$40,$A1,$10,$A1; 14:9D42
    .db $10,$A1,$6E,$A1,$4A,$A1,$42,$A1,$44,$A1,$08,$A1,$0A,$A1,$30,$A1; 14:9D52
    .db $10,$A1,$10,$A1                 ; 14:9D62
ItemStoreValue:  ; 113 bytes: value stored in the inventory slot (ids 50-70 -> base item)
    .db $00,$01,$02,$03,$04,$05,$06,$07,$08,$09,$0A,$0B,$0C,$0D,$0E,$0F; 14:9D66
    .db $10,$11,$12,$13,$14,$15,$16,$17,$18,$19,$1A,$1B,$1C,$1D,$1E,$1F; 14:9D76
    .db $20,$21,$22,$23,$24,$25,$26,$27,$28,$29,$2A,$2B,$2C,$2D,$2E,$2F; 14:9D86
    .db $30,$31,$32,$33,$34,$35,$36,$37,$38,$39,$3A,$3B,$3C,$3D,$3E,$3F; 14:9D96
    .db $40,$41,$42,$43,$44,$45,$46,$47,$48,$49,$4A,$4B,$4C,$4D,$4E,$4F; 14:9DA6
    .db $1D,$12,$1D,$1D,$22,$1D,$1D,$1D,$22,$1D,$21,$28,$1F,$1F,$1E,$1E; 14:9DB6
    .db $1E,$21,$21,$26,$21,$22,$23,$22,$22,$22,$24,$22,$22,$26,$24,$22; 14:9DC6
    .db $1D,$71,$72,$73,$74,$75,$76,$77,$78,$79,$7A,$7B,$7C,$7D,$7E,$7F; 14:9DD6
    .db $10,$08,$00,$00,$40,$E6,$FF,$10,$08,$00,$00,$40,$E5,$FF,$20,$08; 14:9DE6
    .db $00,$00,$40,$E4,$FF,$10,$08,$00,$00,$40,$D2,$FF,$20,$08,$00,$00; 14:9DF6
    .db $40,$D4,$FF,$10,$08,$00,$00,$40,$AA,$FF,$10,$08,$00,$00,$40,$AB; 14:9E06
    .db $FF,$10,$08,$00,$00,$40,$AD,$FF,$10,$08,$00,$00,$40,$AE,$FF,$10; 14:9E16
    .db $08,$00,$00,$40,$AF,$FF,$10,$08,$00,$00,$40,$B0,$FF,$10,$08,$00; 14:9E26
    .db $00,$40,$B1,$FF,$10,$08,$00,$00,$40,$B2,$FF,$10,$08,$00,$00,$40; 14:9E36
    .db $B3,$FF,$10,$08,$00,$00,$40,$B4,$FF,$10,$08,$00,$00,$40,$B5,$FF; 14:9E46
    .db $10,$08,$00,$00,$40,$B6,$FF,$10,$08,$00,$00,$40,$B7,$FF,$10,$08; 14:9E56
    .db $00,$00,$40,$B8,$FF,$10,$08,$00,$00,$40,$B9,$FF,$10,$08,$00,$00; 14:9E66
    .db $40,$BA,$FF,$10,$08,$00,$00,$40,$BB,$FF,$10,$08,$00,$00,$40,$BC; 14:9E76
    .db $FF,$10,$08,$00,$00,$40,$BD,$FF,$10,$08,$00,$00,$40,$BE,$FF,$10; 14:9E86
    .db $08,$00,$00,$41,$0C,$FF,$10,$08,$00,$00,$40,$BF,$FF,$10,$08,$00; 14:9E96
    .db $00,$40,$C0,$FF,$10,$08,$00,$00,$40,$C1,$FF,$10,$08,$00,$00,$40; 14:9EA6
    .db $C2,$FF,$10,$08,$00,$00,$40,$C3,$FF,$10,$08,$00,$00,$40,$C4,$FF; 14:9EB6
    .db $10,$08,$00,$00,$40,$C5,$FF,$10,$08,$00,$00,$40,$C6,$FF,$10,$08; 14:9EC6
    .db $00,$00,$01,$06,$40,$C7,$FF,$10,$08,$00,$00,$40,$C8,$FF,$10,$08; 14:9ED6
    .db $00,$00,$40,$C9,$FF,$20,$08,$00,$00,$01,$08,$00,$24,$40,$CA,$FF; 14:9EE6
    .db $18,$08,$00,$00,$40,$CB,$FF,$04,$04,$00,$00,$C0,$00,$FF,$04,$04; 14:9EF6
    .db $00,$00,$01,$0B,$00,$CC,$40,$6C,$FF,$08,$04,$00,$00,$40,$CD,$FF; 14:9F06
    .db $04,$04,$00,$00,$40,$DF,$FF,$08,$04,$00,$00,$40,$62,$20,$08,$00; 14:9F16
    .db $00,$40,$CF,$FF,$20,$08,$00,$00,$40,$D1,$FF,$10,$08,$00,$00,$01; 14:9F26
    .db $05,$40,$D3,$FF,$00,$80,$B0,$00,$40,$0B,$FF,$01,$80,$B0,$00,$40; 14:9F36
    .db $49,$FF,$02,$80,$B0,$00,$40,$17,$FF,$03,$80,$08,$00,$40,$5F,$FF; 14:9F46
    .db $00,$80,$B0,$00,$C0,$00,$FF,$0C,$80,$00,$00,$40,$87,$FF,$0D,$80; 14:9F56
    .db $00,$00,$00,$41,$00,$44,$41,$01,$FF,$0E,$80,$00,$00,$01,$03,$00; 14:9F66
    .db $1F,$00,$20,$40,$7B,$FF,$0F,$80,$00,$00,$40,$67,$FF,$0C,$C0,$00; 14:9F76
    .db $00,$40,$4E,$FF,$0D,$C0,$00,$00,$00,$77,$41,$02,$FF,$0E,$C0,$00; 14:9F86
    .db $00,$40,$7A,$FF,$0F,$C0,$00,$00,$40,$78,$FF,$08,$04,$00,$00,$40; 14:9F96
    .db $1C,$FF,$20,$08,$00,$00,$40,$CE,$FF,$20,$08,$00,$00,$C0,$00,$FF; 14:9FA6
    .db $20,$08,$00,$00,$40,$D0,$FF,$18,$08,$00,$00,$40,$2E,$FF,$18,$08; 14:9FB6
    .db $00,$00,$41,$00,$FF,$20,$08,$00,$00,$40,$3C,$FF,$20,$08,$00,$00; 14:9FC6
    .db $40,$79,$FF,$18,$08,$00,$00,$C0,$00,$FF,$18,$08,$00,$00,$40,$E7; 14:9FD6
    .db $FF,$20,$08,$00,$00,$40,$7D,$FF,$20,$08,$00,$00,$40,$13,$FF,$20; 14:9FE6
    .db $08,$00,$00,$40,$8B,$FF,$10,$08,$00,$00; 14:9FF6
