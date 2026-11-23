; re-c bank 20  file 0x28010  CPU $8000-$9FFF  (tools/disasm_all.py)

ppuctrl_shadow = $00
frame_ready = $09
tmp_0A = $0A
tmp_0B = $0B
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
tmp_2A = $2A
tmp_2B = $2B
tmp_2C = $2C
tmp_2D = $2D
tmp_2E = $2E
tmp_2F = $2F
sub_state = $41
pad_new = $4B
mmc3_banksel_shadow = $50
r7_shadow = $6F
snd_queue_count = $0100
snd_queue_lock = $0101
snd_queue = $0103
oam_y = $0200
oam_tile = $0201
oam_attr = $0202
oam_x = $0203
ram_07DC = $07DC
text_msg_id = $07DE
text_group = $07DF
chr_r5_play = $07F5
PPUCTRL = $2000
wram_buf = $6000
wram_60F8 = $60F8
wram_60FC = $60FC
wram_60FF = $60FF
pal_buf = $6140
wram_6150 = $6150
wram_6154 = $6154
ppu_queue = $6200
ppu_queue_1 = $6201
ppu_queue_2 = $6202
ppu_queue_3 = $6203
player_name = $6400
ReadPads = $FE80

    .org $8000
    .db $00,$A0,$1C,$A0,$36,$A0,$46,$A0,$77,$A0,$36,$A0,$AB,$A0,$CB,$A0; 20:8000
    .db $36,$A0,$E6,$A0,$01,$A1,$2A,$A1,$6A,$A1,$36,$A0,$A3,$A1,$43,$A2; 20:8010
    .db $67,$A2,$36,$A0,$67,$A2,$84,$A2,$93,$A2,$9F,$A2,$C4,$A2,$EA,$A2; 20:8020
    .db $0C,$A3,$36,$A0,$4A,$A3,$9F,$A3,$BC,$A3,$E4,$A3,$F7,$A3,$FD,$A3; 20:8030
    .db $12,$A4,$3F,$A4,$8B,$A4,$9D,$A4,$A5,$A4,$D2,$A4,$38,$A5,$9B,$A5; 20:8040
    .db $DC,$A5,$0F,$A6,$57,$A6,$80,$A6,$C3,$A6,$E2,$A6,$27,$A7,$51,$A7; 20:8050
    .db $51,$A7,$90,$A7,$02,$A8,$B2,$A8,$E3,$A8,$04,$A9,$3C,$A9,$64,$A9; 20:8060
    .db $72,$A9,$A3,$A9,$CA,$A9,$CA,$A9,$E0,$A9,$E0,$A9,$13,$AA,$13,$AA; 20:8070
    .db $24,$AA,$24,$AA,$4C,$AA,$93,$AA,$D9,$AA,$D9,$AA,$F8,$AA,$22,$AB; 20:8080
    .db $3B,$AB,$3F,$AB,$5C,$AB,$7E,$AB,$7E,$AB,$A5,$AB,$7E,$AC,$85,$AC; 20:8090
    .db $10,$AD,$52,$AD,$72,$AD,$7E,$AD,$B1,$AD,$BE,$AD,$03,$AE,$41,$AE; 20:80A0
    .db $4E,$AE,$86,$AE,$B7,$AE,$FB,$AE,$2B,$AF,$98,$AF,$C4,$AF,$2E,$B0; 20:80B0
    .db $43,$B0,$5F,$B0,$57,$B1,$36,$B2,$55,$B2,$9E,$B2,$E3,$B2,$22,$B3; 20:80C0
    .db $53,$B3,$81,$B3,$B0,$B3,$A0,$B3,$A0,$B3,$B0,$B3,$B0,$B3,$C7,$B3; 20:80D0
    .db $5E,$B4,$71,$B4,$85,$B4,$99,$B4,$BB,$B4,$D9,$B4,$E6,$B4,$20,$B5; 20:80E0
    .db $43,$B5,$5F,$B5,$74,$B5,$C0,$B5,$E5,$B5,$1C,$B6,$62,$B6,$62,$B6; 20:80F0
    .db $7C,$B6,$B2,$B6,$EB,$B6,$22,$B7,$93,$B7,$C3,$B7,$F3,$B7,$2A,$B8; 20:8100
    .db $44,$B8,$92,$B8,$AC,$B8,$BB,$B8,$ED,$B8,$03,$B9,$31,$B9,$77,$B9; 20:8110
    .db $A4,$B9,$DB,$B9,$FC,$B9,$8F,$BA,$8F,$BA,$C6,$BA,$15,$BB,$62,$BB; 20:8120
    .db $CA,$BB,$F9,$BB,$02,$BC,$52,$BC,$9D,$BC,$AA,$BC,$B1,$BC,$61,$BD; 20:8130
    .db $CA,$BD,$E0,$BD,$14,$BE,$3B,$BE,$84,$BE,$84,$BE,$A5,$BE,$A5,$BE; 20:8140
    .db $AD,$BE,$E3,$BE,$1C,$BF,$24,$BF,$2C,$BF,$00,$A0,$31,$A0,$38,$A0; 20:8150
    .db $3F,$A0,$FB,$A0,$FB,$A0,$5B,$A1,$01,$A2,$1F,$A2,$35,$A2,$4C,$A2; 20:8160
    .db $65,$A2,$8F,$A2,$1F,$A2,$A9,$A2,$D8,$A2,$E9,$A2,$15,$A3,$1F,$A3; 20:8170
    .db $88,$A3,$C1,$A3,$F7,$A3,$28,$A4,$40,$A4,$63,$A4,$89,$A4,$9A,$A4; 20:8180
    .db $B0,$A4,$E1,$A4,$E1,$A4,$F4,$A4,$43,$A5,$B2,$A5,$DD,$A5,$08,$A6; 20:8190
    .db $60,$A6,$84,$A6,$DF,$A6,$20,$A7,$3B,$A7,$66,$A7,$81,$A7,$B3,$A7; 20:81A0
    .db $CE,$A7,$07,$A8,$16,$A8,$38,$A8,$5E,$A8,$71,$A8,$8B,$A8,$07,$A9; 20:81B0
    .db $5B,$A9,$78,$A9,$9F,$A9,$BB,$A9,$CE,$A9,$E5,$A9,$0C,$AA,$30,$AA; 20:81C0
    .db $58,$AA,$84,$AA,$91,$AA,$BD,$AA,$D5,$AA,$F1,$AA,$12,$AB,$12,$AB; 20:81D0
    .db $12,$AB,$5A,$AB,$81,$AB,$D7,$AB,$16,$AC,$34,$AC,$76,$AC,$90,$AC; 20:81E0
    .db $15,$AD,$2E,$AD,$39,$AD,$62,$AD,$6E,$AD,$BB,$AD,$E7,$AD,$23,$AE; 20:81F0
    .db $38,$AE,$A2,$AE,$D4,$AE,$F2,$AE,$2B,$AF,$87,$AF,$AE,$AF,$BA,$AF; 20:8200
    .db $D2,$AF,$F4,$AF,$16,$B0,$28,$B0,$4C,$B0,$F4,$AF,$5F,$B0,$70,$B0; 20:8210
    .db $78,$B0,$C1,$B0,$E2,$B0,$3D,$B1,$72,$B1,$7C,$B1,$0A,$B2,$24,$B2; 20:8220
    .db $4B,$B2,$83,$B2,$97,$B2,$A1,$B2,$A1,$B2,$DC,$B2,$EA,$B2,$0A,$B3; 20:8230
    .db $0F,$B3,$1C,$B3,$35,$B3,$BE,$B6,$F5,$B3,$0A,$B4,$1C,$B4,$36,$B4; 20:8240
    .db $5E,$B4,$88,$B4,$A8,$B4,$D4,$B4,$F5,$B4,$1C,$B5,$24,$B5,$31,$B5; 20:8250
    .db $41,$B5,$66,$B5,$F6,$B5,$0B,$B6,$BE,$B6,$C8,$B6,$20,$B7,$37,$B7; 20:8260
    .db $94,$B7,$AF,$B7,$C4,$B7,$E2,$B7,$11,$B8,$3E,$B8,$76,$B8,$AC,$B8; 20:8270
    .db $D2,$B8,$F5,$B8,$2A,$B9,$6D,$B9,$8E,$B9,$B2,$B9,$DE,$B9,$0B,$BA; 20:8280
    .db $CA,$BA,$F7,$BA,$0B,$BB,$32,$BB,$74,$BB,$A6,$BB,$10,$BC,$4F,$BC; 20:8290
    .db $B1,$BC,$1C,$BD,$D2,$BD,$0F,$BE,$54,$BE,$69,$BE,$83,$BE,$1F,$BF; 20:82A0
    .db $66,$BF,$00,$A0,$3B,$A0,$82,$A0,$AF,$A0,$E1,$A0,$06,$A1,$C8,$A1; 20:82B0
    .db $31,$A2,$EE,$A1,$53,$A2,$78,$A2,$88,$A2,$97,$A2,$B3,$A2,$DD,$A2; 20:82C0
    .db $EA,$A2,$2C,$A3,$BF,$A3,$D9,$A3,$D9,$A3,$E5,$A3,$0C,$A4,$CC,$A4; 20:82D0
    .db $EA,$A4,$18,$A5,$56,$A5,$54,$A6,$08,$A7,$41,$A7,$3F,$A8,$06,$A9; 20:82E0
    .db $C9,$A9,$23,$AA,$72,$AA,$9F,$AA,$AA,$AA,$E5,$AA,$77,$AB,$D9,$AB; 20:82F0
    .db $F5,$AB,$03,$AC,$20,$AC,$47,$AC,$77,$AC,$AD,$AC,$DD,$AC,$0C,$AD; 20:8300
    .db $4B,$AD,$70,$AD,$92,$AD,$C3,$AD,$FE,$AD,$1E,$AE,$36,$AE,$50,$AE; 20:8310
    .db $7E,$AE,$E6,$AE,$4F,$AF,$56,$AF,$80,$AF,$A7,$AF,$C8,$AF,$EA,$AF; 20:8320
    .db $06,$B0,$3C,$B0,$65,$B0,$B3,$B0,$DD,$B0,$FF,$B0,$35,$B1,$52,$B1; 20:8330
    .db $8C,$B1,$BB,$B1,$EC,$B1,$20,$B2,$3D,$B2,$57,$B2,$74,$B2,$B4,$B2; 20:8340
    .db $E5,$B2,$F2,$B2,$0E,$B3,$48,$B3,$7C,$B3,$94,$B3,$B8,$B3,$CE,$B3; 20:8350
    .db $E0,$B3,$FA,$B3,$FA,$B3,$AE,$B4,$FB,$B4,$00,$B5,$18,$B5,$64,$B5; 20:8360
    .db $74,$B5,$92,$B5,$C3,$B5,$EA,$B5,$F8,$B5,$2B,$B6,$61,$B6,$89,$B6; 20:8370
    .db $9C,$B6,$B6,$B6,$D2,$B6,$D2,$B6,$D2,$B6,$DB,$B6,$DB,$B6,$DB,$B6; 20:8380
    .db $DB,$B6,$DB,$B6,$DB,$B6,$DB,$B6,$DB,$B6,$E1,$B6,$FB,$B6,$FB,$B6; 20:8390
    .db $04,$B7,$34,$B7,$45,$B7,$58,$B7,$58,$B7,$5F,$B7,$71,$B7,$7E,$B7; 20:83A0
    .db $B9,$B7,$F0,$B7,$F0,$B7,$F0,$B7,$F0,$B7,$F7,$B7,$FE,$B7,$14,$B8; 20:83B0
    .db $30,$B8,$51,$B8,$69,$B8,$84,$B8,$A7,$B8,$D8,$B8,$EF,$B8,$14,$B9; 20:83C0
    .db $31,$B9,$4F,$B9,$92,$B9,$9F,$B9,$C5,$B9,$DC,$B9,$EE,$B9,$05,$BA; 20:83D0
    .db $15,$BA,$2B,$BA,$3D,$BA,$62,$BA,$82,$BA,$A2,$BA,$C2,$BA,$E5,$BA; 20:83E0
    .db $F7,$BA,$0F,$BB,$2C,$BB,$46,$BB,$61,$BB,$9D,$BB,$B2,$BB; 20:83F0
TextGroupBank:  ; 36 bytes: PRG bank (21/22/23) per text group $07DF
    .db $15,$15,$15,$15,$15,$15,$15,$15,$15,$15,$15,$15,$15,$15,$16,$16; 20:83FE
    .db $16,$16,$16,$16,$16,$16,$16,$16,$16,$16,$17,$17,$17,$17,$17,$17; 20:840E
    .db $17,$17,$17,$17                 ; 20:841E
TextGroupPtrs:  ; 34 ptr16: per-group message ptr arrays (bank 20 $8000-$83FD)
    .db $00,$80,$3C,$80,$48,$80,$62,$80,$70,$80,$A6,$80,$C8,$80,$CC,$80; 20:8422
    .db $E2,$80,$0E,$81,$1C,$81,$3C,$81,$44,$81,$50,$81,$5A,$81,$66,$81; 20:8432
    .db $98,$81,$C0,$81,$C4,$81,$EA,$81,$16,$82,$48,$82,$62,$82,$72,$82; 20:8442
    .db $94,$82,$A6,$82,$B2,$82,$DA,$82,$00,$83,$40,$83,$7C,$83,$BA,$83; 20:8452
    .db $BC,$83,$F8,$83                 ; 20:8462
StaleText20_8466:  ; dead filler to 84FF: copy of 22:A466 text (filler_scan.DEAD)
    .db $20,$F7,$73,$75,$72,$70,$72,$69,$73,$86,$BD,$21,$20,$49,$02,$74; 20:8466
    .db $68,$6F,$75,$67,$68,$74,$20,$F3,$EE,$61,$02,$6D,$6F,$6E,$73,$74; 20:8476
    .db $FF,$2E,$00,$01,$C6,$FB,$F8,$6C,$69,$76,$65,$20,$73,$61,$66,$65; 20:8486
    .db $6C,$79,$2E,$00,$01,$53,$6F,$6D,$65,$74,$69,$6D,$65,$73,$20,$FB; 20:8496
    .db $A7,$D1,$81,$DA,$02,$CB,$05,$06,$2E,$00,$01,$06,$04,$3A,$22,$45; 20:84A6
    .db $76,$65,$72,$79,$62,$6F,$64,$79,$20,$A3,$62,$65,$65,$6E,$02,$6C; 20:84B6
    .db $69,$76,$AC,$70,$65,$61,$63,$65,$66,$75,$6C,$6C,$79,$20,$73,$69; 20:84C6
    .db $6E,$63,$65,$02,$06,$10,$AD,$9C,$2E,$22,$00,$01,$57,$68,$6F,$20; 20:84D6
    .db $83,$F3,$3F,$20,$47,$65,$74,$20,$61,$77,$61,$79,$21,$00,$01,$06; 20:84E6
    .db $0A,$3A,$22,$57,$68,$6F,$20,$83,$F3,$3F; 20:84F6
TextWindowOpen:  ; params 20:8879 -> draw frame NT2 rows 0-9 + attrs; clear queue $6200
    LDX #$00                            ; 8500  a2 00
loc_20_8502:
    LDA TextWinParamsOpen,X             ; 8502  bd 79 88
    STA tmp_20,X                        ; 8505  95 20
    INX                                 ; 8507  e8
    CPX #$10                            ; 8508  e0 10
    BNE loc_20_8502                     ; 850A  d0 f6
    JSR TextDrawFrame                   ; 850C  20 b7 87
loc_20_850F:
    LDA tmp_0A                          ; 850F  a5 0a
    EOR tmp_0B                          ; 8511  45 0b
    BNE loc_20_850F                     ; 8513  d0 fa
    LDX #$1F                            ; 8515  a2 1f
    LDA #$FF                            ; 8517  a9 ff
loc_20_8519:
    STA ppu_queue,X                     ; 8519  9d 00 62
    DEX                                 ; 851C  ca
    BPL loc_20_8519                     ; 851D  10 fa
    RTS                                 ; 851F  60
    .db $6C,$69,$6B,$65,$20,$F3,$73,$68 ; 20:8520
TextPrintBox:  ; load window params 20:8889 -> $20-$2F then TextPrint
    LDX #$00                            ; 8528  a2 00
loc_20_852A:
    LDA $8889,X                         ; 852A  bd 89 88
    STA tmp_20,X                        ; 852D  95 20
    INX                                 ; 852F  e8
    CPX #$10                            ; 8530  e0 10
    BNE loc_20_852A                     ; 8532  d0 f6
    JSR TextPrint                       ; 8534  20 3a 85
    JMP loc_20_850F                     ; 8537  4c 0f 85
TextPrint:  ; print message $07DE&$1F of group $07DF; ctrl <$0A / word >=$80 / else ASCII
    LDX text_group                      ; 853A  ae df 07
    STX $6700                           ; 853D  8e 00 67
    LDA TextGroupBank,X                 ; 8540  bd fe 83
    JSR TextSetR7                       ; 8543  20 5a 88
    LDA text_group                      ; 8546  ad df 07
    ASL A                               ; 8549  0a
    TAX                                 ; 854A  aa
    LDA TextGroupPtrs,X                 ; 854B  bd 22 84
    STA tmp_2C                          ; 854E  85 2c
    LDA $8423,X                         ; 8550  bd 23 84
    STA tmp_2D                          ; 8553  85 2d
    LDA text_msg_id                     ; 8555  ad de 07
    STA $6701                           ; 8558  8d 01 67
    AND #$1F                            ; 855B  29 1f
    ASL A                               ; 855D  0a
    TAY                                 ; 855E  a8
    LDA (tmp_2C),Y                      ; 855F  b1 2c
    STA tmp_2A                          ; 8561  85 2a
    INY                                 ; 8563  c8
    LDA (tmp_2C),Y                      ; 8564  b1 2c
    STA tmp_2B                          ; 8566  85 2b
    LDY #$00                            ; 8568  a0 00
loc_20_856A:
    LDA (tmp_2A),Y                      ; 856A  b1 2a
    CMP #$0A                            ; 856C  c9 0a
    BCS loc_20_8576                     ; 856E  b0 06
    JSR TextCtrlDispatch                ; 8570  20 93 85
    JMP loc_20_856A                     ; 8573  4c 6a 85
loc_20_8576:
    LDA (tmp_2A),Y                      ; 8576  b1 2a
    BPL loc_20_8580                     ; 8578  10 06
    JSR TextWord                        ; 857A  20 f5 86
    JMP loc_20_856A                     ; 857D  4c 6a 85
loc_20_8580:
    JSR TextChar                        ; 8580  20 ae 87
    JMP loc_20_856A                     ; 8583  4c 6a 85
    .db $98,$18,$65,$2A,$85,$2A,$90,$04,$E6,$2B,$A0,$00,$60; 20:8586
TextCtrlDispatch:  ; A=ctrl code 0-9 via table 20:85A2
    ASL A                               ; 8593  0a
    TAX                                 ; 8594  aa
    LDA TextCtrlTable,X                 ; 8595  bd a2 85
    STA tmp_2C                          ; 8598  85 2c
    LDA $85A3,X                         ; 859A  bd a3 85
    STA tmp_2D                          ; 859D  85 2d
    JMP (tmp_2C)                        ; 859F  6c 2c 00
TextCtrlTable:  ; 10 ptr16 handlers for codes 00-09
    .dw sub_20_85B6                     ; 20:85A2
    .dw sub_20_85F2                     ; 20:85A4
    .dw sub_20_8607                     ; 20:85A6
    .dw TextPageWait                    ; 20:85A8
    .dw TextPlayerName                  ; 20:85AA
    .dw sub_20_8682                     ; 20:85AC
    .dw sub_20_86CD                     ; 20:85AE
    .dw sub_20_86E1                     ; 20:85B0
    .dw TextCurItem                     ; 20:85B2
    .dw TextSpaces                      ; 20:85B4
sub_20_85B6:
    PLA                                 ; 85B6  68
    PLA                                 ; 85B7  68
TextFlush:  ; upload $6000 line buffer 4x$40 to PPU $2820/$2860/$28A0/$28E0
    LDA #$28                            ; 85B8  a9 28
    STA tmp_21                          ; 85BA  85 21
    LDA #$20                            ; 85BC  a9 20
    STA tmp_20                          ; 85BE  85 20
    LDA #$00                            ; 85C0  a9 00
    STA tmp_25                          ; 85C2  85 25
    LDA #$40                            ; 85C4  a9 40
    STA tmp_24                          ; 85C6  85 24
    JSR sub_20_85E9                     ; 85C8  20 e9 85
    LDA #$60                            ; 85CB  a9 60
    STA tmp_20                          ; 85CD  85 20
    LDA #$40                            ; 85CF  a9 40
    STA tmp_25                          ; 85D1  85 25
    JSR sub_20_85E9                     ; 85D3  20 e9 85
    LDA #$A0                            ; 85D6  a9 a0
    STA tmp_20                          ; 85D8  85 20
    LDA #$80                            ; 85DA  a9 80
    STA tmp_25                          ; 85DC  85 25
    JSR sub_20_85E9                     ; 85DE  20 e9 85
    LDA #$E0                            ; 85E1  a9 e0
    STA tmp_20                          ; 85E3  85 20
    LDA #$C0                            ; 85E5  a9 c0
    STA tmp_25                          ; 85E7  85 25
sub_20_85E9:
    LDA tmp_0A                          ; 85E9  a5 0a
    EOR tmp_0B                          ; 85EB  45 0b
    BNE sub_20_85E9                     ; 85ED  d0 fa
    JMP TextQueueUpload                 ; 85EF  4c 36 88
sub_20_85F2:
    JSR TextClearBuf                    ; 85F2  20 20 87
    STA tmp_29                          ; 85F5  85 29
loc_20_85F7:
    LDA tmp_0A                          ; 85F7  a5 0a
    EOR tmp_0B                          ; 85F9  45 0b
    BNE loc_20_85F7                     ; 85FB  d0 fa
    LDA #$23                            ; 85FD  a9 23
    STA tmp_28                          ; 85FF  85 28
    INY                                 ; 8601  c8
    BNE loc_20_8606                     ; 8602  d0 02
    INC tmp_2B                          ; 8604  e6 2b
loc_20_8606:
    RTS                                 ; 8606  60
sub_20_8607:
    CLC                                 ; 8607  18
    LDA tmp_28                          ; 8608  a5 28
    ADC #$40                            ; 860A  69 40
    AND #$E0                            ; 860C  29 e0
    CLC                                 ; 860E  18
    ADC #$04                            ; 860F  69 04
    STA tmp_28                          ; 8611  85 28
    INY                                 ; 8613  c8
    BNE loc_20_8618                     ; 8614  d0 02
    INC tmp_2B                          ; 8616  e6 2b
loc_20_8618:
    RTS                                 ; 8618  60
TextPageWait:  ; code 03: prompt tile at $2910 then wait A/B then clear
    STY tmp_29                          ; 8619  84 29
    INY                                 ; 861B  c8
    JSR TextFlush                       ; 861C  20 b8 85
loc_20_861F:
    LDA tmp_0A                          ; 861F  a5 0a
    EOR tmp_0B                          ; 8621  45 0b
    BNE loc_20_861F                     ; 8623  d0 fa
    LDA #$29                            ; 8625  a9 29
    STA tmp_21                          ; 8627  85 21
    LDA #$10                            ; 8629  a9 10
    STA tmp_20                          ; 862B  85 20
    LDA #$01                            ; 862D  a9 01
    STA tmp_24                          ; 862F  85 24
    LDA #$00                            ; 8631  a9 00
    STA tmp_25                          ; 8633  85 25
    LDA #$40                            ; 8635  a9 40
    STA wram_buf                        ; 8637  8d 00 60
    JSR TextQueueUpload                 ; 863A  20 36 88
    LDA #$40                            ; 863D  a9 40
    STA tmp_24                          ; 863F  85 24
loc_20_8641:
    LSR frame_ready                     ; 8641  46 09
    BCC loc_20_8641                     ; 8643  90 fc
    JSR ReadPads                        ; 8645  20 80 fe
    LDX text_group                      ; 8648  ae df 07
    LDA TextGroupBank,X                 ; 864B  bd fe 83
    JSR TextSetR7                       ; 864E  20 5a 88
    LDA pad_new                         ; 8651  a5 4b
    AND #$80                            ; 8653  29 80
    BNE loc_20_8660                     ; 8655  d0 09
    LDA pad_new                         ; 8657  a5 4b
    AND #$40                            ; 8659  29 40
    BNE loc_20_8660                     ; 865B  d0 03
    JMP loc_20_8641                     ; 865D  4c 41 86
loc_20_8660:
    LDY tmp_29                          ; 8660  a4 29
    INY                                 ; 8662  c8
    BNE loc_20_8667                     ; 8663  d0 02
    INC tmp_2B                          ; 8665  e6 2b
loc_20_8667:
    JSR TextClearBuf                    ; 8667  20 20 87
    RTS                                 ; 866A  60
TextPlayerName:  ; code 04: print zero-terminated name at $6400
    STY tmp_29                          ; 866B  84 29
    LDY #$00                            ; 866D  a0 00
loc_20_866F:
    LDA player_name,Y                   ; 866F  b9 00 64
    BEQ loc_20_867A                     ; 8672  f0 06
    JSR TextChar                        ; 8674  20 ae 87
    JMP loc_20_866F                     ; 8677  4c 6f 86
loc_20_867A:
    LDY tmp_29                          ; 867A  a4 29
    INY                                 ; 867C  c8
    BNE loc_20_8681                     ; 867D  d0 02
    INC tmp_2B                          ; 867F  e6 2b
loc_20_8681:
    RTS                                 ; 8681  60
sub_20_8682:
    JSR sub_20_8710                     ; 8682  20 10 87
    LDA (tmp_2A),Y                      ; 8685  b1 2a
    ASL A                               ; 8687  0a
    TAX                                 ; 8688  aa
    LDA TextStr05,X                     ; 8689  bd 00 8a
    STA tmp_2A                          ; 868C  85 2a
    LDA $8A01,X                         ; 868E  bd 01 8a
    STA tmp_2B                          ; 8691  85 2b
TextPrintStr:  ; print ($2A) zero-terminated then auto-space rule and resume message
    LDY #$00                            ; 8693  a0 00
loc_20_8695:
    LDA (tmp_2A),Y                      ; 8695  b1 2a
    BEQ loc_20_869F                     ; 8697  f0 06
    JSR TextChar                        ; 8699  20 ae 87
    JMP loc_20_8695                     ; 869C  4c 95 86
loc_20_869F:
    LDY tmp_29                          ; 869F  a4 29
    INY                                 ; 86A1  c8
    LDA (tmp_2C),Y                      ; 86A2  b1 2c
    CMP #$04                            ; 86A4  c9 04
    BCC loc_20_86BD                     ; 86A6  90 15
    CMP #$0A                            ; 86A8  c9 0a
    BCC loc_20_86B4                     ; 86AA  90 08
    CMP #$5F                            ; 86AC  c9 5f
    BEQ loc_20_86BD                     ; 86AE  f0 0d
    CMP #$40                            ; 86B0  c9 40
    BCC loc_20_86BD                     ; 86B2  90 09
loc_20_86B4:
    LDA #$20                            ; 86B4  a9 20
    LDX tmp_28                          ; 86B6  a6 28
    STA wram_buf,X                      ; 86B8  9d 00 60
    INC tmp_28                          ; 86BB  e6 28
loc_20_86BD:
    LDY tmp_29                          ; 86BD  a4 29
    INY                                 ; 86BF  c8
    BNE loc_20_86C4                     ; 86C0  d0 02
    INC tmp_2D                          ; 86C2  e6 2d
loc_20_86C4:
    LDA tmp_2C                          ; 86C4  a5 2c
    STA tmp_2A                          ; 86C6  85 2a
    LDA tmp_2D                          ; 86C8  a5 2d
    STA tmp_2B                          ; 86CA  85 2b
    RTS                                 ; 86CC  60
sub_20_86CD:
    JSR sub_20_8710                     ; 86CD  20 10 87
    LDA (tmp_2A),Y                      ; 86D0  b1 2a
    ASL A                               ; 86D2  0a
    TAX                                 ; 86D3  aa
    LDA TextStr06,X                     ; 86D4  bd 14 8a
    STA tmp_2A                          ; 86D7  85 2a
    LDA $8A15,X                         ; 86D9  bd 15 8a
    STA tmp_2B                          ; 86DC  85 2b
    JMP TextPrintStr                    ; 86DE  4c 93 86
sub_20_86E1:
    JSR sub_20_8710                     ; 86E1  20 10 87
    LDA (tmp_2A),Y                      ; 86E4  b1 2a
    ASL A                               ; 86E6  0a
    TAX                                 ; 86E7  aa
    LDA TextStr07,X                     ; 86E8  bd 5c 8a
    STA tmp_2A                          ; 86EB  85 2a
    LDA $8A5D,X                         ; 86ED  bd 5d 8a
    STA tmp_2B                          ; 86F0  85 2b
    JMP TextPrintStr                    ; 86F2  4c 93 86
TextWord:  ; dict word code >=$80 -> TextDictPtrs
    LDA tmp_2A                          ; 86F5  a5 2a
    STA tmp_2C                          ; 86F7  85 2c
    LDA tmp_2B                          ; 86F9  a5 2b
    STA tmp_2D                          ; 86FB  85 2d
    STY tmp_29                          ; 86FD  84 29
    LDA (tmp_2A),Y                      ; 86FF  b1 2a
    ASL A                               ; 8701  0a
    TAX                                 ; 8702  aa
    LDA TextDictPtrs,X                  ; 8703  bd 00 89
    STA tmp_2A                          ; 8706  85 2a
    LDA $8901,X                         ; 8708  bd 01 89
    STA tmp_2B                          ; 870B  85 2b
    JMP TextPrintStr                    ; 870D  4c 93 86
sub_20_8710:
    LDA tmp_2A                          ; 8710  a5 2a
    STA tmp_2C                          ; 8712  85 2c
    LDA tmp_2B                          ; 8714  a5 2b
    STA tmp_2D                          ; 8716  85 2d
    INY                                 ; 8718  c8
    BNE loc_20_871D                     ; 8719  d0 02
    INC tmp_2D                          ; 871B  e6 2d
loc_20_871D:
    STY tmp_29                          ; 871D  84 29
    RTS                                 ; 871F  60
TextClearBuf:  ; fill $6000-$60FF rows FE 1E 29x20 1F; cursor $28=$20
    STY tmp_29                          ; 8720  84 29
loc_20_8722:
    LDA tmp_0A                          ; 8722  a5 0a
    EOR tmp_0B                          ; 8724  45 0b
    BNE loc_20_8722                     ; 8726  d0 fa
    LDX #$00                            ; 8728  a2 00
loc_20_872A:
    LDA #$FE                            ; 872A  a9 fe
    STA wram_buf,X                      ; 872C  9d 00 60
    INX                                 ; 872F  e8
    LDA #$1E                            ; 8730  a9 1e
    STA wram_buf,X                      ; 8732  9d 00 60
    INX                                 ; 8735  e8
    LDA #$1D                            ; 8736  a9 1d
    STA tmp_27                          ; 8738  85 27
    LDA #$20                            ; 873A  a9 20
loc_20_873C:
    STA wram_buf,X                      ; 873C  9d 00 60
    INX                                 ; 873F  e8
    DEC tmp_27                          ; 8740  c6 27
    BNE loc_20_873C                     ; 8742  d0 f8
    LDA #$1F                            ; 8744  a9 1f
    STA wram_buf,X                      ; 8746  9d 00 60
    INX                                 ; 8749  e8
    BNE loc_20_872A                     ; 874A  d0 de
    LDA #$20                            ; 874C  a9 20
    STA tmp_28                          ; 874E  85 28
    LDY tmp_29                          ; 8750  a4 29
    RTS                                 ; 8752  60
TextSpaces:  ; code 09 n: n spaces
    STY tmp_29                          ; 8753  84 29
    INY                                 ; 8755  c8
    LDA (tmp_2A),Y                      ; 8756  b1 2a
    TAY                                 ; 8758  a8
    LDA #$20                            ; 8759  a9 20
loc_20_875B:
    LDX tmp_28                          ; 875B  a6 28
    STA wram_buf,X                      ; 875D  9d 00 60
    INC tmp_28                          ; 8760  e6 28
    DEY                                 ; 8762  88
    BNE loc_20_875B                     ; 8763  d0 f6
    LDY tmp_29                          ; 8765  a4 29
    INY                                 ; 8767  c8
    BNE loc_20_876C                     ; 8768  d0 02
    INC tmp_2B                          ; 876A  e6 2b
loc_20_876C:
    INY                                 ; 876C  c8
    BNE loc_20_8771                     ; 876D  d0 02
    INC tmp_2B                          ; 876F  e6 2b
loc_20_8771:
    RTS                                 ; 8771  60
TextCurItem:  ; code 08: item name 20:8A5C indexed by $07DC ($FF -> $49 blank)
    LDA tmp_2A                          ; 8772  a5 2a
    STA tmp_2C                          ; 8774  85 2c
    LDA tmp_2B                          ; 8776  a5 2b
    STA tmp_2D                          ; 8778  85 2d
    STY tmp_29                          ; 877A  84 29
    INY                                 ; 877C  c8
    LDA ram_07DC                        ; 877D  ad dc 07
    CMP #$FF                            ; 8780  c9 ff
    BNE loc_20_8786                     ; 8782  d0 02
    LDA #$49                            ; 8784  a9 49
loc_20_8786:
    ASL A                               ; 8786  0a
    TAX                                 ; 8787  aa
    LDA TextStr07,X                     ; 8788  bd 5c 8a
    STA tmp_2A                          ; 878B  85 2a
    LDA $8A5D,X                         ; 878D  bd 5d 8a
    STA tmp_2B                          ; 8790  85 2b
    LDY #$00                            ; 8792  a0 00
loc_20_8794:
    LDA (tmp_2A),Y                      ; 8794  b1 2a
    BEQ loc_20_879E                     ; 8796  f0 06
    JSR TextChar                        ; 8798  20 ae 87
    JMP loc_20_8794                     ; 879B  4c 94 87
loc_20_879E:
    LDY tmp_29                          ; 879E  a4 29
    INY                                 ; 87A0  c8
    BNE loc_20_87A5                     ; 87A1  d0 02
    INC tmp_2D                          ; 87A3  e6 2d
loc_20_87A5:
    LDA tmp_2C                          ; 87A5  a5 2c
    STA tmp_2A                          ; 87A7  85 2a
    LDA tmp_2D                          ; 87A9  a5 2d
    STA tmp_2B                          ; 87AB  85 2b
    RTS                                 ; 87AD  60
TextChar:  ; $6000+$28 = A; $28++; Y++
    LDX tmp_28                          ; 87AE  a6 28
    STA wram_buf,X                      ; 87B0  9d 00 60
    INC tmp_28                          ; 87B3  e6 28
    INY                                 ; 87B5  c8
    RTS                                 ; 87B6  60
TextDrawFrame:  ; 5x TextFrameRows from templates 20:8899; attr $2BC0 = $FF
    LDX #$00                            ; 87B7  a2 00
    JSR TextFrameRows                   ; 87B9  20 ef 87
    LDX #$03                            ; 87BC  a2 03
    JSR TextFrameRows                   ; 87BE  20 ef 87
    LDX #$03                            ; 87C1  a2 03
    JSR TextFrameRows                   ; 87C3  20 ef 87
    LDX #$03                            ; 87C6  a2 03
    JSR TextFrameRows                   ; 87C8  20 ef 87
    LDX #$06                            ; 87CB  a2 06
    JSR TextFrameRows                   ; 87CD  20 ef 87
loc_20_87D0:
    LDA tmp_0A                          ; 87D0  a5 0a
    EOR tmp_0B                          ; 87D2  45 0b
    BNE loc_20_87D0                     ; 87D4  d0 fa
    LDX #$41                            ; 87D6  a2 41
    LDA #$FF                            ; 87D8  a9 ff
loc_20_87DA:
    STA wram_buf,X                      ; 87DA  9d 00 60
    DEX                                 ; 87DD  ca
    BNE loc_20_87DA                     ; 87DE  d0 fa
    LDA #$2B                            ; 87E0  a9 2b
    STA tmp_21                          ; 87E2  85 21
    LDA #$C0                            ; 87E4  a9 c0
    STA tmp_20                          ; 87E6  85 20
    LDA #$01                            ; 87E8  a9 01
    STA tmp_25                          ; 87EA  85 25
    JMP TextQueueUpload                 ; 87EC  4c 36 88
TextFrameRows:  ; 2 rows of 32 tiles ($FE left fill*29 right) -> queue
    LDA tmp_0A                          ; 87EF  a5 0a
    EOR tmp_0B                          ; 87F1  45 0b
    BNE TextFrameRows                   ; 87F3  d0 fa
    LDY #$00                            ; 87F5  a0 00
    LDA #$02                            ; 87F7  a9 02
    STA tmp_2F                          ; 87F9  85 2f
loc_20_87FB:
    LDA #$FE                            ; 87FB  a9 fe
    STA wram_buf,Y                      ; 87FD  99 00 60
    INY                                 ; 8800  c8
    LDA TextFrameTiles,X                ; 8801  bd 99 88
    STA wram_buf,Y                      ; 8804  99 00 60
    INX                                 ; 8807  e8
    INY                                 ; 8808  c8
    LDA #$1D                            ; 8809  a9 1d
    STA tmp_2E                          ; 880B  85 2e
loc_20_880D:
    LDA TextFrameTiles,X                ; 880D  bd 99 88
    STA wram_buf,Y                      ; 8810  99 00 60
    INY                                 ; 8813  c8
    DEC tmp_2E                          ; 8814  c6 2e
    BNE loc_20_880D                     ; 8816  d0 f5
    INX                                 ; 8818  e8
    LDA TextFrameTiles,X                ; 8819  bd 99 88
    STA wram_buf,Y                      ; 881C  99 00 60
    INX                                 ; 881F  e8
    INY                                 ; 8820  c8
    DEC tmp_2F                          ; 8821  c6 2f
    BNE loc_20_87FB                     ; 8823  d0 d6
    JSR TextQueueUpload                 ; 8825  20 36 88
    CLC                                 ; 8828  18
    LDA tmp_22                          ; 8829  a5 22
    ADC tmp_20                          ; 882B  65 20
    STA tmp_20                          ; 882D  85 20
    LDA tmp_23                          ; 882F  a5 23
    ADC tmp_21                          ; 8831  65 21
    STA tmp_21                          ; 8833  85 21
    RTS                                 ; 8835  60
TextQueueUpload:  ; append [hi lo len srcoff] to PPU queue ring $6200 (wr $0B rd $0A)
    JSR sub_20_8871                     ; 8836  20 71 88
    LDX tmp_0B                          ; 8839  a6 0b
    LDA tmp_21                          ; 883B  a5 21
    STA ppu_queue,X                     ; 883D  9d 00 62
    LDA tmp_20                          ; 8840  a5 20
    STA ppu_queue_1,X                   ; 8842  9d 01 62
    LDA tmp_24                          ; 8845  a5 24
    STA ppu_queue_2,X                   ; 8847  9d 02 62
    LDA tmp_25                          ; 884A  a5 25
    STA ppu_queue_3,X                   ; 884C  9d 03 62
    TXA                                 ; 884F  8a
    CLC                                 ; 8850  18
    ADC #$04                            ; 8851  69 04
    AND #$1F                            ; 8853  29 1f
    STA tmp_0B                          ; 8855  85 0b
    JMP loc_20_8869                     ; 8857  4c 69 88
TextSetR7:  ; inline R7 write (sets $6F/$50) used for text banks
    STA r7_shadow                       ; 885A  85 6f
    LDA #$07                            ; 885C  a9 07
    STA mmc3_banksel_shadow             ; 885E  85 50
    STA $8000                           ; 8860  8d 00 80
    LDA r7_shadow                       ; 8863  a5 6f
    STA $8001                           ; 8865  8d 01 80
    RTS                                 ; 8868  60
loc_20_8869:
    LDA ppuctrl_shadow                  ; 8869  a5 00
    ORA #$80                            ; 886B  09 80
    STA PPUCTRL                         ; 886D  8d 00 20
    RTS                                 ; 8870  60
sub_20_8871:
    LDA ppuctrl_shadow                  ; 8871  a5 00
    AND #$7F                            ; 8873  29 7f
    STA PPUCTRL                         ; 8875  8d 00 20
    RTS                                 ; 8878  60
TextWinParamsOpen:  ; 16-byte params -> $20-$2F (dest $2800 stride $40 len $40)
    .db $00,$28,$40,$00,$40,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 20:8879
    .db $20,$28,$00,$00,$40,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 20:8889
TextFrameTiles:  ; 3-byte row templates 18 19 1A / 1E 20 1F / 1B 1C 1D
    .db $18,$19,$1A,$1E,$20,$1F,$1E,$20,$1F,$1B,$1C,$1D; 20:8899
StaleText20_88A5:  ; dead filler to 88FF: copy of 22:A8A5 text
    .db $03,$01,$45,$78,$63,$65,$6C,$6C,$65,$6E,$74,$21,$20,$F7,$27,$76; 20:88A5
    .db $65,$20,$66,$69,$6E,$61,$6C,$6C,$79,$02,$72,$65,$76,$65,$61,$6C; 20:88B5
    .db $86,$BD,$21,$20,$FB,$FD,$65,$6E,$6A,$6F,$79,$02,$64,$65,$73,$74; 20:88C5
    .db $72,$6F,$79,$AC,$F3,$21,$20,$B0,$06,$10,$03,$01,$20,$B0,$05,$02; 20:88D5
    .db $CF,$DA,$06,$17,$27,$73,$02,$66,$69,$6E,$65,$73,$74,$20,$66,$6F; 20:88E5
    .db $75,$72,$2E,$20,$54,$68,$AD,$FD,$62,$65,$02; 20:88F5
TextDictPtrs:  ; 128 ptr16 dictionary words for codes $80-$FF
    .db $F0,$8A,$F3,$8A,$F6,$8A,$F9,$8A,$FD,$8A,$02,$8B,$06,$8B,$09,$8B; 20:8900
    .db $0C,$8B,$10,$8B,$14,$8B,$1A,$8B,$21,$8B,$26,$8B,$2B,$8B,$2E,$8B; 20:8910
    .db $31,$8B,$36,$8B,$3C,$8B,$40,$8B,$44,$8B,$49,$8B,$4E,$8B,$54,$8B; 20:8920
    .db $57,$8B,$5A,$8B,$5E,$8B,$63,$8B,$68,$8B,$6D,$8B,$72,$8B,$77,$8B; 20:8930
    .db $7B,$8B,$80,$8B,$84,$8B,$88,$8B,$8C,$8B,$91,$8B,$96,$8B,$9B,$8B; 20:8940
    .db $A0,$8B,$A5,$8B,$AA,$8B,$B2,$8B,$B5,$8B,$B9,$8B,$BC,$8B,$C1,$8B; 20:8950
    .db $C6,$8B,$CA,$8B,$CD,$8B,$D2,$8B,$D7,$8B,$DD,$8B,$E2,$8B,$E7,$8B; 20:8960
    .db $EC,$8B,$F1,$8B,$F6,$8B,$FB,$8B,$00,$8C,$05,$8C,$08,$8C,$0D,$8C; 20:8970
    .db $12,$8C,$17,$8C,$1C,$8C,$21,$8C,$26,$8C,$2B,$8C,$2E,$8C,$32,$8C; 20:8980
    .db $36,$8C,$3A,$8C,$3F,$8C,$44,$8C,$49,$8C,$4F,$8C,$52,$8C,$55,$8C; 20:8990
    .db $58,$8C,$5C,$8C,$60,$8C,$65,$8C,$6A,$8C,$70,$8C,$75,$8C,$7A,$8C; 20:89A0
    .db $80,$8C,$83,$8C,$87,$8C,$8B,$8C,$90,$8C,$95,$8C,$9A,$8C,$9F,$8C; 20:89B0
    .db $A4,$8C,$A9,$8C,$AE,$8C,$B4,$8C,$B7,$8C,$BA,$8C,$BE,$8C,$C3,$8C; 20:89C0
    .db $C8,$8C,$CC,$8C,$D0,$8C,$D5,$8C,$DA,$8C,$DF,$8C,$E4,$8C,$E9,$8C; 20:89D0
    .db $EE,$8C,$F3,$8C,$F8,$8C,$FE,$8C,$02,$8D,$07,$8D,$0E,$8D,$15,$8D; 20:89E0
    .db $19,$8D,$1D,$8D,$22,$8D,$25,$8D,$27,$8D,$2C,$8D,$31,$8D,$35,$8D; 20:89F0
TextStr05:  ; 10 ptr16 strings for ctrl 05
    .db $38,$8D,$3F,$8D,$46,$8D,$4A,$8D,$50,$8D,$56,$8D,$5E,$8D,$65,$8D; 20:8A00
    .db $6C,$8D,$77,$8D                 ; 20:8A10
TextStr06:  ; 36 ptr16 proper-noun strings for ctrl 06
    .db $8E,$8D,$96,$8D,$9E,$8D,$A4,$8D,$AB,$8D,$B1,$8D,$B5,$8D,$BB,$8D; 20:8A14
    .db $C2,$8D,$C8,$8D,$D2,$8D,$D8,$8D,$E0,$8D,$E7,$8D,$EC,$8D,$F1,$8D; 20:8A24
    .db $F7,$8D,$FE,$8D,$03,$8E,$0A,$8E,$0F,$8E,$18,$8E,$22,$8E,$2B,$8E; 20:8A34
    .db $35,$8E,$39,$8E,$3E,$8E,$43,$8E,$4C,$8E,$55,$8E,$59,$8E,$60,$8E; 20:8A44
    .db $68,$8E,$6D,$8E,$74,$8E,$7A,$8E ; 20:8A54
TextStr07:  ; 74 ptr16 item/spell names for ctrl 07/08
    .db $81,$8E,$8F,$8E,$9D,$8E,$AC,$8E,$BD,$8E,$C7,$8E,$D4,$8E,$E5,$8E; 20:8A5C
    .db $F2,$8E,$01,$8F,$0F,$8F,$21,$8F,$31,$8F,$40,$8F,$50,$8F,$5E,$8F; 20:8A6C
    .db $6E,$8F,$7E,$8F,$8D,$8F,$9B,$8F,$A9,$8F,$B7,$8F,$C3,$8F,$D1,$8F; 20:8A7C
    .db $DE,$8F,$ED,$8F,$FA,$8F,$07,$90,$14,$90,$21,$90,$2E,$90,$37,$90; 20:8A8C
    .db $43,$90,$51,$90,$60,$90,$6B,$90,$7A,$90,$85,$90,$94,$90,$A0,$90; 20:8A9C
    .db $AD,$90,$BB,$90,$C4,$90,$CF,$90,$DC,$90,$EA,$90,$F8,$90,$05,$91; 20:8AAC
    .db $13,$91,$1F,$91,$2B,$91,$38,$91,$46,$91,$52,$91,$5B,$91,$67,$91; 20:8ABC
    .db $73,$91,$81,$91,$8E,$91,$9D,$91,$AA,$91,$B7,$91,$C4,$91,$D0,$91; 20:8ACC
    .db $DB,$91,$E8,$91,$F0,$91,$FA,$91,$04,$92,$0D,$92,$15,$92,$1D,$92; 20:8ADC
    .db $24,$92,$2B,$92                 ; 20:8AEC
TextDict:  ; zero-terminated dictionary + table strings
    .db $61,$73,$00,$61,$74,$00,$61,$6E,$00,$61,$72,$65,$00,$61,$62,$6C; 20:8AF0
    .db $65,$00,$61,$6E,$64,$00,$65,$64,$00,$62,$65,$00,$42,$75,$74,$00; 20:8B00
    .db $62,$75,$74,$00,$51,$75,$65,$65,$6E,$00,$62,$61,$74,$74,$6C,$65; 20:8B10
    .db $00,$63,$61,$72,$65,$00,$63,$6F,$6D,$65,$00,$44,$6F,$00,$64,$6F; 20:8B20
    .db $00,$64,$6F,$77,$6E,$00,$44,$6F,$6E,$27,$74,$00,$6E,$27,$74,$00; 20:8B30
    .db $65,$6E,$64,$00,$65,$76,$65,$6E,$00,$66,$65,$65,$6C,$00,$66,$6F; 20:8B40
    .db $75,$6E,$64,$00,$67,$6F,$00,$77,$65,$00,$67,$65,$74,$00,$47,$6F; 20:8B50
    .db $6F,$64,$00,$67,$6F,$6F,$64,$00,$67,$6F,$6E,$65,$00,$67,$69,$72; 20:8B60
    .db $6C,$00,$67,$69,$76,$65,$00,$54,$68,$65,$00,$66,$69,$6E,$64,$00; 20:8B70
    .db $48,$65,$79,$00,$68,$6F,$77,$00,$68,$61,$73,$00,$68,$61,$76,$65; 20:8B80
    .db $00,$68,$6F,$6D,$65,$00,$68,$65,$72,$6F,$00,$68,$65,$6C,$70,$00; 20:8B90
    .db $68,$6F,$70,$65,$00,$68,$65,$72,$65,$00,$68,$61,$76,$65,$6E,$27; 20:8BA0
    .db $74,$00,$69,$6E,$00,$69,$6E,$67,$00,$69,$73,$00,$49,$27,$6C,$6C; 20:8BB0
    .db $00,$69,$6E,$74,$6F,$00,$49,$27,$6D,$00,$69,$74,$00,$6B,$65,$65; 20:8BC0
    .db $70,$00,$6B,$69,$6E,$64,$00,$61,$67,$61,$69,$6E,$00,$6B,$6E,$6F; 20:8BD0
    .db $77,$00,$6C,$6F,$6E,$67,$00,$6C,$65,$61,$64,$00,$6C,$61,$73,$74; 20:8BE0
    .db $00,$6C,$6F,$6F,$6B,$00,$6C,$65,$66,$74,$00,$6C,$61,$6B,$65,$00; 20:8BF0
    .db $6C,$6F,$73,$74,$00,$6D,$65,$00,$6D,$61,$6E,$79,$00,$6D,$61,$6B; 20:8C00
    .db $65,$00,$6D,$6F,$72,$65,$00,$6D,$6F,$73,$74,$00,$6D,$61,$64,$65; 20:8C10
    .db $00,$6D,$75,$63,$68,$00,$6D,$75,$73,$74,$00,$6E,$6F,$00,$4E,$6F; 20:8C20
    .db $77,$00,$6E,$6F,$77,$00,$6E,$6F,$74,$00,$6E,$65,$65,$64,$00,$6E; 20:8C30
    .db $65,$73,$73,$00,$6E,$65,$78,$74,$00,$6E,$65,$76,$65,$72,$00,$6F; 20:8C40
    .db $6E,$00,$6F,$72,$00,$6F,$66,$00,$6F,$66,$66,$00,$6F,$75,$74,$00; 20:8C50
    .db $6F,$6E,$6C,$79,$00,$6F,$76,$65,$72,$00,$72,$69,$67,$68,$74,$00; 20:8C60
    .db $73,$6F,$6D,$65,$00,$53,$75,$72,$65,$00,$73,$70,$65,$61,$6B,$00; 20:8C70
    .db $74,$6F,$00,$74,$6F,$6F,$00,$74,$68,$65,$00,$74,$68,$61,$6E,$00; 20:8C80
    .db $74,$68,$65,$79,$00,$74,$6F,$77,$6E,$00,$54,$61,$6B,$65,$00,$74; 20:8C90
    .db $61,$6B,$65,$00,$74,$68,$69,$73,$00,$74,$68,$61,$74,$00,$74,$68; 20:8CA0
    .db $65,$72,$65,$00,$75,$73,$00,$75,$70,$00,$75,$73,$65,$00,$75,$73; 20:8CB0
    .db $65,$64,$00,$76,$65,$72,$79,$00,$77,$61,$73,$00,$77,$61,$79,$00; 20:8CC0
    .db $77,$6F,$72,$6B,$00,$77,$68,$65,$6E,$00,$77,$61,$69,$74,$00,$77; 20:8CD0
    .db $65,$6C,$6C,$00,$77,$65,$72,$65,$00,$77,$65,$6E,$74,$00,$77,$69; 20:8CE0
    .db $74,$68,$00,$77,$68,$61,$74,$00,$77,$6F,$6E,$27,$74,$00,$79,$6F; 20:8CF0
    .db $75,$00,$79,$6F,$75,$72,$00,$79,$6F,$75,$27,$76,$65,$00,$79,$6F; 20:8D00
    .db $75,$27,$72,$65,$00,$59,$6F,$75,$00,$63,$61,$6E,$00,$63,$61,$76; 20:8D10
    .db $65,$00,$6D,$79,$00,$49,$00,$66,$72,$6F,$6D,$00,$77,$69,$6C,$6C; 20:8D20
    .db $00,$66,$6F,$72,$00,$65,$72,$00,$50,$6C,$65,$61,$73,$65,$00,$48; 20:8D30
    .db $75,$6D,$61,$6E,$73,$00,$6F,$6E,$65,$00,$70,$6F,$77,$65,$72,$00; 20:8D40
    .db $6D,$61,$67,$69,$63,$00,$44,$72,$61,$79,$67,$6F,$6E,$00,$69,$73; 20:8D50
    .db $6C,$61,$6E,$64,$00,$70,$65,$6F,$70,$6C,$65,$00,$42,$65,$20,$63; 20:8D60
    .db $61,$72,$65,$66,$75,$6C,$00,$77,$69,$73,$65,$20,$6D,$65,$6E,$00; 20:8D70
    .db $47,$72,$65,$61,$74,$20,$47,$65,$6E,$65,$72,$61,$6C,$00,$41,$72; 20:8D80
    .db $79,$6C,$6C,$69,$73,$00,$41,$6B,$61,$68,$61,$6E,$61,$00,$41,$73; 20:8D90
    .db $69,$6E,$61,$00,$41,$7A,$74,$65,$63,$61,$00,$43,$6C,$61,$72,$6B; 20:8DA0
    .db $00,$44,$65,$6F,$00,$45,$76,$61,$6E,$73,$00,$47,$65,$6C,$69,$6E; 20:8DB0
    .db $65,$00,$47,$75,$61,$72,$64,$00,$4B,$65,$6C,$62,$65,$73,$71,$75; 20:8DC0
    .db $65,$00,$4B,$65,$6E,$73,$75,$00,$4B,$61,$72,$6D,$69,$6E,$65,$00; 20:8DD0
    .db $4E,$61,$64,$61,$72,$65,$00,$4D,$61,$64,$6F,$00,$52,$61,$67,$65; 20:8DE0
    .db $00,$52,$61,$6C,$70,$68,$00,$53,$61,$62,$65,$72,$61,$00,$53,$74; 20:8DF0
    .db $6F,$6D,$00,$54,$6F,$72,$6E,$65,$6C,$00,$5A,$65,$62,$75,$00,$41; 20:8E00
    .db $6D,$61,$7A,$6F,$6E,$65,$73,$00,$41,$6E,$67,$72,$79,$20,$53,$65; 20:8E10
    .db $61,$00,$42,$72,$79,$6E,$6D,$61,$65,$72,$00,$44,$72,$61,$79,$67; 20:8E20
    .db $6F,$6E,$69,$61,$00,$47,$6F,$61,$00,$4A,$6F,$65,$6C,$00,$4C,$65; 20:8E30
    .db $61,$66,$00,$4D,$74,$2E,$53,$61,$62,$72,$65,$00,$4D,$74,$2E,$48; 20:8E40
    .db $79,$64,$72,$61,$00,$4F,$61,$6B,$00,$50,$6F,$72,$74,$6F,$61,$00; 20:8E50
    .db $50,$79,$72,$61,$6D,$69,$64,$00,$53,$77,$61,$6E,$00,$53,$61,$68; 20:8E60
    .db $61,$72,$61,$00,$4D,$65,$73,$69,$61,$00,$53,$68,$79,$72,$6F,$6E; 20:8E70
    .db $00,$53,$77,$6F,$72,$64,$20,$6F,$66,$20,$57,$69,$6E,$64,$00,$53; 20:8E80
    .db $77,$6F,$72,$64,$20,$6F,$66,$20,$46,$69,$72,$65,$00,$53,$77,$6F; 20:8E90
    .db $72,$64,$20,$6F,$66,$20,$57,$61,$74,$65,$72,$00,$53,$77,$6F,$72; 20:8EA0
    .db $64,$20,$6F,$66,$20,$54,$68,$75,$6E,$64,$65,$72,$00,$43,$72,$79; 20:8EB0
    .db $73,$74,$61,$6C,$69,$73,$00,$42,$61,$6C,$6C,$20,$6F,$66,$20,$57; 20:8EC0
    .db $69,$6E,$64,$00,$54,$6F,$72,$6E,$61,$64,$6F,$20,$42,$72,$61,$63; 20:8ED0
    .db $65,$6C,$65,$74,$00,$42,$61,$6C,$6C,$20,$6F,$66,$20,$46,$69,$72; 20:8EE0
    .db $65,$00,$46,$6C,$61,$6D,$65,$20,$42,$72,$61,$63,$65,$6C,$65,$74; 20:8EF0
    .db $00,$42,$61,$6C,$6C,$20,$6F,$66,$20,$57,$61,$74,$65,$72,$00,$42; 20:8F00
    .db $6C,$69,$7A,$7A,$61,$72,$64,$20,$42,$72,$61,$63,$65,$6C,$65,$74; 20:8F10
    .db $00,$42,$61,$6C,$6C,$20,$6F,$66,$20,$54,$68,$75,$6E,$64,$65,$72; 20:8F20
    .db $00,$53,$74,$6F,$72,$6D,$20,$42,$72,$61,$63,$65,$6C,$65,$74,$00; 20:8F30
    .db $43,$61,$72,$61,$70,$61,$63,$65,$20,$53,$68,$69,$65,$6C,$64,$00; 20:8F40
    .db $42,$72,$6F,$6E,$7A,$65,$20,$53,$68,$69,$65,$6C,$64,$00,$50,$6C; 20:8F50
    .db $61,$74,$69,$6E,$75,$6D,$20,$53,$68,$69,$65,$6C,$64,$00,$4D,$69; 20:8F60
    .db $72,$72,$6F,$72,$65,$64,$20,$53,$68,$69,$65,$6C,$64,$00,$43,$65; 20:8F70
    .db $72,$61,$6D,$69,$63,$20,$53,$68,$69,$65,$6C,$64,$00,$53,$61,$63; 20:8F80
    .db $72,$65,$64,$20,$53,$68,$69,$65,$6C,$64,$00,$42,$61,$74,$74,$6C; 20:8F90
    .db $65,$20,$53,$68,$69,$65,$6C,$64,$00,$50,$73,$79,$63,$68,$6F,$20; 20:8FA0
    .db $53,$68,$69,$65,$6C,$64,$00,$54,$61,$6E,$6E,$65,$64,$20,$48,$69; 20:8FB0
    .db $64,$65,$00,$4C,$65,$61,$74,$68,$65,$72,$20,$41,$72,$6D,$6F,$72; 20:8FC0
    .db $00,$42,$72,$6F,$6E,$7A,$65,$20,$41,$72,$6D,$6F,$72,$00,$50,$6C; 20:8FD0
    .db $61,$74,$69,$6E,$75,$6D,$20,$41,$72,$6D,$6F,$72,$00,$53,$6F,$6C; 20:8FE0
    .db $64,$69,$65,$72,$20,$53,$75,$69,$74,$00,$43,$65,$72,$61,$6D,$69; 20:8FF0
    .db $63,$20,$53,$75,$69,$74,$00,$42,$61,$74,$74,$6C,$65,$20,$41,$72; 20:9000
    .db $6D,$6F,$72,$00,$50,$73,$79,$63,$68,$6F,$20,$41,$72,$6D,$6F,$72; 20:9010
    .db $00,$4D,$65,$64,$69,$63,$61,$6C,$20,$48,$65,$72,$62,$00,$41,$6E; 20:9020
    .db $74,$69,$64,$6F,$74,$65,$00,$4C,$79,$73,$69,$73,$20,$50,$6C,$61; 20:9030
    .db $6E,$74,$00,$46,$72,$75,$69,$74,$20,$6F,$66,$20,$4C,$69,$6D,$65; 20:9040
    .db $00,$46,$72,$75,$69,$74,$20,$6F,$66,$20,$50,$6F,$77,$65,$72,$00; 20:9050
    .db $4D,$61,$67,$69,$63,$20,$52,$69,$6E,$67,$00,$46,$72,$75,$69,$74; 20:9060
    .db $20,$6F,$66,$20,$52,$65,$70,$75,$6E,$00,$57,$61,$72,$70,$20,$42; 20:9070
    .db $6F,$6F,$74,$73,$00,$53,$74,$61,$74,$75,$65,$20,$6F,$66,$20,$4F; 20:9080
    .db $6E,$79,$78,$00,$4F,$70,$65,$6C,$20,$53,$74,$61,$74,$75,$65,$00; 20:9090
    .db $49,$6E,$73,$65,$63,$74,$20,$46,$6C,$75,$74,$65,$00,$46,$6C,$75; 20:90A0
    .db $74,$65,$20,$6F,$66,$20,$4C,$69,$6D,$65,$00,$47,$61,$73,$20,$4D; 20:90B0
    .db $61,$73,$6B,$00,$50,$6F,$77,$65,$72,$20,$52,$69,$6E,$67,$00,$57; 20:90C0
    .db $61,$72,$72,$69,$6F,$72,$20,$52,$69,$6E,$67,$00,$49,$72,$6F,$6E; 20:90D0
    .db $20,$4E,$65,$63,$6B,$6C,$61,$63,$65,$00,$44,$65,$6F,$27,$73,$20; 20:90E0
    .db $50,$65,$6E,$64,$61,$6E,$74,$00,$52,$61,$62,$62,$69,$74,$20,$42; 20:90F0
    .db $6F,$6F,$74,$73,$00,$4C,$65,$61,$74,$68,$65,$72,$20,$42,$6F,$6F; 20:9100
    .db $74,$73,$00,$53,$68,$69,$65,$6C,$64,$20,$52,$69,$6E,$67,$00,$41; 20:9110
    .db $6C,$61,$72,$6D,$20,$46,$6C,$75,$74,$65,$00,$57,$69,$6E,$64,$6D; 20:9120
    .db $69,$6C,$6C,$20,$4B,$65,$79,$00,$4B,$65,$79,$20,$74,$6F,$20,$50; 20:9130
    .db $72,$69,$73,$6F,$6E,$00,$4B,$65,$79,$20,$74,$6F,$20,$53,$74,$78; 20:9140
    .db $79,$00,$46,$6F,$67,$20,$4C,$61,$6D,$70,$00,$53,$68,$65,$6C,$6C; 20:9150
    .db $20,$46,$6C,$75,$74,$65,$00,$45,$79,$65,$20,$47,$6C,$61,$73,$73; 20:9160
    .db $65,$73,$00,$42,$72,$6F,$6B,$65,$6E,$20,$53,$74,$61,$74,$75,$65; 20:9170
    .db $00,$47,$6C,$6F,$77,$69,$6E,$67,$20,$4C,$61,$6D,$70,$00,$53,$74; 20:9180
    .db $61,$74,$75,$65,$20,$6F,$66,$20,$47,$6F,$6C,$64,$00,$4C,$6F,$76; 20:9190
    .db $65,$20,$50,$65,$6E,$64,$61,$6E,$74,$00,$4B,$69,$72,$69,$73,$61; 20:91A0
    .db $20,$50,$6C,$61,$6E,$74,$00,$49,$76,$6F,$72,$79,$20,$53,$74,$61; 20:91B0
    .db $74,$75,$65,$00,$42,$6F,$77,$20,$6F,$66,$20,$4D,$6F,$6F,$6E,$00; 20:91C0
    .db $42,$6F,$77,$20,$6F,$66,$20,$53,$75,$6E,$00,$42,$6F,$77,$20,$6F; 20:91D0
    .db $66,$20,$54,$72,$75,$74,$68,$00,$52,$65,$66,$72,$65,$73,$68,$00; 20:91E0
    .db $50,$61,$72,$61,$6C,$79,$73,$69,$73,$00,$54,$65,$6C,$65,$70,$61; 20:91F0
    .db $74,$68,$79,$00,$54,$65,$6C,$65,$70,$6F,$72,$74,$00,$52,$65,$63; 20:9200
    .db $6F,$76,$65,$72,$00,$42,$61,$72,$72,$69,$65,$72,$00,$43,$68,$61; 20:9210
    .db $6E,$67,$65,$00,$46,$6C,$69,$67,$68,$74,$00,$20,$20,$20,$20,$20; 20:9220
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$00; 20:9230
StaleText20_923D:  ; dead filler to 93FF: copy of 22:B23D text
    .db $73,$2E,$20,$B0,$02,$77,$6F,$72,$72,$69,$86,$2E,$22,$00,$01,$06; 20:923D
    .db $11,$3A,$22,$41,$66,$74,$FF,$06,$17,$AD,$02,$64,$65,$73,$74,$72; 20:924D
    .db $6F,$79,$86,$2C,$20,$98,$27,$6C,$6C,$20,$A4,$61,$02,$72,$65,$6D; 20:925D
    .db $61,$74,$63,$68,$2E,$20,$54,$68,$AD,$74,$69,$BD,$FB,$F2,$02,$6C; 20:926D
    .db $6F,$73,$65,$2E,$22,$00,$01,$45,$79,$61,$61,$68,$21,$20,$48,$65; 20:927D
    .db $65,$79,$21,$20,$59,$6F,$77,$77,$21,$00,$01,$45,$69,$79,$61,$61; 20:928D
    .db $61,$68,$21,$00,$01,$06,$02,$3A,$22,$04,$21,$20,$06,$13,$AD,$02; 20:929D
    .db $77,$61,$69,$74,$AC,$FE,$F3,$AB,$DA,$02,$62,$61,$63,$6B,$20,$CF; 20:92AD
    .db $DA,$74,$65,$6D,$70,$6C,$65,$2C,$20,$73,$65,$65,$02,$68,$69,$6D; 20:92BD
    .db $20,$69,$6D,$6D,$65,$64,$69,$61,$74,$65,$6C,$79,$2E,$22,$00,$01; 20:92CD
    .db $06,$02,$3A,$22,$F7,$83,$FA,$B8,$02,$A8,$2E,$22,$00,$01,$06,$02; 20:92DD
    .db $AD,$68,$65,$6C,$70,$AC,$DA,$02,$77,$6F,$75,$6E,$64,$86,$61,$72; 20:92ED
    .db $6F,$75,$6E,$64,$20,$DA,$63,$6C,$6F,$63,$6B,$2E,$00,$01,$05,$08; 20:92FD
    .db $5F,$00,$01,$4F,$6F,$6F,$6F,$5F,$20,$4F,$77,$77,$77,$5F,$00,$01; 20:930D
    .db $4F,$6F,$6F,$6F,$5F,$20,$47,$69,$76,$65,$20,$BD,$77,$61,$74,$FF; 20:931D
    .db $5F,$20,$4F,$6F,$6F,$6F,$5F,$00,$01,$06,$13,$3A,$22,$47,$72,$65; 20:932D
    .db $61,$74,$21,$20,$F7,$C2,$B1,$02,$E0,$66,$61,$72,$2E,$20,$42,$65; 20:933D
    .db $73,$69,$64,$65,$73,$20,$DA,$02,$74,$68,$72,$65,$65,$20,$73,$77; 20:934D
    .db $6F,$72,$64,$73,$20,$F3,$C7,$5F,$22,$03,$01,$22,$5F,$70,$6F,$73; 20:935D
    .db $73,$65,$73,$73,$2C,$20,$DA,$C1,$02,$70,$6F,$77,$65,$72,$66,$75; 20:936D
    .db $6C,$2C,$20,$DA,$53,$77,$6F,$72,$64,$20,$CF,$02,$54,$68,$75,$6E; 20:937D
    .db $64,$65,$72,$2C,$20,$AD,$AB,$DA,$F9,$5F,$22,$03,$01,$22,$5F,$CF; 20:938D
    .db $53,$74,$79,$78,$2E,$20,$45,$76,$69,$6C,$20,$73,$70,$69,$72,$69; 20:939D
    .db $74,$73,$02,$B2,$E3,$FC,$69,$6E,$76,$61,$64,$AC,$02,$E2,$2E,$20; 20:93AD
    .db $57,$61,$74,$63,$68,$20,$79,$6F,$75,$72,$73,$65,$6C,$66,$2E,$22; 20:93BD
    .db $03,$01,$22,$9F,$67,$61,$74,$65,$20,$D8,$53,$74,$79,$78,$20,$AD; 20:93CD
    .db $81,$DA,$02,$74,$6F,$70,$20,$CF,$06,$1C,$2E,$20,$DE,$02,$E0,$6B; 20:93DD
    .db $65,$79,$20,$85,$97,$2E,$22,$00,$01,$06,$01,$3A,$22,$54,$68,$81; 20:93ED
    .db $27,$73,$20                     ; 20:93FD
WakeCutscene:  ; s38: own frame loop (LSR frame_ready); ~504 frames (emu f2351-2855) then substate 8 at 20:948E
    LSR frame_ready                     ; 9400  46 09
    BCC WakeCutscene                    ; 9402  90 fc
    LDX #$00                            ; 9404  a2 00
loc_20_9406:
    LDA $9B02,X                         ; 9406  bd 02 9b
    STA wram_6154,X                     ; 9409  9d 54 61
    INX                                 ; 940C  e8
    CPX #$0C                            ; 940D  e0 0c
    BNE loc_20_9406                     ; 940F  d0 f5
    JSR sub_20_94F5                     ; 9411  20 f5 94
    LDA #$73                            ; 9414  a9 73
    STA chr_r5_play                     ; 9416  8d f5 07
    LDA #$00                            ; 9419  a9 00
    STA wram_60F8                       ; 941B  8d f8 60
    LDA #$08                            ; 941E  a9 08
    STA tmp_21                          ; 9420  85 21
    LDA #$02                            ; 9422  a9 02
    JSR sub_20_94A6                     ; 9424  20 a6 94
    LDA #$C0                            ; 9427  a9 c0
    STA wram_60F8                       ; 9429  8d f8 60
    LDA #$03                            ; 942C  a9 03
    STA tmp_22                          ; 942E  85 22
    LDA #$60                            ; 9430  a9 60
    JSR sub_20_9756                     ; 9432  20 56 97
loc_20_9435:
    JSR sub_20_948F                     ; 9435  20 8f 94
    JSR sub_20_94F5                     ; 9438  20 f5 94
    JSR sub_20_95FA                     ; 943B  20 fa 95
    DEC wram_60F8                       ; 943E  ce f8 60
    BEQ loc_20_945F                     ; 9441  f0 1c
    LDA wram_60F8                       ; 9443  ad f8 60
    CMP #$60                            ; 9446  c9 60
    BEQ loc_20_9458                     ; 9448  f0 0e
    CMP #$A0                            ; 944A  c9 a0
    BEQ loc_20_9451                     ; 944C  f0 03
    JMP loc_20_9435                     ; 944E  4c 35 94
loc_20_9451:
    LDA #$02                            ; 9451  a9 02
    STA tmp_22                          ; 9453  85 22
    JMP loc_20_9435                     ; 9455  4c 35 94
loc_20_9458:
    LDA #$01                            ; 9458  a9 01
    STA tmp_22                          ; 945A  85 22
    JMP loc_20_9435                     ; 945C  4c 35 94
loc_20_945F:
    LDA #$61                            ; 945F  a9 61
    JSR sub_20_9756                     ; 9461  20 56 97
    JSR sub_20_94F5                     ; 9464  20 f5 94
    LDA #$01                            ; 9467  a9 01
    JSR sub_20_94A6                     ; 9469  20 a6 94
    JSR sub_20_9481                     ; 946C  20 81 94
    LDA #$00                            ; 946F  a9 00
    JSR sub_20_94A6                     ; 9471  20 a6 94
    LDA #$60                            ; 9474  a9 60
    STA tmp_22                          ; 9476  85 22
    JSR sub_20_974C                     ; 9478  20 4c 97
    JSR sub_20_94F5                     ; 947B  20 f5 94
    JMP loc_20_9565                     ; 947E  4c 65 95
sub_20_9481:
    LDX #$78                            ; 9481  a2 78
loc_20_9483:
    LSR frame_ready                     ; 9483  46 09
    BCC loc_20_9483                     ; 9485  90 fc
    DEX                                 ; 9487  ca
    BNE loc_20_9483                     ; 9488  d0 f9
    LDA #$08                            ; 948A  a9 08
    STA sub_state                       ; 948C  85 41
    RTS                                 ; 948E  60
sub_20_948F:
    LDA wram_60F8                       ; 948F  ad f8 60
    AND #$0C                            ; 9492  29 0c
    ASL A                               ; 9494  0a
    ASL A                               ; 9495  0a
    TAX                                 ; 9496  aa
    LDY #$00                            ; 9497  a0 00
loc_20_9499:
    LDA $9B0E,X                         ; 9499  bd 0e 9b
    STA pal_buf,Y                       ; 949C  99 40 61
    INX                                 ; 949F  e8
    INY                                 ; 94A0  c8
    CPY #$10                            ; 94A1  c0 10
    BNE loc_20_9499                     ; 94A3  d0 f4
    RTS                                 ; 94A5  60
sub_20_94A6:
    ASL A                               ; 94A6  0a
    ASL A                               ; 94A7  0a
    ASL A                               ; 94A8  0a
    STA tmp_20                          ; 94A9  85 20
    JSR sub_20_94C0                     ; 94AB  20 c0 94
    INC tmp_20                          ; 94AE  e6 20
    INC tmp_20                          ; 94B0  e6 20
    JSR sub_20_94C0                     ; 94B2  20 c0 94
    INC tmp_20                          ; 94B5  e6 20
    INC tmp_20                          ; 94B7  e6 20
    JSR sub_20_94C0                     ; 94B9  20 c0 94
    INC tmp_20                          ; 94BC  e6 20
    INC tmp_20                          ; 94BE  e6 20
sub_20_94C0:
    LDX tmp_21                          ; 94C0  a6 21
loc_20_94C2:
    LSR frame_ready                     ; 94C2  46 09
    BCC loc_20_94C2                     ; 94C4  90 fc
    DEX                                 ; 94C6  ca
    BNE loc_20_94C2                     ; 94C7  d0 f9
    LDX tmp_20                          ; 94C9  a6 20
    LDA WakePalPtrs,X                   ; 94CB  bd 6a 9a
    STA tmp_26                          ; 94CE  85 26
    LDA $9A6B,X                         ; 94D0  bd 6b 9a
    STA tmp_27                          ; 94D3  85 27
    LDA #$10                            ; 94D5  a9 10
    STA tmp_23                          ; 94D7  85 23
    LDY #$00                            ; 94D9  a0 00
loc_20_94DB:
    LDA (tmp_26),Y                      ; 94DB  b1 26
    STA pal_buf,Y                       ; 94DD  99 40 61
    INY                                 ; 94E0  c8
    DEC tmp_23                          ; 94E1  c6 23
    BNE loc_20_94DB                     ; 94E3  d0 f6
    LDA pal_buf                         ; 94E5  ad 40 61
    STA wram_6150                       ; 94E8  8d 50 61
    STA wram_6154                       ; 94EB  8d 54 61
    STA $6158                           ; 94EE  8d 58 61
    STA $615C                           ; 94F1  8d 5c 61
    RTS                                 ; 94F4  60
sub_20_94F5:
    LDX #$00                            ; 94F5  a2 00
    LDA #$F0                            ; 94F7  a9 f0
loc_20_94F9:
    STA oam_y,X                         ; 94F9  9d 00 02
    DEX                                 ; 94FC  ca
    BNE loc_20_94F9                     ; 94FD  d0 fa
    RTS                                 ; 94FF  60
WakeSprDraw:  ; s72: ($26) = count + n x (dy tile attr dx); dy=$80 hides the sprite; base y wram_60FC / x wram_60FF (dx<0 / carry clip); OAM from $2A
    LDY #$00                            ; 9500  a0 00
    LDA (tmp_26),Y                      ; 9502  b1 26
    STA tmp_2C                          ; 9504  85 2c
    INY                                 ; 9506  c8
    LDX tmp_2A                          ; 9507  a6 2a
loc_20_9509:
    LDA (tmp_26),Y                      ; 9509  b1 26
    CMP #$80                            ; 950B  c9 80
    BNE loc_20_9524                     ; 950D  d0 15
    LDA #$F8                            ; 950F  a9 f8
    STA oam_y,X                         ; 9511  9d 00 02
    STA oam_tile,X                      ; 9514  9d 01 02
    STA oam_attr,X                      ; 9517  9d 02 02
    STA oam_x,X                         ; 951A  9d 03 02
    INY                                 ; 951D  c8
    INY                                 ; 951E  c8
    INY                                 ; 951F  c8
    INY                                 ; 9520  c8
    JMP loc_20_9551                     ; 9521  4c 51 95
loc_20_9524:
    CLC                                 ; 9524  18
    ADC wram_60FC                       ; 9525  6d fc 60
    STA oam_y,X                         ; 9528  9d 00 02
    INY                                 ; 952B  c8
    LDA (tmp_26),Y                      ; 952C  b1 26
    STA oam_tile,X                      ; 952E  9d 01 02
    INY                                 ; 9531  c8
    LDA (tmp_26),Y                      ; 9532  b1 26
    STA oam_attr,X                      ; 9534  9d 02 02
    INY                                 ; 9537  c8
    LDA (tmp_26),Y                      ; 9538  b1 26
    BMI loc_20_9548                     ; 953A  30 0c
    CLC                                 ; 953C  18
    ADC wram_60FF                       ; 953D  6d ff 60
    BCS loc_20_955D                     ; 9540  b0 1b
    STA oam_x,X                         ; 9542  9d 03 02
    JMP loc_20_9551                     ; 9545  4c 51 95
loc_20_9548:
    CLC                                 ; 9548  18
    ADC wram_60FF                       ; 9549  6d ff 60
    BCC loc_20_955D                     ; 954C  90 0f
    STA oam_x,X                         ; 954E  9d 03 02
loc_20_9551:
    INX                                 ; 9551  e8
    INX                                 ; 9552  e8
    INX                                 ; 9553  e8
    INX                                 ; 9554  e8
    STX tmp_2A                          ; 9555  86 2a
    INY                                 ; 9557  c8
    DEC tmp_2C                          ; 9558  c6 2c
    BNE loc_20_9509                     ; 955A  d0 ad
    RTS                                 ; 955C  60
loc_20_955D:
    LDA #$F8                            ; 955D  a9 f8
    STA oam_y,X                         ; 955F  9d 00 02
    JMP loc_20_9551                     ; 9562  4c 51 95
loc_20_9565:
    LDA #$07                            ; 9565  a9 07
    STA wram_60F8                       ; 9567  8d f8 60
    LDA #$5D                            ; 956A  a9 5d
    JSR sub_20_9756                     ; 956C  20 56 97
loc_20_956F:
    JSR sub_20_95B8                     ; 956F  20 b8 95
    DEC wram_60F8                       ; 9572  ce f8 60
    LDA wram_60F8                       ; 9575  ad f8 60
    CMP #$FF                            ; 9578  c9 ff
    BNE loc_20_956F                     ; 957A  d0 f3
    LDA #$5C                            ; 957C  a9 5c
    JSR sub_20_9756                     ; 957E  20 56 97
    LDA #$30                            ; 9581  a9 30
    STA tmp_22                          ; 9583  85 22
    JSR sub_20_974C                     ; 9585  20 4c 97
    LDA #$04                            ; 9588  a9 04
    STA tmp_2A                          ; 958A  85 2a
    LDA #$1A                            ; 958C  a9 1a
    JSR WakeSprFrame                    ; 958E  20 3d 97
    LDA #$00                            ; 9591  a9 00
    JSR WakeSprFrame                    ; 9593  20 3d 97
    JSR sub_20_9706                     ; 9596  20 06 97
    LDA #$60                            ; 9599  a9 60
    STA tmp_22                          ; 959B  85 22
    JSR sub_20_974C                     ; 959D  20 4c 97
    LDA #$5E                            ; 95A0  a9 5e
    JSR sub_20_9756                     ; 95A2  20 56 97
    LDA #$07                            ; 95A5  a9 07
    STA wram_60F8                       ; 95A7  8d f8 60
loc_20_95AA:
    JSR sub_20_95D5                     ; 95AA  20 d5 95
    DEC wram_60F8                       ; 95AD  ce f8 60
    LDA wram_60F8                       ; 95B0  ad f8 60
    CMP #$FF                            ; 95B3  c9 ff
    BNE loc_20_95AA                     ; 95B5  d0 f3
    RTS                                 ; 95B7  60
sub_20_95B8:
    LDA #$80                            ; 95B8  a9 80
    STA wram_60FF                       ; 95BA  8d ff 60
    LDA #$73                            ; 95BD  a9 73
    STA wram_60FC                       ; 95BF  8d fc 60
    LDA #$04                            ; 95C2  a9 04
    STA tmp_2A                          ; 95C4  85 2a
    LDA wram_60F8                       ; 95C6  ad f8 60
    AND #$07                            ; 95C9  29 07
    JSR WakeSprFrame                    ; 95CB  20 3d 97
    LDA #$03                            ; 95CE  a9 03
    STA tmp_22                          ; 95D0  85 22
    JMP sub_20_974C                     ; 95D2  4c 4c 97
sub_20_95D5:
    LDA #$80                            ; 95D5  a9 80
    STA wram_60FF                       ; 95D7  8d ff 60
    LDA #$73                            ; 95DA  a9 73
    STA wram_60FC                       ; 95DC  8d fc 60
    LDA #$04                            ; 95DF  a9 04
    STA tmp_2A                          ; 95E1  85 2a
    LDA #$1A                            ; 95E3  a9 1a
    JSR WakeSprFrame                    ; 95E5  20 3d 97
    LDA wram_60F8                       ; 95E8  ad f8 60
    AND #$07                            ; 95EB  29 07
    CLC                                 ; 95ED  18
    ADC #$08                            ; 95EE  69 08
    JSR WakeSprFrame                    ; 95F0  20 3d 97
    LDA #$03                            ; 95F3  a9 03
    STA tmp_22                          ; 95F5  85 22
    JMP sub_20_974C                     ; 95F7  4c 4c 97
sub_20_95FA:
    LDA #$04                            ; 95FA  a9 04
    STA tmp_2A                          ; 95FC  85 2a
    LDA wram_60F8                       ; 95FE  ad f8 60
    AND #$03                            ; 9601  29 03
    ASL A                               ; 9603  0a
    TAX                                 ; 9604  aa
    LDA $9675,X                         ; 9605  bd 75 96
    STA wram_60FF                       ; 9608  8d ff 60
    LDA $9676,X                         ; 960B  bd 76 96
    STA wram_60FC                       ; 960E  8d fc 60
    LDA #$1B                            ; 9611  a9 1b
    JSR WakeSprFrame                    ; 9613  20 3d 97
    LDA #$60                            ; 9616  a9 60
    STA wram_60FF                       ; 9618  8d ff 60
    LDA #$6B                            ; 961B  a9 6b
    STA wram_60FC                       ; 961D  8d fc 60
    LDA wram_60F8                       ; 9620  ad f8 60
    AND #$03                            ; 9623  29 03
    CLC                                 ; 9625  18
    ADC #$1C                            ; 9626  69 1c
    JSR WakeSprFrame                    ; 9628  20 3d 97
    LDA wram_60F8                       ; 962B  ad f8 60
    AND #$0C                            ; 962E  29 0c
    LSR A                               ; 9630  4a
    TAX                                 ; 9631  aa
    LDA $967D,X                         ; 9632  bd 7d 96
    STA wram_60FF                       ; 9635  8d ff 60
    LDA $967E,X                         ; 9638  bd 7e 96
    STA wram_60FC                       ; 963B  8d fc 60
    LDA #$19                            ; 963E  a9 19
    JSR WakeSprFrame                    ; 9640  20 3d 97
    LDA wram_60F8                       ; 9643  ad f8 60
    AND #$08                            ; 9646  29 08
    BEQ loc_20_9659                     ; 9648  f0 0f
    LDA #$58                            ; 964A  a9 58
    STA wram_60FF                       ; 964C  8d ff 60
    LDA #$53                            ; 964F  a9 53
    STA wram_60FC                       ; 9651  8d fc 60
    LDA #$18                            ; 9654  a9 18
    JSR WakeSprFrame                    ; 9656  20 3d 97
loc_20_9659:
    LDA wram_60F8                       ; 9659  ad f8 60
    AND #$04                            ; 965C  29 04
    LSR A                               ; 965E  4a
    LSR A                               ; 965F  4a
    LSR A                               ; 9660  4a
    LDA #$80                            ; 9661  a9 80
    STA wram_60FF                       ; 9663  8d ff 60
    LDA #$43                            ; 9666  a9 43
    STA wram_60FC                       ; 9668  8d fc 60
    LDA #$00                            ; 966B  a9 00
    ADC #$20                            ; 966D  69 20
    JSR WakeSprFrame                    ; 966F  20 3d 97
    JMP sub_20_974C                     ; 9672  4c 4c 97
    .db $5B,$2E,$5B,$2C,$5B,$2A,$5B,$28,$78,$5F,$78,$5B,$78,$57,$78,$53; 20:9675
    .db $A9,$03,$85,$23,$20,$93,$96,$A9,$01,$85,$22,$4C,$4C,$97,$20,$F5; 20:9685
    .db $94,$AD,$F8,$60,$4A,$90,$2F,$A5,$23,$0A,$AA,$AD,$F8,$60,$4A,$4A; 20:9695
    .db $85,$20,$BD,$F6,$96,$38,$E5,$20,$8D,$FF,$60,$BD,$F7,$96,$8D,$FC; 20:96A5
    .db $60,$AD,$F8,$60,$29,$0C,$4A,$4A,$69,$14,$20,$3D,$97,$C6,$23,$A5; 20:96B5
    .db $23,$C9,$FF,$D0,$D2,$60,$A5,$23,$0A,$AA,$AD,$F8,$60,$4A,$4A,$18; 20:96C5
    .db $7D,$FE,$96,$8D,$FF,$60,$BD,$FF,$96,$8D,$FC,$60,$AD,$F8,$60,$29; 20:96D5
    .db $0C,$4A,$4A,$69,$10,$20,$3D,$97,$C6,$23,$A5,$23,$C9,$FF,$D0,$D6; 20:96E5
    .db $60,$80,$68,$90,$70,$80,$A0,$88,$B0,$80,$48,$68,$88,$78,$90,$80; 20:96F5
    .db $B6                             ; 20:9705
sub_20_9706:
    LDA #$00                            ; 9706  a9 00
    JSR sub_20_9717                     ; 9708  20 17 97
    LDA #$04                            ; 970B  a9 04
    JSR sub_20_9717                     ; 970D  20 17 97
    LDA #$08                            ; 9710  a9 08
    JSR sub_20_9717                     ; 9712  20 17 97
    LDA #$0C                            ; 9715  a9 0c
sub_20_9717:
    TAX                                 ; 9717  aa
    LDY #$00                            ; 9718  a0 00
loc_20_971A:
    LDA $972D,X                         ; 971A  bd 2d 97
    STA wram_6150,Y                     ; 971D  99 50 61
    INY                                 ; 9720  c8
    INX                                 ; 9721  e8
    CPY #$04                            ; 9722  c0 04
    BNE loc_20_971A                     ; 9724  d0 f4
    LDA #$08                            ; 9726  a9 08
    STA tmp_22                          ; 9728  85 22
    JMP sub_20_974C                     ; 972A  4c 4c 97
    .db $0F,$0F,$0F,$0F,$0F,$16,$0F,$0F,$0F,$26,$04,$0F,$0F,$36,$14,$0F; 20:972D
WakeSprFrame:  ; s72: A=frame -> WakeSprFramePtrs -> JMP WakeSprDraw 20:9500; 12 callers in WakeCutscene
    ASL A                               ; 973D  0a
    TAX                                 ; 973E  aa
    LDA WakeSprFramePtrs,X              ; 973F  bd 67 97
    STA tmp_26                          ; 9742  85 26
    LDA $9768,X                         ; 9744  bd 68 97
    STA tmp_27                          ; 9747  85 27
    JMP WakeSprDraw                     ; 9749  4c 00 95
sub_20_974C:
    LDX tmp_22                          ; 974C  a6 22
loc_20_974E:
    LSR frame_ready                     ; 974E  46 09
    BCC loc_20_974E                     ; 9750  90 fc
    DEX                                 ; 9752  ca
    BNE loc_20_974E                     ; 9753  d0 f9
    RTS                                 ; 9755  60
sub_20_9756:
    STA snd_queue                       ; 9756  8d 03 01
    LDA #$01                            ; 9759  a9 01
    STA snd_queue_lock                  ; 975B  8d 01 01
    STA snd_queue_count                 ; 975E  8d 00 01
    LDA #$00                            ; 9761  a9 00
    STA snd_queue_lock                  ; 9763  8d 01 01
    RTS                                 ; 9766  60
WakeSprFramePtrs:  ; s72 (was TextPtrTable_9767): .dw[34] WakeCutscene metasprite frames $97AB-$9A51 (not text); WakeSprFrame 20:973D A=frame
    .db $32,$98,$11,$98,$F8,$97,$DF,$97,$CE,$97,$BD,$97,$B4,$97,$AB,$97; 20:9767
    .db $AB,$97,$B4,$97,$BD,$97,$CE,$97,$DF,$97,$F8,$97,$11,$98,$32,$98; 20:9777
    .db $53,$98,$74,$98,$95,$98,$B6,$98,$3A,$99,$19,$99,$F8,$98,$D7,$98; 20:9787
    .db $5B,$99,$6C,$99,$51,$9A,$7D,$99,$BE,$99,$D7,$99,$F0,$99,$05,$9A; 20:9797
    .db $2F,$9A,$40,$9A,$02,$F8,$FC,$02,$00,$F8,$FC,$02,$08,$02,$F8,$FD; 20:97A7
    .db $02,$00,$F8,$FD,$02,$08,$04,$F0,$FC,$02,$00,$F0,$FC,$02,$08,$F8; 20:97B7
    .db $FD,$02,$00,$F8,$FD,$02,$08,$04,$F0,$FD,$02,$00,$F0,$FD,$02,$08; 20:97C7
    .db $F8,$FD,$02,$00,$F8,$FD,$02,$08,$06,$E8,$FC,$02,$00,$E8,$FC,$02; 20:97D7
    .db $08,$F0,$FD,$02,$00,$F0,$FD,$02,$08,$F8,$FD,$02,$00,$F8,$FD,$02; 20:97E7
    .db $08,$06,$E8,$FD,$02,$00,$E8,$FD,$02,$08,$F0,$FD,$02,$00,$F0,$FD; 20:97F7
    .db $02,$08,$F8,$FD,$02,$00,$F8,$FD,$02,$08,$08,$E0,$FC,$02,$00,$E0; 20:9807
    .db $FC,$02,$08,$E8,$FD,$02,$00,$E8,$FD,$02,$08,$F0,$FD,$02,$00,$F0; 20:9817
    .db $FD,$02,$08,$F8,$FD,$02,$00,$F8,$FD,$02,$08,$08,$E0,$FD,$02,$00; 20:9827
    .db $E0,$FD,$02,$08,$E8,$FD,$02,$00,$E8,$FD,$02,$08,$F0,$FD,$02,$00; 20:9837
    .db $F0,$FD,$02,$08,$F8,$FD,$02,$00,$F8,$FD,$02,$08,$08,$F8,$F0,$02; 20:9847
    .db $00,$F8,$F1,$02,$08,$F8,$F0,$02,$10,$F8,$F1,$02,$18,$F8,$F0,$02; 20:9857
    .db $20,$F8,$F1,$02,$28,$F8,$F0,$02,$30,$F8,$F1,$02,$38,$08,$F8,$F2; 20:9867
    .db $02,$00,$F8,$F3,$02,$08,$F8,$F2,$02,$10,$F8,$F3,$02,$18,$F8,$F2; 20:9877
    .db $02,$20,$F8,$F3,$02,$28,$F8,$F2,$02,$30,$F8,$F3,$02,$38,$08,$F8; 20:9887
    .db $F4,$02,$00,$F8,$F5,$02,$08,$F8,$F4,$02,$10,$F8,$F5,$02,$18,$F8; 20:9897
    .db $F4,$02,$20,$F8,$F5,$02,$28,$F8,$F4,$02,$30,$F8,$F5,$02,$38,$08; 20:98A7
    .db $F8,$F6,$02,$00,$F8,$F7,$02,$08,$F8,$F6,$02,$10,$F8,$F7,$02,$18; 20:98B7
    .db $F8,$F6,$02,$20,$F8,$F7,$02,$28,$F8,$F6,$02,$30,$F8,$F7,$02,$38; 20:98C7
    .db $08,$F8,$F7,$42,$00,$F8,$F6,$42,$F8,$F8,$F7,$42,$F0,$F8,$F6,$42; 20:98D7
    .db $E8,$F8,$F7,$42,$E0,$F8,$F6,$42,$D8,$F8,$F7,$42,$D0,$F8,$F6,$42; 20:98E7
    .db $C8,$08,$F8,$F5,$42,$00,$F8,$F4,$42,$F8,$F8,$F5,$42,$F0,$F8,$F4; 20:98F7
    .db $42,$E8,$F8,$F5,$42,$E0,$F8,$F4,$42,$D8,$F8,$F5,$42,$D0,$F8,$F4; 20:9907
    .db $42,$C8,$08,$F8,$F3,$42,$00,$F8,$F2,$42,$F8,$F8,$F3,$42,$F0,$F8; 20:9917
    .db $F2,$42,$E8,$F8,$F3,$42,$E0,$F8,$F2,$42,$D8,$F8,$F3,$42,$D0,$F8; 20:9927
    .db $F2,$42,$C8,$08,$F8,$F1,$42,$00,$F8,$F0,$42,$F8,$F8,$F1,$42,$F0; 20:9937
    .db $F8,$F0,$42,$E8,$F8,$F1,$42,$E0,$F8,$F0,$42,$D8,$F8,$F1,$42,$D0; 20:9947
    .db $F8,$F0,$42,$C8,$04,$00,$F8,$03,$00,$00,$F8,$03,$60,$10,$F8,$03; 20:9957
    .db $00,$10,$F8,$03,$60,$04,$00,$F9,$03,$00,$00,$F9,$03,$18,$10,$F9; 20:9967
    .db $03,$00,$10,$F9,$03,$18,$10,$00,$FA,$01,$00,$08,$FA,$01,$00,$10; 20:9977
    .db $FA,$01,$00,$18,$FA,$01,$00,$F8,$FA,$01,$20,$00,$FA,$01,$20,$08; 20:9987
    .db $FA,$01,$20,$10,$FA,$01,$20,$F8,$FA,$01,$38,$00,$FA,$01,$38,$08; 20:9997
    .db $FA,$01,$38,$10,$FA,$01,$38,$00,$FA,$01,$58,$08,$FA,$01,$58,$10; 20:99A7
    .db $FA,$01,$58,$18,$FA,$01,$58,$06,$E0,$FA,$01,$0C,$F8,$FA,$01,$0C; 20:99B7
    .db $E0,$FA,$01,$40,$F8,$FA,$01,$40,$F8,$FA,$01,$EC,$F8,$FA,$01,$60; 20:99C7
    .db $06,$E0,$FA,$01,$08,$F8,$FA,$01,$08,$E0,$FA,$01,$44,$F8,$FA,$01; 20:99D7
    .db $44,$F8,$FA,$01,$E8,$F8,$FA,$01,$64,$05,$E0,$FA,$01,$04,$F8,$FA; 20:99E7
    .db $01,$04,$E0,$FA,$01,$48,$F8,$FA,$01,$48,$F8,$FA,$01,$DC,$05,$E0; 20:99F7
    .db $FA,$01,$00,$F8,$FA,$01,$00,$E0,$FA,$01,$4C,$F8,$FA,$01,$4C,$F8; 20:9A07
    .db $FA,$01,$D8,$E0,$FA,$01,$00,$E0,$FA,$01,$F8,$18,$FA,$01,$00,$18; 20:9A17
    .db $FA,$01,$F8,$01,$00,$FA,$01,$00,$04,$00,$FB,$03,$00,$08,$FB,$02; 20:9A27
    .db $00,$00,$FB,$03,$08,$08,$FB,$02,$08,$04,$00,$FB,$02,$00,$08,$FB; 20:9A37
    .db $03,$00,$00,$FB,$02,$08,$08,$FB,$03,$08,$06,$E7,$1A,$00,$00,$E7; 20:9A47
    .db $1B,$00,$08,$EF,$00,$00,$00,$EF,$01,$00,$08,$F7,$20,$00,$00,$F7; 20:9A57
    .db $21,$00,$08                     ; 20:9A67
WakePalPtrs:  ; s72 (was TextPtrTable_9A6A): .dw[12] 16-byte BG palette sets $9A82-$9B01 -> pal_buf (20:94C9; X=$20 byte offset); fade cycle 0-3-0 / 7 / 8-11
    .db $92,$9A,$A2,$9A,$B2,$9A,$C2,$9A,$B2,$9A,$A2,$9A,$92,$9A,$82,$9A; 20:9A6A
    .db $F2,$9A,$E2,$9A,$D2,$9A,$C2,$9A,$30,$30,$30,$30,$30,$30,$30,$30; 20:9A7A
    .db $30,$30,$30,$30,$30,$30,$11,$0F,$10,$30,$30,$30,$10,$30,$31,$30; 20:9A8A
    .db $10,$30,$37,$30,$10,$30,$11,$0F,$00,$30,$30,$31,$00,$3B,$21,$30; 20:9A9A
    .db $00,$37,$27,$30,$00,$30,$11,$0F,$0F,$30,$31,$21,$0F,$2B,$11,$30; 20:9AAA
    .db $0F,$27,$17,$37,$0F,$30,$11,$0F,$0F,$20,$21,$11,$0F,$1B,$01,$31; 20:9ABA
    .db $0F,$17,$07,$27,$0F,$30,$11,$0F,$0F,$10,$11,$01,$0F,$1A,$01,$21; 20:9ACA
    .db $0F,$07,$07,$17,$0F,$30,$11,$0F,$0F,$00,$01,$0F,$0F,$0F,$0F,$11; 20:9ADA
    .db $0F,$0F,$0F,$07,$0F,$30,$11,$0F,$0F,$0F,$0F,$0F,$0F,$0F,$0F,$0F; 20:9AEA
    .db $0F,$0F,$0F,$0F,$0F,$30,$11,$0F,$0F,$30,$31,$0F,$0F,$05,$31,$0F; 20:9AFA
    .db $0F,$25,$31,$0F,$0F,$20,$21,$11,$0F,$1B,$01,$31,$0F,$17,$07,$27; 20:9B0A
    .db $0F,$30,$11,$0F,$0F,$10,$11,$01,$0F,$1A,$01,$21,$0F,$07,$0F,$17; 20:9B1A
    .db $0F,$30,$11,$0F,$0F,$30,$30,$31,$0F,$3B,$21,$30,$0F,$37,$27,$30; 20:9B2A
    .db $0F,$30,$11,$0F,$0F,$30,$31,$21,$0F,$2B,$11,$30,$0F,$27,$17,$30; 20:9B3A
    .db $0F,$30,$11,$0F                 ; 20:9B4A
StaleText20_9B4E:  ; dead filler to 9BFF: copy of 22:BB4E text
    .db $69,$6C,$6C,$02,$63,$61,$70,$74,$69,$76,$65,$2E,$20,$FB,$FD,$72; 20:9B4E
    .db $65,$63,$6F,$76,$FF,$02,$F4,$73,$74,$72,$65,$6E,$67,$74,$68,$2E; 20:9B5E
    .db $20,$DE,$8C,$2E,$22,$00,$01,$06,$10,$3A,$22,$4C,$6F,$6E,$67,$20; 20:9B6E
    .db $74,$69,$BD,$C5,$73,$65,$65,$21,$02,$48,$61,$21,$20,$FB,$F2,$6C; 20:9B7E
    .db $65,$74,$20,$F3,$99,$62,$79,$02,$BD,$21,$20,$42,$65,$20,$70,$72; 20:9B8E
    .db $65,$70,$61,$72,$86,$21,$22,$00,$01,$06,$12,$3A,$22,$B0,$73,$61; 20:9B9E
    .db $76,$86,$21,$20,$42,$79,$20,$DA,$02,$77,$61,$79,$2C,$20,$A2,$AD; 20:9BAE
    .db $06,$11,$21,$22,$02,$04,$3A,$22,$5F,$5F,$5F,$22,$03,$01,$06,$12; 20:9BBE
    .db $3A,$22,$FB,$73,$65,$65,$5F,$20,$88,$68,$65,$20,$68,$65,$6C,$64; 20:9BCE
    .db $02,$CD,$D8,$DA,$6C,$61,$73,$74,$5F,$20,$73,$69,$67,$68,$2E,$20; 20:9BDE
    .db $49,$02,$FD,$72,$65,$73,$74,$6F,$72,$65,$20,$F4,$73,$74,$72,$65; 20:9BEE
    .db $6E,$67,$FF,$42,$01,$01,$04,$0C,$10,$01,$04,$0C,$11,$01,$04,$08; 20:9BFE
    .db $12,$04,$FF,$43,$01,$01,$04,$07,$18,$01,$04,$05,$19,$01,$04,$05; 20:9C0E
    .db $1A,$01,$FF,$44,$01,$01,$04,$07,$14,$01,$04,$0C,$15,$01,$04,$0C; 20:9C1E
    .db $16,$08,$FF,$45,$01,$01,$04,$07,$1D,$03,$04,$0B,$1E,$07,$04,$05; 20:9C2E
    .db $1F,$01,$0C,$0D,$20,$01,$0C,$0D,$21,$01,$0C,$0D,$22,$01,$04,$05; 20:9C3E
    .db $33,$01,$05,$07,$32,$01,$00,$00,$00,$00,$0D,$1F,$E3,$01,$04,$0C; 20:9C4E
    .db $17,$01,$00,$00,$00,$00,$02,$03,$08,$01,$0B,$0C,$09,$01,$03,$04; 20:9C5E
    .db $0A,$01,$05,$0C,$13,$01,$05,$0C,$17,$01,$05,$0C,$1B,$01,$00,$00; 20:9C6E
    .db $00,$00,$0D,$1F,$0B,$01,$0D,$1F,$0B,$01,$0D,$1F,$0B,$01,$0D,$1F; 20:9C7E
    .db $0B,$01,$0D,$1F,$0B,$01,$0D,$1F,$0B,$01,$0D,$1F,$3F,$01,$0E,$12; 20:9C8E
    .db $BC,$01,$0D,$1F,$B8,$01,$0D,$1F,$B9,$01,$0D,$1F,$BA,$01,$0D,$1F; 20:9C9E
    .db $BB,$01,$0D,$1F,$0B,$01,$0D,$1F,$BE,$01,$1A,$1F,$BF,$01,$14,$1F; 20:9CAE
    .db $CE,$01,$14,$1F,$CD,$01,$0E,$1F,$CC,$01,$0F,$1F,$CB,$01,$0D,$1F; 20:9CBE
    .db $CA,$01,$0D,$1F,$C9,$01,$1B,$1F,$C8,$01,$0D,$1F,$C7,$01,$0D,$1F; 20:9CCE
    .db $C6,$01,$0D,$1F,$C5,$04,$14,$15,$C4,$01,$15,$1F,$C3,$01,$0E,$10; 20:9CDE
    .db $C1,$01,$11,$1F,$C0,$01,$0D,$1F,$D7,$01,$0D,$1F,$D5,$01,$0D,$1F; 20:9CEE
    .db $0C,$01,$0D,$1F,$E0,$01,$0D,$1F,$E1,$01,$0D,$1F,$E2,$01,$0D,$1F; 20:9CFE
    .db $E3,$01,$0D,$1F,$E4,$01,$0D,$1F,$E5,$01,$0D,$1F,$E6,$01,$0D,$1F; 20:9D0E
    .db $E7,$01,$0D,$1F,$E8,$01,$0D,$1F,$E9,$01,$0D,$1F,$EA,$01,$0D,$1F; 20:9D1E
    .db $EB,$01,$0D,$1F,$EC,$01,$0D,$1F,$ED,$01,$0D,$1F,$EE,$01,$0D,$1F; 20:9D2E
    .db $EF,$01,$0D,$1F,$F0,$01,$0D,$1F,$F1,$01,$0D,$1F,$F2,$01,$0D,$1F; 20:9D3E
    .db $F3,$01,$0D,$1F,$F4,$01,$0D,$1F,$F5,$01,$0D,$1F,$F6,$01,$0D,$1F; 20:9D4E
    .db $F7,$01,$0D,$1F,$F8,$01,$0D,$1F,$F9,$01,$0D,$1F,$FA,$01,$0D,$1F; 20:9D5E
    .db $FB,$01,$0D,$1F,$FC,$01,$0D,$1F,$FD,$01,$0D,$1F,$FE,$01,$0D,$1F; 20:9D6E
    .db $FF,$01,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 20:9D7E
    .db $00,$00,$00,$F0,$00,$08,$08,$FD,$00,$08,$00,$08,$00,$08,$F8,$FD; 20:9D8E
    .db $00,$08,$00,$04,$07,$01,$F9,$01,$0A,$F8,$F6,$F8,$07,$EF,$F9,$EF; 20:9D9E
    .db $00,$EC,$00,$E8,$00,$08,$18,$FD,$00,$08,$00,$18,$00,$08,$E8,$FD; 20:9DAE
    .db $00,$08,$00,$EF,$00,$08,$0C,$FD,$00,$08,$00,$0C,$00,$08,$F4,$FD; 20:9DBE
    .db $00,$08,$18,$1A,$00,$28,$E8,$1A,$DC,$00,$E8,$EA,$00,$DC,$18,$EA; 20:9DCE
    .db $24,$00,$00,$EC,$07,$EF,$0A,$FC,$07,$01,$00,$04,$F9,$01,$F6,$FC; 20:9DDE
    .db $F9,$EF,$00,$C8,$00,$08,$42,$00,$00,$08,$00,$46,$00,$08,$BE,$00; 20:9DEE
    .db $00,$08,$00,$F1,$00,$F1,$00,$F1,$00,$F1,$00,$F1,$00,$F1,$00,$F1; 20:9DFE
    .db $00,$F1,$08,$30,$08,$30,$00,$00,$08,$30,$08,$30,$08,$30,$00,$00; 20:9E0E
    .db $08,$30,$10,$F0,$10,$F0,$10,$F0,$10,$F0,$F0,$F0,$F0,$F0,$F0,$F0; 20:9E1E
    .db $F0,$F0                         ; 20:9E2E
StaleText20_9E30:  ; dead filler to 9FFF: copy of 22:BE30 text + 18 tail
    .db $20,$FB,$BA,$02,$77,$69,$74,$68,$20,$61,$20,$66,$72,$69,$93,$AB; 20:9E30
    .db $47,$5F,$20,$06,$18,$5F,$02,$55,$68,$2C,$20,$75,$75,$75,$68,$5F; 20:9E40
    .db $5F,$5F,$22,$00,$01,$06,$11,$5F,$20,$20,$68,$75,$72,$72,$79,$20; 20:9E50
    .db $20,$45,$73,$63,$61,$70,$65,$5F,$00,$01,$06,$11,$5F,$20,$B0,$67; 20:9E60
    .db $6C,$61,$64,$5F,$20,$20,$F7,$27,$72,$65,$02,$73,$61,$66,$65,$5F; 20:9E70
    .db $20,$5F,$00,$01,$06,$11,$3A,$22,$4F,$6F,$6F,$6F,$68,$5F,$20,$06; 20:9E80
    .db $17,$27,$73,$02,$61,$72,$FA,$63,$61,$BD,$5F,$20,$45,$76,$65,$72; 20:9E90
    .db $79,$05,$02,$02,$6B,$69,$6C,$6C,$86,$5F,$20,$77,$61,$74,$63,$68; 20:9EA0
    .db $20,$D1,$FE,$02,$06,$0D,$5F,$22,$03,$01,$22,$48,$65,$20,$AD,$C0; 20:9EB0
    .db $74,$72,$65,$61,$63,$68,$65,$72,$6F,$E3,$02,$DB,$DA,$6F,$74,$68; 20:9EC0
    .db $FF,$74,$68,$72,$65,$65,$5F,$20,$55,$68,$5F,$22,$02,$04,$3A,$22; 20:9ED0
    .db $91,$D7,$2E,$20,$EC,$2E,$22,$03,$01,$06,$11,$3A,$22,$54,$61,$6B; 20:9EE0
    .db $65,$20,$8C,$CF,$5F,$20,$06,$12,$5F,$02,$09,$04,$4F,$6F,$6F,$6F; 20:9EF0
    .db $5F,$5F,$20,$5F,$5F,$20,$5F,$22,$02,$04,$3A,$22,$06,$11,$2C,$20; 20:9F00
    .db $68,$6F,$6C,$64,$20,$CD,$21,$02,$09,$04,$06,$11,$21,$22,$00,$01; 20:9F10
    .db $48,$61,$21,$48,$61,$21,$20,$B0,$06,$0D,$21,$20,$FB,$64,$65,$73; 20:9F20
    .db $74,$72,$6F,$79,$86,$02,$74,$68,$6F,$73,$65,$20,$73,$69,$6C,$6C; 20:9F30
    .db $79,$20,$05,$09,$02,$61,$67,$61,$69,$6E,$73,$74,$20,$DA,$45,$6D; 20:9F40
    .db $70,$65,$72,$CE,$2E,$03,$01,$F7,$FD,$C7,$6A,$6F,$AB,$DA,$02,$05; 20:9F50
    .db $07,$CF,$06,$23,$21,$00,$01,$91,$74,$68,$69,$6E,$6B,$20,$E1,$F5; 20:9F60
    .db $02,$77,$6F,$6E,$21,$20,$91,$66,$6F,$72,$99,$98,$73,$74,$69,$6C; 20:9F70
    .db $6C,$02,$A4,$06,$22,$85,$DA,$6F,$74,$68,$FF,$02,$05,$09,$2E,$00; 20:9F80
    .db $3B,$3F,$21,$22,$26,$27,$2C,$2D,$2E,$5F,$FF,$20,$FF,$20,$20,$FF; 20:9F90
    .db $FF,$FF,$FF,$FF,$FF,$20,$20,$20,$20,$FF,$FF,$FF,$FF,$FF,$FF,$FF; 20:9FA0
    .db $20,$20,$20,$20,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$20,$20,$20,$FF; 20:9FB0
    .db $FF,$FF,$CE,$FE,$06,$F0,$01,$60,$EE,$F1,$06,$60,$20,$B3,$82,$20; 20:9FC0
    .db $D8,$81,$20,$98,$81,$A9,$03,$85,$51,$A9,$00,$A2,$28,$95,$70,$9D; 20:9FD0
    .db $00,$06,$CA,$10,$F8,$A2,$80,$9D,$00,$06,$E8,$D0,$FA,$A9,$00,$8D; 20:9FE0
    .db $F0,$06,$8D,$F1,$06,$8D,$F2,$06,$8D,$F3,$06,$85,$09,$85,$02,$85; 20:9FF0
