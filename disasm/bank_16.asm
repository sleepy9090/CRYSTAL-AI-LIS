; re-c bank 16  file 0x20010  CPU $8000-$9FFF  (tools/disasm_all.py)

ppuctrl_shadow = $00
ppumask_shadow = $01
frame_ready = $09
tmp_0B = $0B
jmp_trampoline = $10
tmp_11 = $11
tmp_12 = $12
tmp_13 = $13
tmp_14 = $14
tmp_15 = $15
tmp_17 = $17
tmp_18 = $18
tmp_19 = $19
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
irq_mode = $51
chr_r0_alt = $58
chr_r1_alt = $59
chr_r2_alt = $5A
chr_r3_alt = $5B
chr_r4_alt = $5C
chr_r5_alt = $5D
zp_5E = $5E
zp_5F = $5F
area_id = $6C
snd_queue_count = $0100
snd_queue_lock = $0101
snd_queue = $0103
oam_y = $0200
player_hp_max = $03C0
player_hp = $03C1
gold = $0702
gold_hi = $0703
mp_cur = $0708
mp_max = $0709
obj1_flash = $0710
equip_sword = $0711
ram_07DD = $07DD
PPUCTRL = $2000
wram_buf = $6000
wram_6001 = $6001
wram_6002 = $6002
wram_6020 = $6020
wram_6021 = $6021
wram_6040 = $6040
wram_6041 = $6041
wram_60DF = $60DF
wram_60F8 = $60F8
wram_60F9 = $60F9
wram_60FA = $60FA
wram_60FB = $60FB
pal_buf = $6140
wram_6150 = $6150
wram_6154 = $6154
ppu_queue = $6200
ppu_queue_1 = $6201
ppu_queue_2 = $6202
ppu_queue_3 = $6203
wram_6424 = $6424
wram_6426 = $6426
wram_6427 = $6427
inv_select = $6428
inventory = $6430
wram_646A = $646A
save_slot_sel = $646B
wram_646C = $646C
shop_id = $646D
wram_646E = $646E
shop_goods = $6470
shop_price_sel = $6474
shop_price_sel_hi = $6475
shop_prices = $6478
menu_save_state = $7000
save_reset_flag = $7001
save_csum0_a = $70F0
save_csum0_b = $70F1
save_csum1_a = $70F2
save_csum1_b = $70F3
save_checksum = $70F5
SaveJumpTable = $BC00
sub_23_BC03 = $BC03
AreaSaveFlags = $BF00
WaitFrame = $C169
WaitPpuQueue = $C676
PpuQueueFlush = $C67D
FadeSaveZpToWram = $C7D6
loc_30_C80C = $C80C
ReadPadsMenu = $FEE0
FarCall_B26_8E46 = $FFA9

    .org $8000
sub_16_8000:
    JSR sub_16_8605                     ; 8000  20 05 86
    JSR WaitFrame                       ; 8003  20 69 c1
    LDA #$00                            ; 8006  a9 00
    STA menu_save_state                 ; 8008  8d 00 70
    LDX #$00                            ; 800B  a2 00
loc_16_800D:
    LDA $8AED,X                         ; 800D  bd ed 8a
    STA tmp_20,X                        ; 8010  95 20
    INX                                 ; 8012  e8
    CPX #$10                            ; 8013  e0 10
    BNE loc_16_800D                     ; 8015  d0 f6
    LDY #$58                            ; 8017  a0 58
    STY chr_r2_alt                      ; 8019  84 5a
    INY                                 ; 801B  c8
    STY chr_r3_alt                      ; 801C  84 5b
    INY                                 ; 801E  c8
    STY chr_r4_alt                      ; 801F  84 5c
    INY                                 ; 8021  c8
    STY chr_r5_alt                      ; 8022  84 5d
    LDA #$3C                            ; 8024  a9 3c
    STA zp_5E                           ; 8026  85 5e
    LDA #$38                            ; 8028  a9 38
    STA zp_5F                           ; 802A  85 5f
    JSR sub_16_881E                     ; 802C  20 1e 88
    JSR WaitFrame                       ; 802F  20 69 c1
    LDA #$06                            ; 8032  a9 06
    STA irq_mode                        ; 8034  85 51
    LDA ppuctrl_shadow                  ; 8036  a5 00
    AND #$FC                            ; 8038  29 fc
    ORA #$02                            ; 803A  09 02
    STA ppuctrl_shadow                  ; 803C  85 00
    LDA #$0F                            ; 803E  a9 0f
    STA pal_buf                         ; 8040  8d 40 61
    STA $6144                           ; 8043  8d 44 61
    STA $6148                           ; 8046  8d 48 61
    STA $614C                           ; 8049  8d 4c 61
    STA wram_6150                       ; 804C  8d 50 61
    STA wram_6154                       ; 804F  8d 54 61
    STA $6158                           ; 8052  8d 58 61
    STA $615C                           ; 8055  8d 5c 61
    LDA #$00                            ; 8058  a9 00
    STA wram_6424                       ; 805A  8d 24 64
    STA $6425                           ; 805D  8d 25 64
    STA wram_6426                       ; 8060  8d 26 64
    STA wram_6427                       ; 8063  8d 27 64
    STA wram_6427                       ; 8066  8d 27 64
    STA wram_646E                       ; 8069  8d 6e 64
    STA $646F                           ; 806C  8d 6f 64
    STA pad_new                         ; 806F  85 4b
    JSR sub_16_9A62                     ; 8071  20 62 9a
    JSR sub_16_8534                     ; 8074  20 34 85
    JSR sub_16_9A91                     ; 8077  20 91 9a
loc_16_807A:
    LDA #$00                            ; 807A  a9 00
    BEQ loc_16_8080                     ; 807C  f0 02
loc_16_807E:
    LDA #$01                            ; 807E  a9 01
loc_16_8080:
    STA wram_6424                       ; 8080  8d 24 64
    LDA menu_save_state                 ; 8083  ad 00 70
    CMP #$FA                            ; 8086  c9 fa
    BNE loc_16_8092                     ; 8088  d0 08
    LDA #$4C                            ; 808A  a9 4c
    JSR sub_16_8A9F                     ; 808C  20 9f 8a
    JMP sub_16_8605                     ; 808F  4c 05 86
loc_16_8092:
    CMP #$FB                            ; 8092  c9 fb
    BNE loc_16_80A1                     ; 8094  d0 0b
    LDA #$55                            ; 8096  a9 55
loc_16_8098:
    JSR sub_16_8A9F                     ; 8098  20 9f 8a
    JSR sub_16_8605                     ; 809B  20 05 86
    JMP loc_30_C80C                     ; 809E  4c 0c c8
loc_16_80A1:
    CMP #$AB                            ; 80A1  c9 ab
    BNE loc_16_80A9                     ; 80A3  d0 04
    LDA #$4C                            ; 80A5  a9 4c
    BNE loc_16_8098                     ; 80A7  d0 ef
loc_16_80A9:
    LDA #$86                            ; 80A9  a9 86
    STA irq_mode                        ; 80AB  85 51
    LDA tmp_2A                          ; 80AD  a5 2a
    PHA                                 ; 80AF  48
    LDA tmp_2B                          ; 80B0  a5 2b
    PHA                                 ; 80B2  48
    LDA wram_6427                       ; 80B3  ad 27 64
    PHA                                 ; 80B6  48
    JSR sub_16_81FD                     ; 80B7  20 fd 81
    JSR sub_16_871F                     ; 80BA  20 1f 87
    PLA                                 ; 80BD  68
    STA wram_6427                       ; 80BE  8d 27 64
    PLA                                 ; 80C1  68
    STA tmp_2B                          ; 80C2  85 2b
    PLA                                 ; 80C4  68
    STA tmp_2A                          ; 80C5  85 2a
    JSR sub_16_823A                     ; 80C7  20 3a 82
    LDA #$06                            ; 80CA  a9 06
    STA irq_mode                        ; 80CC  85 51
    JMP loc_16_8171                     ; 80CE  4c 71 81
loc_16_80D1:
    JSR WaitFrame                       ; 80D1  20 69 c1
    JSR ReadPadsMenu                    ; 80D4  20 e0 fe
    LDA pad_new                         ; 80D7  a5 4b
    AND #$08                            ; 80D9  29 08
    BEQ loc_16_80F7                     ; 80DB  f0 1a
    LDA tmp_2B                          ; 80DD  a5 2b
    DEC tmp_2B                          ; 80DF  c6 2b
    BPL loc_16_80F4                     ; 80E1  10 11
    LDA #$03                            ; 80E3  a9 03
    STA tmp_2B                          ; 80E5  85 2b
    LDA tmp_2A                          ; 80E7  a5 2a
    CMP #$04                            ; 80E9  c9 04
    BMI loc_16_80EF                     ; 80EB  30 02
    LDA #$03                            ; 80ED  a9 03
loc_16_80EF:
    STA tmp_2A                          ; 80EF  85 2a
    JMP loc_16_81ED                     ; 80F1  4c ed 81
loc_16_80F4:
    JMP loc_16_8171                     ; 80F4  4c 71 81
loc_16_80F7:
    LDA pad_new                         ; 80F7  a5 4b
    AND #$04                            ; 80F9  29 04
    BEQ loc_16_8119                     ; 80FB  f0 1c
    INC tmp_2B                          ; 80FD  e6 2b
    LDA tmp_2B                          ; 80FF  a5 2b
    CMP #$04                            ; 8101  c9 04
    BCC loc_16_8116                     ; 8103  90 11
    LDA #$00                            ; 8105  a9 00
    STA tmp_2B                          ; 8107  85 2b
    LDA tmp_2A                          ; 8109  a5 2a
    CMP #$04                            ; 810B  c9 04
    BMI loc_16_8111                     ; 810D  30 02
    LDA #$03                            ; 810F  a9 03
loc_16_8111:
    STA tmp_2A                          ; 8111  85 2a
    JMP loc_16_81ED                     ; 8113  4c ed 81
loc_16_8116:
    JMP loc_16_8171                     ; 8116  4c 71 81
loc_16_8119:
    LDA pad_new                         ; 8119  a5 4b
    AND #$02                            ; 811B  29 02
    BEQ loc_16_812A                     ; 811D  f0 0b
    DEC tmp_2A                          ; 811F  c6 2a
    BPL loc_16_8127                     ; 8121  10 04
    LDA #$00                            ; 8123  a9 00
    STA tmp_2A                          ; 8125  85 2a
loc_16_8127:
    JMP loc_16_8171                     ; 8127  4c 71 81
loc_16_812A:
    LDA pad_new                         ; 812A  a5 4b
    AND #$01                            ; 812C  29 01
    BEQ loc_16_8140                     ; 812E  f0 10
    LDX tmp_2A                          ; 8130  a6 2a
    INC tmp_2A                          ; 8132  e6 2a
    LDA tmp_2A                          ; 8134  a5 2a
    CMP wram_6426                       ; 8136  cd 26 64
    BCC loc_16_813D                     ; 8139  90 02
    STX tmp_2A                          ; 813B  86 2a
loc_16_813D:
    JMP loc_16_8171                     ; 813D  4c 71 81
loc_16_8140:
    LDA pad_new                         ; 8140  a5 4b
    AND #$20                            ; 8142  29 20
    BEQ loc_16_8149                     ; 8144  f0 03
    JMP loc_16_81EC                     ; 8146  4c ec 81
loc_16_8149:
    LDA pad_new                         ; 8149  a5 4b
    AND #$10                            ; 814B  29 10
    BEQ loc_16_815C                     ; 814D  f0 0d
    LDA ram_07DD                        ; 814F  ad dd 07
    BNE loc_16_815C                     ; 8152  d0 08
    LDX #$00                            ; 8154  a2 00
    JSR sub_16_821F                     ; 8156  20 1f 82
    JMP SaveLoadPage                    ; 8159  4c ee 9a
loc_16_815C:
    LDA pad_new                         ; 815C  a5 4b
    AND #$80                            ; 815E  29 80
    BEQ loc_16_8165                     ; 8160  f0 03
    JSR sub_16_82B7                     ; 8162  20 b7 82
loc_16_8165:
    LDA pad_new                         ; 8165  a5 4b
    AND #$40                            ; 8167  29 40
    BEQ loc_16_816E                     ; 8169  f0 03
    JSR sub_16_8342                     ; 816B  20 42 83
loc_16_816E:
    JMP loc_16_80D1                     ; 816E  4c d1 80
loc_16_8171:
    JSR sub_16_85E6                     ; 8171  20 e6 85
    STA wram_646E                       ; 8174  8d 6e 64
    LDA #$4B                            ; 8177  a9 4b
    JSR sub_16_8A9F                     ; 8179  20 9f 8a
    LDA wram_6424                       ; 817C  ad 24 64
    BNE loc_16_818B                     ; 817F  d0 0a
    LDA tmp_2B                          ; 8181  a5 2b
    ASL A                               ; 8183  0a
    ASL A                               ; 8184  0a
    CLC                                 ; 8185  18
    ADC tmp_2A                          ; 8186  65 2a
    JMP loc_16_8193                     ; 8188  4c 93 81
loc_16_818B:
    LDA tmp_2B                          ; 818B  a5 2b
    ASL A                               ; 818D  0a
    ASL A                               ; 818E  0a
    ASL A                               ; 818F  0a
    CLC                                 ; 8190  18
    ADC tmp_2A                          ; 8191  65 2a
loc_16_8193:
    STA wram_6427                       ; 8193  8d 27 64
    JSR sub_16_8699                     ; 8196  20 99 86
    JSR sub_16_85E6                     ; 8199  20 e6 85
    CMP #$FF                            ; 819C  c9 ff
    BEQ loc_16_81E4                     ; 819E  f0 44
    STA tmp_19                          ; 81A0  85 19
    LDA #$0F                            ; 81A2  a9 0f
    JSR MenuPrint                       ; 81A4  20 33 89
    LDA ram_07DD                        ; 81A7  ad dd 07
    BNE loc_16_81B4                     ; 81AA  d0 08
    LDA #$06                            ; 81AC  a9 06
    JSR MenuPrint                       ; 81AE  20 33 89
    JMP loc_16_815C                     ; 81B1  4c 5c 81
loc_16_81B4:
    LDA #$0F                            ; 81B4  a9 0f
    JSR MenuPrint                       ; 81B6  20 33 89
    LDA #$06                            ; 81B9  a9 06
    JSR MenuPrint                       ; 81BB  20 33 89
    JSR sub_16_85E6                     ; 81BE  20 e6 85
    ASL A                               ; 81C1  0a
    TAX                                 ; 81C2  aa
    LDA $9EC2,X                         ; 81C3  bd c2 9e
    STA shop_price_sel                  ; 81C6  8d 74 64
    LDA $9EC3,X                         ; 81C9  bd c3 9e
    STA shop_price_sel_hi               ; 81CC  8d 75 64
    LDX #$33                            ; 81CF  a2 33
    LDA wram_6424                       ; 81D1  ad 24 64
    BEQ loc_16_81D8                     ; 81D4  f0 02
    LDX #$34                            ; 81D6  a2 34
loc_16_81D8:
    TXA                                 ; 81D8  8a
    JSR MenuPrint                       ; 81D9  20 33 89
    LDA #$0D                            ; 81DC  a9 0d
    JSR sub_16_8613                     ; 81DE  20 13 86
    JMP loc_16_815C                     ; 81E1  4c 5c 81
loc_16_81E4:
    LDA #$0F                            ; 81E4  a9 0f
    JSR MenuPrint                       ; 81E6  20 33 89
    JMP loc_16_80D1                     ; 81E9  4c d1 80
loc_16_81EC:
    RTS                                 ; 81EC  60
loc_16_81ED:
    LDA #$0F                            ; 81ED  a9 0f
    JSR MenuPrint                       ; 81EF  20 33 89
    LDA wram_6424                       ; 81F2  ad 24 64
    BEQ loc_16_81FA                     ; 81F5  f0 03
    JMP loc_16_807A                     ; 81F7  4c 7a 80
loc_16_81FA:
    JMP loc_16_807E                     ; 81FA  4c 7e 80
sub_16_81FD:
    LDX wram_6424                       ; 81FD  ae 24 64
    JSR sub_16_821F                     ; 8200  20 1f 82
    LDA #$3C                            ; 8203  a9 3c
    STA chr_r0_alt                      ; 8205  85 58
    STA zp_5E                           ; 8207  85 5e
    LDX wram_6424                       ; 8209  ae 24 64
    LDA MenuChrR1,X                     ; 820C  bd 32 82
    STA chr_r1_alt                      ; 820F  85 59
    LDA $8238,X                         ; 8211  bd 38 82
    STA wram_6426                       ; 8214  8d 26 64
    LDA $8234,X                         ; 8217  bd 34 82
    STA zp_5F                           ; 821A  85 5f
    JMP loc_16_826C                     ; 821C  4c 6c 82
sub_16_821F:
    LDA $8236,X                         ; 821F  bd 36 82
    TAX                                 ; 8222  aa
    LDY #$00                            ; 8223  a0 00
loc_16_8225:
    LDA $8BFB,X                         ; 8225  bd fb 8b
    STA wram_6150,Y                     ; 8228  99 50 61
    INX                                 ; 822B  e8
    INY                                 ; 822C  c8
    CPY #$10                            ; 822D  c0 10
    BNE loc_16_8225                     ; 822F  d0 f4
    RTS                                 ; 8231  60
MenuChrR1:  ; s70: 4 bytes 3E 3A 3A 38 = chr_r1_alt per menu kind $6424 (R0 always $3C)
    .db $3E,$3A,$3A,$38,$00,$10,$04,$08 ; 16:8232
sub_16_823A:
    LDA #$11                            ; 823A  a9 11
    JSR MenuPrint                       ; 823C  20 33 89
    LDA wram_6424                       ; 823F  ad 24 64
    BEQ loc_16_8253                     ; 8242  f0 0f
    LDA #$10                            ; 8244  a9 10
    JSR MenuPrint                       ; 8246  20 33 89
    LDA #$04                            ; 8249  a9 04
    JSR MenuPrint                       ; 824B  20 33 89
    LDA #$05                            ; 824E  a9 05
    JMP MenuPrint                       ; 8250  4c 33 89
loc_16_8253:
    LDA #$10                            ; 8253  a9 10
    JSR MenuPrint                       ; 8255  20 33 89
    LDA #$00                            ; 8258  a9 00
    JSR MenuPrint                       ; 825A  20 33 89
    LDA #$01                            ; 825D  a9 01
    JSR MenuPrint                       ; 825F  20 33 89
    LDA #$02                            ; 8262  a9 02
    JSR MenuPrint                       ; 8264  20 33 89
    LDA #$03                            ; 8267  a9 03
    JMP MenuPrint                       ; 8269  4c 33 89
loc_16_826C:
    JSR sub_16_8605                     ; 826C  20 05 86
sub_16_826F:
    LDX #$00                            ; 826F  a2 00
    STX tmp_2B                          ; 8271  86 2b
    LDA wram_6424                       ; 8273  ad 24 64
    BNE loc_16_8296                     ; 8276  d0 1e
loc_16_8278:
    LDX tmp_2B                          ; 8278  a6 2b
    LDA inv_select,X                    ; 827A  bd 28 64
    BMI loc_16_828D                     ; 827D  30 0e
    STA tmp_2A                          ; 827F  85 2a
    LDA tmp_2B                          ; 8281  a5 2b
    ASL A                               ; 8283  0a
    ASL A                               ; 8284  0a
    ADC tmp_2A                          ; 8285  65 2a
    STA wram_6427                       ; 8287  8d 27 64
    JSR sub_16_8660                     ; 828A  20 60 86
loc_16_828D:
    INC tmp_2B                          ; 828D  e6 2b
    LDA tmp_2B                          ; 828F  a5 2b
    CMP #$04                            ; 8291  c9 04
    BCC loc_16_8278                     ; 8293  90 e3
    RTS                                 ; 8295  60
loc_16_8296:
    LDX tmp_2B                          ; 8296  a6 2b
    LDA $642C,X                         ; 8298  bd 2c 64
    BMI loc_16_82AE                     ; 829B  30 11
    STA tmp_2A                          ; 829D  85 2a
    LDA tmp_2B                          ; 829F  a5 2b
    ASL A                               ; 82A1  0a
    ASL A                               ; 82A2  0a
    ASL A                               ; 82A3  0a
    CLC                                 ; 82A4  18
    ADC tmp_2A                          ; 82A5  65 2a
    CLC                                 ; 82A7  18
    STA wram_6427                       ; 82A8  8d 27 64
    JSR sub_16_8660                     ; 82AB  20 60 86
loc_16_82AE:
    INC tmp_2B                          ; 82AE  e6 2b
    LDA tmp_2B                          ; 82B0  a5 2b
    CMP #$04                            ; 82B2  c9 04
    BCC loc_16_8296                     ; 82B4  90 e0
    RTS                                 ; 82B6  60
sub_16_82B7:
    LDA ram_07DD                        ; 82B7  ad dd 07
    BEQ loc_16_82BF                     ; 82BA  f0 03
    JMP loc_16_835E                     ; 82BC  4c 5e 83
loc_16_82BF:
    JSR sub_16_85E6                     ; 82BF  20 e6 85
    CMP #$FF                            ; 82C2  c9 ff
    BNE loc_16_82D0                     ; 82C4  d0 0a
    LDA #$4E                            ; 82C6  a9 4e
    JSR sub_16_8A9F                     ; 82C8  20 9f 8a
    LDA #$0F                            ; 82CB  a9 0f
    JMP MenuPrint                       ; 82CD  4c 33 89
loc_16_82D0:
    LDA wram_6424                       ; 82D0  ad 24 64
    BEQ loc_16_82D7                     ; 82D3  f0 02
    LDA #$04                            ; 82D5  a9 04
loc_16_82D7:
    CLC                                 ; 82D7  18
    ADC tmp_2B                          ; 82D8  65 2b
    TAX                                 ; 82DA  aa
    LDA inv_select,X                    ; 82DB  bd 28 64
    ASL A                               ; 82DE  0a
    BCS loc_16_8304                     ; 82DF  b0 23
    LSR A                               ; 82E1  4a
    CMP tmp_2A                          ; 82E2  c5 2a
    BNE loc_16_8304                     ; 82E4  d0 1e
    LDA inv_select,X                    ; 82E6  bd 28 64
    ORA #$80                            ; 82E9  09 80
    STA inv_select,X                    ; 82EB  9d 28 64
    LDA #$4B                            ; 82EE  a9 4b
    JSR sub_16_8A9F                     ; 82F0  20 9f 8a
    JSR sub_16_8A5A                     ; 82F3  20 5a 8a
    JSR sub_16_85F6                     ; 82F6  20 f6 85
    TAX                                 ; 82F9  aa
    LDA #$00                            ; 82FA  a9 00
    STA equip_sword,X                   ; 82FC  9d 11 07
    LDA #$0F                            ; 82FF  a9 0f
    JMP MenuPrint                       ; 8301  4c 33 89
loc_16_8304:
    LDA tmp_2A                          ; 8304  a5 2a
    STA inv_select,X                    ; 8306  9d 28 64
    JSR sub_16_8660                     ; 8309  20 60 86
    LDA #$4C                            ; 830C  a9 4c
    JSR sub_16_8A9F                     ; 830E  20 9f 8a
    JSR sub_16_85E6                     ; 8311  20 e6 85
    STA tmp_19                          ; 8314  85 19
    TAX                                 ; 8316  aa
    LDA $8FF0,X                         ; 8317  bd f0 8f
    AND #$20                            ; 831A  29 20
    BEQ loc_16_8326                     ; 831C  f0 08
    LDA #$09                            ; 831E  a9 09
    JSR MenuPrint                       ; 8320  20 33 89
    JMP loc_16_832B                     ; 8323  4c 2b 83
loc_16_8326:
    LDA #$0A                            ; 8326  a9 0a
    JSR MenuPrint                       ; 8328  20 33 89
loc_16_832B:
    JSR sub_16_85F6                     ; 832B  20 f6 85
    TAY                                 ; 832E  a8
    JSR sub_16_85E6                     ; 832F  20 e6 85
    TAX                                 ; 8332  aa
    LDA $903B,X                         ; 8333  bd 3b 90
    STA equip_sword,Y                   ; 8336  99 11 07
    RTS                                 ; 8339  60
    .db $00,$02,$03,$07,$04,$05,$04,$01 ; 16:833A
sub_16_8342:
    LDA ram_07DD                        ; 8342  ad dd 07
    BEQ loc_16_835E                     ; 8345  f0 17
    LDA #$4C                            ; 8347  a9 4c
    JSR sub_16_8A9F                     ; 8349  20 9f 8a
    LDA #$0F                            ; 834C  a9 0f
    JSR MenuPrint                       ; 834E  20 33 89
    LDA #$31                            ; 8351  a9 31
    JSR MenuPrint                       ; 8353  20 33 89
    LDA #$0E                            ; 8356  a9 0e
    JSR MenuPrint                       ; 8358  20 33 89
    JMP loc_16_847B                     ; 835B  4c 7b 84
loc_16_835E:
    JSR sub_16_85E6                     ; 835E  20 e6 85
    CMP #$FF                            ; 8361  c9 ff
    BNE loc_16_836A                     ; 8363  d0 05
    LDA #$4E                            ; 8365  a9 4e
    JMP sub_16_8A9F                     ; 8367  4c 9f 8a
loc_16_836A:
    STX tmp_2C                          ; 836A  86 2c
    STA tmp_19                          ; 836C  85 19
    STA wram_646E                       ; 836E  8d 6e 64
    TAX                                 ; 8371  aa
    LDA $8FF0,X                         ; 8372  bd f0 8f
    AND #$40                            ; 8375  29 40
    BEQ loc_16_8392                     ; 8377  f0 19
    LDA #$4E                            ; 8379  a9 4e
    JSR sub_16_8A9F                     ; 837B  20 9f 8a
    LDA #$0F                            ; 837E  a9 0f
    JSR MenuPrint                       ; 8380  20 33 89
    LDA ram_07DD                        ; 8383  ad dd 07
    BNE loc_16_838D                     ; 8386  d0 05
    LDA #$08                            ; 8388  a9 08
    JMP MenuPrint                       ; 838A  4c 33 89
loc_16_838D:
    LDA #$0D                            ; 838D  a9 0d
    JMP MenuPrint                       ; 838F  4c 33 89
loc_16_8392:
    LDA #$4C                            ; 8392  a9 4c
    JSR sub_16_8A9F                     ; 8394  20 9f 8a
    LDA #$0F                            ; 8397  a9 0f
    JSR MenuPrint                       ; 8399  20 33 89
    LDA ram_07DD                        ; 839C  ad dd 07
    BNE loc_16_83AE                     ; 839F  d0 0d
    LDA #$07                            ; 83A1  a9 07
    JSR MenuPrint                       ; 83A3  20 33 89
    LDA #$0E                            ; 83A6  a9 0e
    JSR MenuPrint                       ; 83A8  20 33 89
    JMP loc_16_83B8                     ; 83AB  4c b8 83
loc_16_83AE:
    LDA #$0B                            ; 83AE  a9 0b
    JSR MenuPrint                       ; 83B0  20 33 89
    LDA #$0E                            ; 83B3  a9 0e
    JSR MenuPrint                       ; 83B5  20 33 89
loc_16_83B8:
    LDA #$A0                            ; 83B8  a9 a0
    JSR sub_16_84DA                     ; 83BA  20 da 84
    BCC loc_16_83C9                     ; 83BD  90 0a
    LDA #$4B                            ; 83BF  a9 4b
    JSR sub_16_8A9F                     ; 83C1  20 9f 8a
    LDA #$0F                            ; 83C4  a9 0f
    JMP MenuPrint                       ; 83C6  4c 33 89
loc_16_83C9:
    LDA #$4C                            ; 83C9  a9 4c
    JSR sub_16_8A9F                     ; 83CB  20 9f 8a
    LDA ram_07DD                        ; 83CE  ad dd 07
    BEQ loc_16_83DB                     ; 83D1  f0 08
    JSR sub_16_8631                     ; 83D3  20 31 86
    LDA #$01                            ; 83D6  a9 01
    JSR sub_16_8613                     ; 83D8  20 13 86
loc_16_83DB:
    LDA wram_6424                       ; 83DB  ad 24 64
    BEQ loc_16_83E2                     ; 83DE  f0 02
    LDA #$04                            ; 83E0  a9 04
loc_16_83E2:
    CLC                                 ; 83E2  18
    ADC tmp_2B                          ; 83E3  65 2b
    PHA                                 ; 83E5  48
    TAX                                 ; 83E6  aa
    LDA $85A6,X                         ; 83E7  bd a6 85
    CLC                                 ; 83EA  18
    ADC inv_select,X                    ; 83EB  7d 28 64
    TAX                                 ; 83EE  aa
    LDA inventory,X                     ; 83EF  bd 30 64
    STA tmp_26                          ; 83F2  85 26
    PLA                                 ; 83F4  68
    TAX                                 ; 83F5  aa
    LDA inv_select,X                    ; 83F6  bd 28 64
    AND #$07                            ; 83F9  29 07
    CMP tmp_2A                          ; 83FB  c5 2a
    BNE loc_16_8423                     ; 83FD  d0 24
    LDA #$FF                            ; 83FF  a9 ff
    STA inv_select,X                    ; 8401  9d 28 64
    LDA #$FF                            ; 8404  a9 ff
    STA tmp_26                          ; 8406  85 26
    JSR sub_16_85F6                     ; 8408  20 f6 85
    TAX                                 ; 840B  aa
    LDA #$00                            ; 840C  a9 00
    STA equip_sword,X                   ; 840E  9d 11 07
    JSR WaitFrame                       ; 8411  20 69 c1
    LDA wram_6427                       ; 8414  ad 27 64
    STA $607F                           ; 8417  8d 7f 60
    JSR sub_16_8A5A                     ; 841A  20 5a 8a
    LDA $607F                           ; 841D  ad 7f 60
    STA wram_6427                       ; 8420  8d 27 64
loc_16_8423:
    LDA #$0F                            ; 8423  a9 0f
    JSR MenuPrint                       ; 8425  20 33 89
    LDA ram_07DD                        ; 8428  ad dd 07
    BNE loc_16_8432                     ; 842B  d0 05
    LDA #$12                            ; 842D  a9 12
    JSR MenuPrint                       ; 842F  20 33 89
loc_16_8432:
    LDX tmp_2C                          ; 8432  a6 2c
    LDA #$FF                            ; 8434  a9 ff
    STA inventory,X                     ; 8436  9d 30 64
    LDA tmp_2A                          ; 8439  a5 2a
    STA tmp_14                          ; 843B  85 14
    LDA tmp_2B                          ; 843D  a5 2b
    STA tmp_15                          ; 843F  85 15
    LDA wram_6427                       ; 8441  ad 27 64
    PHA                                 ; 8444  48
    JSR sub_16_8534                     ; 8445  20 34 85
    JSR sub_16_85AE                     ; 8448  20 ae 85
    LDA frame_ready                     ; 844B  a5 09
    AND #$FE                            ; 844D  29 fe
    STA frame_ready                     ; 844F  85 09
    JSR sub_16_826F                     ; 8451  20 6f 82
    JSR sub_16_871F                     ; 8454  20 1f 87
    PLA                                 ; 8457  68
    STA wram_6427                       ; 8458  8d 27 64
    LDA tmp_14                          ; 845B  a5 14
    STA tmp_2A                          ; 845D  85 2a
    LDA tmp_15                          ; 845F  a5 15
    STA tmp_2B                          ; 8461  85 2b
    JSR sub_16_823A                     ; 8463  20 3a 82
    LDA ram_07DD                        ; 8466  ad dd 07
    BNE loc_16_846C                     ; 8469  d0 01
    RTS                                 ; 846B  60
loc_16_846C:
    LDA #$0F                            ; 846C  a9 0f
    JSR MenuPrint                       ; 846E  20 33 89
    LDA #$0C                            ; 8471  a9 0c
    JSR MenuPrint                       ; 8473  20 33 89
    LDA #$0E                            ; 8476  a9 0e
    JSR MenuPrint                       ; 8478  20 33 89
loc_16_847B:
    LDA #$A0                            ; 847B  a9 a0
    JSR sub_16_84DA                     ; 847D  20 da 84
    BCC loc_16_849E                     ; 8480  90 1c
    LDA #$4C                            ; 8482  a9 4c
    JSR sub_16_8A9F                     ; 8484  20 9f 8a
    LDA #$0F                            ; 8487  a9 0f
    JSR MenuPrint                       ; 8489  20 33 89
    LDA #$2C                            ; 848C  a9 2c
    JSR MenuPrint                       ; 848E  20 33 89
loc_16_8491:
    JSR WaitFrame                       ; 8491  20 69 c1
    JSR ReadPadsMenu                    ; 8494  20 e0 fe
    LDA pad_new                         ; 8497  a5 4b
    BEQ loc_16_8491                     ; 8499  f0 f6
    PLA                                 ; 849B  68
    PLA                                 ; 849C  68
    RTS                                 ; 849D  60
loc_16_849E:
    LDA #$4C                            ; 849E  a9 4c
    JSR sub_16_8A9F                     ; 84A0  20 9f 8a
    LDA #$0F                            ; 84A3  a9 0f
    JSR MenuPrint                       ; 84A5  20 33 89
    JSR sub_16_85E6                     ; 84A8  20 e6 85
    CMP #$FF                            ; 84AB  c9 ff
    BNE loc_16_84B0                     ; 84AD  d0 01
    RTS                                 ; 84AF  60
loc_16_84B0:
    STA tmp_19                          ; 84B0  85 19
    LDA #$06                            ; 84B2  a9 06
    JSR MenuPrint                       ; 84B4  20 33 89
    LDX #$33                            ; 84B7  a2 33
    LDA wram_6424                       ; 84B9  ad 24 64
    BEQ loc_16_84C0                     ; 84BC  f0 02
    LDX #$34                            ; 84BE  a2 34
loc_16_84C0:
    TXA                                 ; 84C0  8a
    JSR MenuPrint                       ; 84C1  20 33 89
    JSR sub_16_85E6                     ; 84C4  20 e6 85
    ASL A                               ; 84C7  0a
    TAX                                 ; 84C8  aa
    LDA $9EC2,X                         ; 84C9  bd c2 9e
    STA shop_price_sel                  ; 84CC  8d 74 64
    LDA $9EC3,X                         ; 84CF  bd c3 9e
    STA shop_price_sel_hi               ; 84D2  8d 75 64
    LDA #$0D                            ; 84D5  a9 0d
    JMP sub_16_8613                     ; 84D7  4c 13 86
sub_16_84DA:
    CMP #$41                            ; 84DA  c9 41
    BNE loc_16_84E0                     ; 84DC  d0 02
    LDA #$49                            ; 84DE  a9 49
loc_16_84E0:
    STA tmp_22                          ; 84E0  85 22
    LDA #$01                            ; 84E2  a9 01
    STA tmp_23                          ; 84E4  85 23
loc_16_84E6:
    JSR WaitFrame                       ; 84E6  20 69 c1
    JSR sub_16_850D                     ; 84E9  20 0d 85
    JSR ReadPadsMenu                    ; 84EC  20 e0 fe
    LDA pad_new                         ; 84EF  a5 4b
    AND #$0F                            ; 84F1  29 0f
    BEQ loc_16_84FC                     ; 84F3  f0 07
    INC tmp_23                          ; 84F5  e6 23
    LDA #$4B                            ; 84F7  a9 4b
    JSR sub_16_8A9F                     ; 84F9  20 9f 8a
loc_16_84FC:
    LDA pad_new                         ; 84FC  a5 4b
    AND #$C0                            ; 84FE  29 c0
    BEQ loc_16_84E6                     ; 8500  f0 e4
    JSR WaitFrame                       ; 8502  20 69 c1
    LDA #$F0                            ; 8505  a9 f0
    STA $02DC                           ; 8507  8d dc 02
    LSR tmp_23                          ; 850A  46 23
    RTS                                 ; 850C  60
sub_16_850D:
    LDA tmp_23                          ; 850D  a5 23
    AND #$01                            ; 850F  29 01
    TAX                                 ; 8511  aa
    LDA $8532,X                         ; 8512  bd 32 85
    STA wram_60FB                       ; 8515  8d fb 60
    LDA tmp_22                          ; 8518  a5 22
    STA wram_60F8                       ; 851A  8d f8 60
    LDA #$00                            ; 851D  a9 00
    STA wram_60F9                       ; 851F  8d f9 60
    LDA #$01                            ; 8522  a9 01
    STA wram_60FA                       ; 8524  8d fa 60
    LDA #$36                            ; 8527  a9 36
    STA tmp_2E                          ; 8529  85 2e
    LDA #$01                            ; 852B  a9 01
    STA tmp_12                          ; 852D  85 12
    JMP MenuSprDraw                     ; 852F  4c d3 86
    .db $B8,$D8                         ; 16:8532
sub_16_8534:
    LDA #$00                            ; 8534  a9 00
    STA tmp_2E                          ; 8536  85 2e
loc_16_8538:
    LDX tmp_2E                          ; 8538  a6 2e
    LDA $859E,X                         ; 853A  bd 9e 85
    STA tmp_2D                          ; 853D  85 2d
    LDA $85A6,X                         ; 853F  bd a6 85
    TAX                                 ; 8542  aa
    LDY #$00                            ; 8543  a0 00
loc_16_8545:
    LDA inventory,X                     ; 8545  bd 30 64
    STA $60E0,Y                         ; 8548  99 e0 60
    INX                                 ; 854B  e8
    INY                                 ; 854C  c8
    DEC tmp_2D                          ; 854D  c6 2d
    BNE loc_16_8545                     ; 854F  d0 f4
loc_16_8551:
    LDX tmp_2E                          ; 8551  a6 2e
    LDA $859E,X                         ; 8553  bd 9e 85
    TAX                                 ; 8556  aa
    LDA #$00                            ; 8557  a9 00
    STA tmp_2F                          ; 8559  85 2f
loc_16_855B:
    LDA wram_60DF,X                     ; 855B  bd df 60
    DEX                                 ; 855E  ca
    BEQ loc_16_8578                     ; 855F  f0 17
    CMP wram_60DF,X                     ; 8561  dd df 60
    BCS loc_16_855B                     ; 8564  b0 f5
    TAY                                 ; 8566  a8
    LDA wram_60DF,X                     ; 8567  bd df 60
    INX                                 ; 856A  e8
    STA wram_60DF,X                     ; 856B  9d df 60
    TYA                                 ; 856E  98
    DEX                                 ; 856F  ca
    STA wram_60DF,X                     ; 8570  9d df 60
    INC tmp_2F                          ; 8573  e6 2f
    JMP loc_16_855B                     ; 8575  4c 5b 85
loc_16_8578:
    LDA tmp_2F                          ; 8578  a5 2f
    BNE loc_16_8551                     ; 857A  d0 d5
    LDX tmp_2E                          ; 857C  a6 2e
    LDA $859E,X                         ; 857E  bd 9e 85
    STA tmp_2D                          ; 8581  85 2d
    LDA $85A6,X                         ; 8583  bd a6 85
    TAX                                 ; 8586  aa
    LDY #$00                            ; 8587  a0 00
loc_16_8589:
    LDA $60E0,Y                         ; 8589  b9 e0 60
    STA inventory,X                     ; 858C  9d 30 64
    INX                                 ; 858F  e8
    INY                                 ; 8590  c8
    DEC tmp_2D                          ; 8591  c6 2d
    BNE loc_16_8589                     ; 8593  d0 f4
    INC tmp_2E                          ; 8595  e6 2e
    LDA #$08                            ; 8597  a9 08
    CMP tmp_2E                          ; 8599  c5 2e
    BNE loc_16_8538                     ; 859B  d0 9b
    RTS                                 ; 859D  60
    .db $04,$04,$04,$04,$08,$08,$08,$08,$00,$04,$08,$0C,$10,$18,$20,$28; 16:859E
sub_16_85AE:
    LDA #$FF                            ; 85AE  a9 ff
    CMP tmp_26                          ; 85B0  c5 26
    BNE loc_16_85B5                     ; 85B2  d0 01
    RTS                                 ; 85B4  60
loc_16_85B5:
    LDY #$00                            ; 85B5  a0 00
    LDA wram_6424                       ; 85B7  ad 24 64
    BEQ loc_16_85BE                     ; 85BA  f0 02
    LDA #$04                            ; 85BC  a9 04
loc_16_85BE:
    CLC                                 ; 85BE  18
    ADC tmp_2B                          ; 85BF  65 2b
    TAX                                 ; 85C1  aa
    LDA $85A6,X                         ; 85C2  bd a6 85
    TAX                                 ; 85C5  aa
loc_16_85C6:
    LDA inventory,X                     ; 85C6  bd 30 64
    INY                                 ; 85C9  c8
    INX                                 ; 85CA  e8
    CMP tmp_26                          ; 85CB  c5 26
    BNE loc_16_85C6                     ; 85CD  d0 f7
    LDA wram_6424                       ; 85CF  ad 24 64
    BEQ loc_16_85D6                     ; 85D2  f0 02
    LDA #$04                            ; 85D4  a9 04
loc_16_85D6:
    CLC                                 ; 85D6  18
    ADC tmp_2B                          ; 85D7  65 2b
    TAX                                 ; 85D9  aa
    DEY                                 ; 85DA  88
    TYA                                 ; 85DB  98
    CMP #$08                            ; 85DC  c9 08
    BMI loc_16_85E2                     ; 85DE  30 02
    LDA #$FF                            ; 85E0  a9 ff
loc_16_85E2:
    STA inv_select,X                    ; 85E2  9d 28 64
    RTS                                 ; 85E5  60
sub_16_85E6:
    LDA wram_6424                       ; 85E6  ad 24 64
    BEQ loc_16_85ED                     ; 85E9  f0 02
    LDA #$10                            ; 85EB  a9 10
loc_16_85ED:
    CLC                                 ; 85ED  18
    ADC wram_6427                       ; 85EE  6d 27 64
    TAX                                 ; 85F1  aa
    LDA inventory,X                     ; 85F2  bd 30 64
    RTS                                 ; 85F5  60
sub_16_85F6:
    LDA wram_6424                       ; 85F6  ad 24 64
    BEQ loc_16_85FD                     ; 85F9  f0 02
    LDA #$04                            ; 85FB  a9 04
loc_16_85FD:
    CLC                                 ; 85FD  18
    ADC tmp_2B                          ; 85FE  65 2b
    TAX                                 ; 8600  aa
    LDA $833A,X                         ; 8601  bd 3a 83
    RTS                                 ; 8604  60
sub_16_8605:
    JSR WaitFrame                       ; 8605  20 69 c1
    LDA #$F0                            ; 8608  a9 f0
    LDX #$00                            ; 860A  a2 00
loc_16_860C:
    STA oam_y,X                         ; 860C  9d 00 02
    INX                                 ; 860F  e8
    BNE loc_16_860C                     ; 8610  d0 fa
    RTS                                 ; 8612  60
sub_16_8613:
    TAY                                 ; 8613  a8
    LDX #$00                            ; 8614  a2 00
loc_16_8616:
    LDA jmp_trampoline,X                ; 8616  b5 10
    STA $60EC,X                         ; 8618  9d ec 60
    INX                                 ; 861B  e8
    CPX #$20                            ; 861C  e0 20
    BNE loc_16_8616                     ; 861E  d0 f6
    TYA                                 ; 8620  98
    JSR FarCall_B26_8E46                ; 8621  20 a9 ff
    LDX #$00                            ; 8624  a2 00
loc_16_8626:
    LDA $60EC,X                         ; 8626  bd ec 60
    STA jmp_trampoline,X                ; 8629  95 10
    INX                                 ; 862B  e8
    CPX #$20                            ; 862C  e0 20
    BNE loc_16_8626                     ; 862E  d0 f6
    RTS                                 ; 8630  60
sub_16_8631:
    JSR sub_16_85E6                     ; 8631  20 e6 85
    ASL A                               ; 8634  0a
    TAX                                 ; 8635  aa
    CLC                                 ; 8636  18
    LDA $9EC2,X                         ; 8637  bd c2 9e
    ADC gold                            ; 863A  6d 02 07
    STA shop_price_sel                  ; 863D  8d 74 64
    LDA $9EC3,X                         ; 8640  bd c3 9e
    ADC gold_hi                         ; 8643  6d 03 07
    STA shop_price_sel_hi               ; 8646  8d 75 64
    BCC loc_16_8653                     ; 8649  90 08
    LDA #$FF                            ; 864B  a9 ff
    STA shop_price_sel                  ; 864D  8d 74 64
    STA shop_price_sel_hi               ; 8650  8d 75 64
loc_16_8653:
    LDA shop_price_sel                  ; 8653  ad 74 64
    STA gold                            ; 8656  8d 02 07
    LDA shop_price_sel_hi               ; 8659  ad 75 64
    STA gold_hi                         ; 865C  8d 03 07
    RTS                                 ; 865F  60
sub_16_8660:
    JSR sub_16_85E6                     ; 8660  20 e6 85
    CMP #$4B                            ; 8663  c9 4b
    BCC loc_16_8668                     ; 8665  90 01
    RTS                                 ; 8667  60
loc_16_8668:
    TAX                                 ; 8668  aa
    LDA $8FA5,X                         ; 8669  bd a5 8f
    STA wram_60F9                       ; 866C  8d f9 60
    LDA tmp_2B                          ; 866F  a5 2b
    ASL A                               ; 8671  0a
    ASL A                               ; 8672  0a
    ASL A                               ; 8673  0a
    ADC tmp_2B                          ; 8674  65 2b
    STA tmp_2E                          ; 8676  85 2e
    LDA $8FF0,X                         ; 8678  bd f0 8f
    TAX                                 ; 867B  aa
    AND #$03                            ; 867C  29 03
    STA wram_60FA                       ; 867E  8d fa 60
    TXA                                 ; 8681  8a
    ASL A                               ; 8682  0a
    BCC loc_16_868F                     ; 8683  90 0a
    LDA #$03                            ; 8685  a9 03
    STA tmp_12                          ; 8687  85 12
    JSR sub_16_86AF                     ; 8689  20 af 86
    JMP MenuSprDraw                     ; 868C  4c d3 86
loc_16_868F:
    LDA #$02                            ; 868F  a9 02
    STA tmp_12                          ; 8691  85 12
    JSR sub_16_86AF                     ; 8693  20 af 86
    JMP MenuSprDraw                     ; 8696  4c d3 86
sub_16_8699:
    LDA #$00                            ; 8699  a9 00
    STA tmp_12                          ; 869B  85 12
    STA wram_60F9                       ; 869D  8d f9 60
    LDA #$03                            ; 86A0  a9 03
    STA wram_60FA                       ; 86A2  8d fa 60
    LDA #$38                            ; 86A5  a9 38
    STA tmp_2E                          ; 86A7  85 2e
    JSR sub_16_86AF                     ; 86A9  20 af 86
    JMP MenuSprDraw                     ; 86AC  4c d3 86
sub_16_86AF:
    LDA wram_6424                       ; 86AF  ad 24 64
    BEQ loc_16_86B6                     ; 86B2  f0 02
    LDA #$10                            ; 86B4  a9 10
loc_16_86B6:
    CLC                                 ; 86B6  18
    ADC wram_6427                       ; 86B7  6d 27 64
    ASL A                               ; 86BA  0a
    TAX                                 ; 86BB  aa
    LDA $8B73,X                         ; 86BC  bd 73 8b
    CLC                                 ; 86BF  18
    ADC #$30                            ; 86C0  69 30
    STA wram_60F8                       ; 86C2  8d f8 60
    LDA $8B74,X                         ; 86C5  bd 74 8b
    CLC                                 ; 86C8  18
    ADC #$38                            ; 86C9  69 38
    STA wram_60FB                       ; 86CB  8d fb 60
    LDA inventory,X                     ; 86CE  bd 30 64
    TAX                                 ; 86D1  aa
    RTS                                 ; 86D2  60
MenuSprDraw:  ; s72: $12=shape (MenuSprShapes) $2E=OAM slot-1; offsets wram_60F8 y / 60F9 tile / 60FA attr OR / 60FB x
    LDA tmp_12                          ; 86D3  a5 12
    ASL A                               ; 86D5  0a
    TAX                                 ; 86D6  aa
    LDA MenuSprShapes,X                 ; 86D7  bd 1f 8b
    STA tmp_12                          ; 86DA  85 12
    LDA $8B20,X                         ; 86DC  bd 20 8b
    STA tmp_13                          ; 86DF  85 13
    LDY #$00                            ; 86E1  a0 00
    LDA (tmp_12),Y                      ; 86E3  b1 12
    STA tmp_2F                          ; 86E5  85 2f
    INY                                 ; 86E7  c8
    LDA tmp_2E                          ; 86E8  a5 2e
    ASL A                               ; 86EA  0a
    ASL A                               ; 86EB  0a
    ADC #$04                            ; 86EC  69 04
    TAX                                 ; 86EE  aa
loc_16_86EF:
    CLC                                 ; 86EF  18
    LDA (tmp_12),Y                      ; 86F0  b1 12
    ADC wram_60F8                       ; 86F2  6d f8 60
    STA oam_y,X                         ; 86F5  9d 00 02
    INX                                 ; 86F8  e8
    INY                                 ; 86F9  c8
    CLC                                 ; 86FA  18
    LDA (tmp_12),Y                      ; 86FB  b1 12
    ADC wram_60F9                       ; 86FD  6d f9 60
    STA oam_y,X                         ; 8700  9d 00 02
    INX                                 ; 8703  e8
    INY                                 ; 8704  c8
    LDA (tmp_12),Y                      ; 8705  b1 12
    ORA wram_60FA                       ; 8707  0d fa 60
    STA oam_y,X                         ; 870A  9d 00 02
    INX                                 ; 870D  e8
    INY                                 ; 870E  c8
    CLC                                 ; 870F  18
    LDA (tmp_12),Y                      ; 8710  b1 12
    ADC wram_60FB                       ; 8712  6d fb 60
    STA oam_y,X                         ; 8715  9d 00 02
    INX                                 ; 8718  e8
    INY                                 ; 8719  c8
    DEC tmp_2F                          ; 871A  c6 2f
    BNE loc_16_86EF                     ; 871C  d0 d1
    RTS                                 ; 871E  60
sub_16_871F:
    JSR WaitPpuQueue                    ; 871F  20 76 c6
    LDX #$00                            ; 8722  a2 00
loc_16_8724:
    LDA $8AFD,X                         ; 8724  bd fd 8a
    STA tmp_20,X                        ; 8727  95 20
    INX                                 ; 8729  e8
    CPX #$10                            ; 872A  e0 10
    BNE loc_16_8724                     ; 872C  d0 f6
    LDY #$00                            ; 872E  a0 00
loc_16_8730:
    LDX #$00                            ; 8730  a2 00
loc_16_8732:
    LDA $8BD3,X                         ; 8732  bd d3 8b
    STA wram_buf,Y                      ; 8735  99 00 60
    INX                                 ; 8738  e8
    INY                                 ; 8739  c8
    CPX #$20                            ; 873A  e0 20
    BNE loc_16_8732                     ; 873C  d0 f4
    CPY #$60                            ; 873E  c0 60
    BMI loc_16_8730                     ; 8740  30 ee
    LDA #$00                            ; 8742  a9 00
    STA tmp_29                          ; 8744  85 29
    STA tmp_28                          ; 8746  85 28
    STA tmp_27                          ; 8748  85 27
loc_16_874A:
    JSR WaitPpuQueue                    ; 874A  20 76 c6
loc_16_874D:
    LDA #$00                            ; 874D  a9 00
    STA frame_ready                     ; 874F  85 09
    LDA wram_6424                       ; 8751  ad 24 64
    BNE loc_16_875C                     ; 8754  d0 06
    LDA tmp_27                          ; 8756  a5 27
    TAX                                 ; 8758  aa
    JMP loc_16_8762                     ; 8759  4c 62 87
loc_16_875C:
    LDA tmp_27                          ; 875C  a5 27
    CLC                                 ; 875E  18
    ADC #$10                            ; 875F  69 10
    TAX                                 ; 8761  aa
loc_16_8762:
    LDA inventory,X                     ; 8762  bd 30 64
    CMP #$4B                            ; 8765  c9 4b
    BCC loc_16_876F                     ; 8767  90 06
    JSR sub_16_87EA                     ; 8769  20 ea 87
    JMP loc_16_8776                     ; 876C  4c 76 87
loc_16_876F:
    TAX                                 ; 876F  aa
    LDA $8F5A,X                         ; 8770  bd 5a 8f
    JSR sub_16_87B8                     ; 8773  20 b8 87
loc_16_8776:
    INC tmp_27                          ; 8776  e6 27
    INC tmp_29                          ; 8778  e6 29
    LDA tmp_29                          ; 877A  a5 29
    CMP wram_6426                       ; 877C  cd 26 64
    BMI loc_16_874D                     ; 877F  30 cc
    JSR sub_16_8795                     ; 8781  20 95 87
    LDA #$00                            ; 8784  a9 00
    STA tmp_29                          ; 8786  85 29
    LDA tmp_28                          ; 8788  a5 28
    CMP #$04                            ; 878A  c9 04
    BMI loc_16_874A                     ; 878C  30 bc
    LDA irq_mode                        ; 878E  a5 51
    AND #$7F                            ; 8790  29 7f
    STA irq_mode                        ; 8792  85 51
    RTS                                 ; 8794  60
sub_16_8795:
    LDA #$00                            ; 8795  a9 00
    STA tmp_29                          ; 8797  85 29
    JSR WaitPpuQueue                    ; 8799  20 76 c6
    LDA irq_mode                        ; 879C  a5 51
    ORA #$80                            ; 879E  09 80
    STA irq_mode                        ; 87A0  85 51
    JSR MenuFlush                       ; 87A2  20 b0 8a
    JSR WaitPpuQueue                    ; 87A5  20 76 c6
    CLC                                 ; 87A8  18
    LDA tmp_20                          ; 87A9  a5 20
    ADC #$60                            ; 87AB  69 60
    STA tmp_20                          ; 87AD  85 20
    LDA #$00                            ; 87AF  a9 00
    ADC tmp_21                          ; 87B1  65 21
    STA tmp_21                          ; 87B3  85 21
    INC tmp_28                          ; 87B5  e6 28
    RTS                                 ; 87B7  60
sub_16_87B8:
    LDY tmp_29                          ; 87B8  a4 29
    LDX $8BF3,Y                         ; 87BA  be f3 8b
    TAY                                 ; 87BD  a8
    STA wram_buf,X                      ; 87BE  9d 00 60
    INY                                 ; 87C1  c8
    TYA                                 ; 87C2  98
    STA wram_6001,X                     ; 87C3  9d 01 60
    INY                                 ; 87C6  c8
    TYA                                 ; 87C7  98
    STA wram_6002,X                     ; 87C8  9d 02 60
    INY                                 ; 87CB  c8
    TYA                                 ; 87CC  98
    STA wram_6020,X                     ; 87CD  9d 20 60
    INY                                 ; 87D0  c8
    TYA                                 ; 87D1  98
    STA wram_6021,X                     ; 87D2  9d 21 60
    INY                                 ; 87D5  c8
    TYA                                 ; 87D6  98
    STA $6022,X                         ; 87D7  9d 22 60
    INY                                 ; 87DA  c8
    TYA                                 ; 87DB  98
    STA wram_6040,X                     ; 87DC  9d 40 60
    INY                                 ; 87DF  c8
    TYA                                 ; 87E0  98
    STA wram_6041,X                     ; 87E1  9d 41 60
    INY                                 ; 87E4  c8
    TYA                                 ; 87E5  98
    STA $6042,X                         ; 87E6  9d 42 60
    RTS                                 ; 87E9  60
sub_16_87EA:
    LDY tmp_29                          ; 87EA  a4 29
    LDX $8BF3,Y                         ; 87EC  be f3 8b
    LDY #$F5                            ; 87EF  a0 f5
    TYA                                 ; 87F1  98
    STA wram_buf,X                      ; 87F2  9d 00 60
    INY                                 ; 87F5  c8
    TYA                                 ; 87F6  98
    STA wram_6001,X                     ; 87F7  9d 01 60
    INY                                 ; 87FA  c8
    TYA                                 ; 87FB  98
    STA wram_6002,X                     ; 87FC  9d 02 60
    INY                                 ; 87FF  c8
    TYA                                 ; 8800  98
    STA wram_6020,X                     ; 8801  9d 20 60
    LDA #$20                            ; 8804  a9 20
    STA wram_6021,X                     ; 8806  9d 21 60
    INY                                 ; 8809  c8
    TYA                                 ; 880A  98
    STA $6022,X                         ; 880B  9d 22 60
    INY                                 ; 880E  c8
    TYA                                 ; 880F  98
    STA wram_6040,X                     ; 8810  9d 40 60
    INY                                 ; 8813  c8
    TYA                                 ; 8814  98
    STA wram_6041,X                     ; 8815  9d 41 60
    INY                                 ; 8818  c8
    TYA                                 ; 8819  98
    STA $6042,X                         ; 881A  9d 42 60
    RTS                                 ; 881D  60
sub_16_881E:
    LDA irq_mode                        ; 881E  a5 51
    PHA                                 ; 8820  48
    ORA #$80                            ; 8821  09 80
    STA irq_mode                        ; 8823  85 51
    LDX #$00                            ; 8825  a2 00
    JSR sub_16_8864                     ; 8827  20 64 88
    LDX #$06                            ; 882A  a2 06
    JSR sub_16_8864                     ; 882C  20 64 88
    LDA #$09                            ; 882F  a9 09
    STA tmp_19                          ; 8831  85 19
loc_16_8833:
    LDX #$09                            ; 8833  a2 09
    JSR sub_16_8864                     ; 8835  20 64 88
    DEC tmp_19                          ; 8838  c6 19
    BNE loc_16_8833                     ; 883A  d0 f7
    LDX #$0C                            ; 883C  a2 0c
    JSR sub_16_8864                     ; 883E  20 64 88
    JSR WaitPpuQueue                    ; 8841  20 76 c6
    LDX #$41                            ; 8844  a2 41
    LDA #$FF                            ; 8846  a9 ff
loc_16_8848:
    STA wram_buf,X                      ; 8848  9d 00 60
    DEX                                 ; 884B  ca
    BNE loc_16_8848                     ; 884C  d0 fa
    LDA #$2B                            ; 884E  a9 2b
    STA tmp_21                          ; 8850  85 21
    LDA #$C0                            ; 8852  a9 c0
    STA tmp_20                          ; 8854  85 20
    LDA #$01                            ; 8856  a9 01
    STA tmp_25                          ; 8858  85 25
    JSR MenuFlush                       ; 885A  20 b0 8a
    JSR WaitPpuQueue                    ; 885D  20 76 c6
    PLA                                 ; 8860  68
    STA irq_mode                        ; 8861  85 51
    RTS                                 ; 8863  60
sub_16_8864:
    JSR WaitPpuQueue                    ; 8864  20 76 c6
    LDY #$00                            ; 8867  a0 00
    LDA #$02                            ; 8869  a9 02
    STA tmp_2F                          ; 886B  85 2f
loc_16_886D:
    LDA #$FE                            ; 886D  a9 fe
    STA wram_buf,Y                      ; 886F  99 00 60
    INY                                 ; 8872  c8
    LDA $8B0D,X                         ; 8873  bd 0d 8b
    STA wram_buf,Y                      ; 8876  99 00 60
    INX                                 ; 8879  e8
    INY                                 ; 887A  c8
    LDA #$1D                            ; 887B  a9 1d
    STA tmp_2E                          ; 887D  85 2e
loc_16_887F:
    LDA $8B0D,X                         ; 887F  bd 0d 8b
    STA wram_buf,Y                      ; 8882  99 00 60
    INY                                 ; 8885  c8
    DEC tmp_2E                          ; 8886  c6 2e
    BNE loc_16_887F                     ; 8888  d0 f5
    INX                                 ; 888A  e8
    LDA $8B0D,X                         ; 888B  bd 0d 8b
    STA wram_buf,Y                      ; 888E  99 00 60
    INX                                 ; 8891  e8
    INY                                 ; 8892  c8
    DEC tmp_2F                          ; 8893  c6 2f
    BNE loc_16_886D                     ; 8895  d0 d6
    JSR MenuFlush                       ; 8897  20 b0 8a
    CLC                                 ; 889A  18
    LDA #$40                            ; 889B  a9 40
    ADC tmp_20                          ; 889D  65 20
    STA tmp_20                          ; 889F  85 20
    LDA #$00                            ; 88A1  a9 00
    ADC tmp_21                          ; 88A3  65 21
    STA tmp_21                          ; 88A5  85 21
    RTS                                 ; 88A7  60
sub_16_88A8:
    ASL tmp_26                          ; 88A8  06 26
    ASL tmp_26                          ; 88AA  06 26
    ASL tmp_26                          ; 88AC  06 26
    LSR tmp_27                          ; 88AE  46 27
    ROR tmp_26                          ; 88B0  66 26
    LSR tmp_27                          ; 88B2  46 27
    ROR tmp_26                          ; 88B4  66 26
    LSR tmp_27                          ; 88B6  46 27
    ROR tmp_26                          ; 88B8  66 26
    LDA tmp_26                          ; 88BA  a5 26
    STA tmp_20                          ; 88BC  85 20
    LDA tmp_27                          ; 88BE  a5 27
    CLC                                 ; 88C0  18
    ADC #$28                            ; 88C1  69 28
    STA tmp_21                          ; 88C3  85 21
    LDA #$03                            ; 88C5  a9 03
    STA tmp_24                          ; 88C7  85 24
    LDA #$00                            ; 88C9  a9 00
    STA tmp_25                          ; 88CB  85 25
    LDA tmp_29                          ; 88CD  a5 29
    CMP #$FF                            ; 88CF  c9 ff
    BEQ loc_16_88FB                     ; 88D1  f0 28
    JSR sub_16_88D9                     ; 88D3  20 d9 88
    JSR sub_16_88D9                     ; 88D6  20 d9 88
sub_16_88D9:
    JSR WaitPpuQueue                    ; 88D9  20 76 c6
    LDX #$00                            ; 88DC  a2 00
loc_16_88DE:
    LDA tmp_29                          ; 88DE  a5 29
    STA wram_buf,X                      ; 88E0  9d 00 60
    INC tmp_29                          ; 88E3  e6 29
    INX                                 ; 88E5  e8
    CPX #$03                            ; 88E6  e0 03
    BNE loc_16_88DE                     ; 88E8  d0 f4
    JSR MenuFlush                       ; 88EA  20 b0 8a
    CLC                                 ; 88ED  18
    LDA #$20                            ; 88EE  a9 20
    ADC tmp_20                          ; 88F0  65 20
    STA tmp_20                          ; 88F2  85 20
    LDA #$00                            ; 88F4  a9 00
    ADC tmp_21                          ; 88F6  65 21
    STA tmp_21                          ; 88F8  85 21
    RTS                                 ; 88FA  60
loc_16_88FB:
    LDX #$00                            ; 88FB  a2 00
    STX tmp_29                          ; 88FD  86 29
    JSR sub_16_8905                     ; 88FF  20 05 89
    JSR sub_16_8905                     ; 8902  20 05 89
sub_16_8905:
    JSR WaitPpuQueue                    ; 8905  20 76 c6
    LDX #$00                            ; 8908  a2 00
loc_16_890A:
    LDY tmp_29                          ; 890A  a4 29
    LDA $892A,Y                         ; 890C  b9 2a 89
    STA wram_buf,X                      ; 890F  9d 00 60
    INC tmp_29                          ; 8912  e6 29
    INX                                 ; 8914  e8
    CPX #$03                            ; 8915  e0 03
    BNE loc_16_890A                     ; 8917  d0 f1
    JSR MenuFlush                       ; 8919  20 b0 8a
    CLC                                 ; 891C  18
    LDA #$20                            ; 891D  a9 20
    ADC tmp_20                          ; 891F  65 20
    STA tmp_20                          ; 8921  85 20
    LDA #$00                            ; 8923  a9 00
    ADC tmp_21                          ; 8925  65 21
    STA tmp_21                          ; 8927  85 21
    RTS                                 ; 8929  60
    .db $F5,$F6,$F7,$F8,$20,$F9,$FA,$FB,$FC; 16:892A
MenuPrint:  ; bank16 menu string printer: A=id -> record 16:8C1B (PPU lo/hi + ptr); escapes FF end FE name FD level FC item
    STA tmp_17                          ; 8933  85 17
    JSR WaitPpuQueue                    ; 8935  20 76 c6
    LDA #$00                            ; 8938  a9 00
    STA tmp_23                          ; 893A  85 23
    STA tmp_22                          ; 893C  85 22
    STA tmp_24                          ; 893E  85 24
    STA tmp_25                          ; 8940  85 25
    STA tmp_28                          ; 8942  85 28
    LDA tmp_17                          ; 8944  a5 17
    ASL A                               ; 8946  0a
    ASL A                               ; 8947  0a
    TAX                                 ; 8948  aa
    LDA MenuStrings,X                   ; 8949  bd 1b 8c
    STA tmp_20                          ; 894C  85 20
    LDA $8C1C,X                         ; 894E  bd 1c 8c
    STA tmp_21                          ; 8951  85 21
    LDA $8C1D,X                         ; 8953  bd 1d 8c
    STA jmp_trampoline                  ; 8956  85 10
    LDA $8C1E,X                         ; 8958  bd 1e 8c
    STA tmp_11                          ; 895B  85 11
    LDY #$00                            ; 895D  a0 00
loc_16_895F:
    LDA (jmp_trampoline),Y              ; 895F  b1 10
    STA tmp_18                          ; 8961  85 18
    CMP #$FF                            ; 8963  c9 ff
    BNE loc_16_896A                     ; 8965  d0 03
    JMP loc_16_898E                     ; 8967  4c 8e 89
loc_16_896A:
    CMP #$FE                            ; 896A  c9 fe
    BNE loc_16_8974                     ; 896C  d0 06
    JSR sub_16_8994                     ; 896E  20 94 89
    JMP loc_16_895F                     ; 8971  4c 5f 89
loc_16_8974:
    CMP #$FD                            ; 8974  c9 fd
    BNE loc_16_897E                     ; 8976  d0 06
    JSR sub_16_89B8                     ; 8978  20 b8 89
    JMP loc_16_895F                     ; 897B  4c 5f 89
loc_16_897E:
    CMP #$FC                            ; 897E  c9 fc
    BNE loc_16_8988                     ; 8980  d0 06
    JSR sub_16_8A0D                     ; 8982  20 0d 8a
    JMP loc_16_895F                     ; 8985  4c 5f 89
loc_16_8988:
    JSR MenuPutc                        ; 8988  20 02 8a
    JMP loc_16_895F                     ; 898B  4c 5f 89
loc_16_898E:
    JSR WaitPpuQueue                    ; 898E  20 76 c6
    JMP MenuFlush                       ; 8991  4c b0 8a
sub_16_8994:
    INY                                 ; 8994  c8
    STY save_reset_flag                 ; 8995  8c 01 70
    LDA (jmp_trampoline),Y              ; 8998  b1 10
    AND #$01                            ; 899A  29 01
    TAX                                 ; 899C  aa
    LDA $89B6,X                         ; 899D  bd b6 89
    STA tmp_13                          ; 89A0  85 13
    LDA #$18                            ; 89A2  a9 18
    STA tmp_12                          ; 89A4  85 12
    LDY #$00                            ; 89A6  a0 00
loc_16_89A8:
    LDA (tmp_12),Y                      ; 89A8  b1 12
    JSR MenuPutc                        ; 89AA  20 02 8a
    CPY #$06                            ; 89AD  c0 06
    BNE loc_16_89A8                     ; 89AF  d0 f7
    LDY save_reset_flag                 ; 89B1  ac 01 70
    INY                                 ; 89B4  c8
    RTS                                 ; 89B5  60
    .db $71,$77                         ; 16:89B6
sub_16_89B8:
    INY                                 ; 89B8  c8
    STY save_reset_flag                 ; 89B9  8c 01 70
    LDA (jmp_trampoline),Y              ; 89BC  b1 10
    BNE loc_16_89C6                     ; 89BE  d0 06
    LDA $71F7                           ; 89C0  ad f7 71
    JMP loc_16_89C9                     ; 89C3  4c c9 89
loc_16_89C6:
    LDA $77F7                           ; 89C6  ad f7 77
loc_16_89C9:
    ASL A                               ; 89C9  0a
    TAY                                 ; 89CA  a8
    LDA MenuLevelDigits,Y               ; 89CB  b9 e0 89
    STA tmp_18                          ; 89CE  85 18
    JSR MenuPutc                        ; 89D0  20 02 8a
    LDA MenuLevelDigits,Y               ; 89D3  b9 e0 89
    STA tmp_18                          ; 89D6  85 18
    JSR MenuPutc                        ; 89D8  20 02 8a
    LDY save_reset_flag                 ; 89DB  ac 01 70
    INY                                 ; 89DE  c8
    RTS                                 ; 89DF  60
MenuLevelDigits:  ; 17 two-char pairs 00..16 for FD escape
    .db $30,$30,$30,$31,$30,$32,$30,$33,$30,$34,$30,$35,$30,$36,$30,$37; 16:89E0
    .db $30,$38,$30,$39,$31,$30,$31,$31,$31,$32,$31,$33,$31,$34,$31,$35; 16:89F0
    .db $31,$36                         ; 16:8A00
MenuPutc:  ; store A to $6000[$28]; INY; $24++ $28++
    LDX tmp_28                          ; 8A02  a6 28
    STA wram_buf,X                      ; 8A04  9d 00 60
    INY                                 ; 8A07  c8
    INC tmp_24                          ; 8A08  e6 24
    INC tmp_28                          ; 8A0A  e6 28
    RTS                                 ; 8A0C  60
sub_16_8A0D:
    INY                                 ; 8A0D  c8
    TYA                                 ; 8A0E  98
    PHA                                 ; 8A0F  48
    LDA tmp_19                          ; 8A10  a5 19
    CMP #$FF                            ; 8A12  c9 ff
    BNE loc_16_8A18                     ; 8A14  d0 02
    LDA #$49                            ; 8A16  a9 49
loc_16_8A18:
    ASL A                               ; 8A18  0a
    TAX                                 ; 8A19  aa
    LDA MenuItemNames,X                 ; 8A1A  bd 86 90
    STA tmp_12                          ; 8A1D  85 12
    LDA $9087,X                         ; 8A1F  bd 87 90
    STA tmp_13                          ; 8A22  85 13
    LDY #$00                            ; 8A24  a0 00
loc_16_8A26:
    LDA (tmp_12),Y                      ; 8A26  b1 12
    STA tmp_18                          ; 8A28  85 18
    CMP #$FF                            ; 8A2A  c9 ff
    BEQ loc_16_8A34                     ; 8A2C  f0 06
    JSR MenuPutc                        ; 8A2E  20 02 8a
    JMP loc_16_8A26                     ; 8A31  4c 26 8a
loc_16_8A34:
    PLA                                 ; 8A34  68
    TAY                                 ; 8A35  a8
    RTS                                 ; 8A36  60
    .db $A5,$2A,$0A,$0A,$0A,$8D,$FB,$60,$A5,$2B,$0A,$0A,$0A,$8D,$F8,$60; 16:8A37
    .db $20,$E6,$85,$AA,$BD,$A5,$8F,$8D,$F9,$60,$BD,$F0,$8F,$29,$03,$8D; 16:8A47
    .db $FA,$60,$60                     ; 16:8A57
sub_16_8A5A:
    LDA wram_6424                       ; 8A5A  ad 24 64
    BNE loc_16_8A61                     ; 8A5D  d0 02
    LDA #$10                            ; 8A5F  a9 10
loc_16_8A61:
    CLC                                 ; 8A61  18
    ADC wram_6427                       ; 8A62  6d 27 64
    ASL A                               ; 8A65  0a
    LDA $8B73,X                         ; 8A66  bd 73 8b
    CLC                                 ; 8A69  18
    ADC tmp_2B                          ; 8A6A  65 2b
    STA wram_60F8                       ; 8A6C  8d f8 60
    LDA $8B74,X                         ; 8A6F  bd 74 8b
    CLC                                 ; 8A72  18
    ADC tmp_2A                          ; 8A73  65 2a
    STA wram_60FB                       ; 8A75  8d fb 60
    LDA tmp_2B                          ; 8A78  a5 2b
    ASL A                               ; 8A7A  0a
    ASL A                               ; 8A7B  0a
    ASL A                               ; 8A7C  0a
    ADC tmp_2B                          ; 8A7D  65 2b
    ASL A                               ; 8A7F  0a
    ASL A                               ; 8A80  0a
    ADC #$04                            ; 8A81  69 04
    TAX                                 ; 8A83  aa
    JSR WaitFrame                       ; 8A84  20 69 c1
    LDA #$F8                            ; 8A87  a9 f8
    LDY #$09                            ; 8A89  a0 09
loc_16_8A8B:
    STA oam_y,X                         ; 8A8B  9d 00 02
    INX                                 ; 8A8E  e8
    STA oam_y,X                         ; 8A8F  9d 00 02
    INX                                 ; 8A92  e8
    STA oam_y,X                         ; 8A93  9d 00 02
    INX                                 ; 8A96  e8
    STA oam_y,X                         ; 8A97  9d 00 02
    INX                                 ; 8A9A  e8
    DEY                                 ; 8A9B  88
    BNE loc_16_8A8B                     ; 8A9C  d0 ed
    RTS                                 ; 8A9E  60
sub_16_8A9F:
    STA snd_queue                       ; 8A9F  8d 03 01
    LDA #$01                            ; 8AA2  a9 01
    STA snd_queue_lock                  ; 8AA4  8d 01 01
    STA snd_queue_count                 ; 8AA7  8d 00 01
    LDA #$00                            ; 8AAA  a9 00
    STA snd_queue_lock                  ; 8AAC  8d 01 01
    RTS                                 ; 8AAF  60
MenuFlush:  ; queue ($21 $20 $24 $25) in PPU ring $6200; wait if rendering on
    JSR sub_16_8AE5                     ; 8AB0  20 e5 8a
    LDX tmp_0B                          ; 8AB3  a6 0b
    LDA tmp_21                          ; 8AB5  a5 21
    STA ppu_queue,X                     ; 8AB7  9d 00 62
    LDA tmp_20                          ; 8ABA  a5 20
    STA ppu_queue_1,X                   ; 8ABC  9d 01 62
    LDA tmp_24                          ; 8ABF  a5 24
    STA ppu_queue_2,X                   ; 8AC1  9d 02 62
    LDA tmp_25                          ; 8AC4  a5 25
    STA ppu_queue_3,X                   ; 8AC6  9d 03 62
    TXA                                 ; 8AC9  8a
    CLC                                 ; 8ACA  18
    ADC #$04                            ; 8ACB  69 04
    AND #$1F                            ; 8ACD  29 1f
    STA tmp_0B                          ; 8ACF  85 0b
    LDA ppumask_shadow                  ; 8AD1  a5 01
    AND #$18                            ; 8AD3  29 18
    BNE loc_16_8ADA                     ; 8AD5  d0 03
    JSR PpuQueueFlush                   ; 8AD7  20 7d c6
loc_16_8ADA:
    JMP loc_16_8ADD                     ; 8ADA  4c dd 8a
loc_16_8ADD:
    LDA ppuctrl_shadow                  ; 8ADD  a5 00
    ORA #$80                            ; 8ADF  09 80
    STA PPUCTRL                         ; 8AE1  8d 00 20
    RTS                                 ; 8AE4  60
sub_16_8AE5:
    LDA ppuctrl_shadow                  ; 8AE5  a5 00
    AND #$7F                            ; 8AE7  29 7f
    STA PPUCTRL                         ; 8AE9  8d 00 20
    RTS                                 ; 8AEC  60
    .db $00,$28,$40,$00,$40,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 16:8AED
    .db $C0,$28,$00,$00,$60,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 16:8AFD
    .db $FF,$FF,$FF,$FF,$FF,$FF,$18,$19,$1A,$1E,$20,$1F,$1E,$20,$1F,$1B; 16:8B0D
    .db $1C,$1D                         ; 16:8B1D
MenuSprShapes:  ; s72 (was MenuPtrTable_8B1F): .dw[4] menu sprite shapes (count + n x y/tile/attr/x) 8B27-8B5E; 0=frame corners 1=arrow 2=2x2 icon 3=4-tile strip
    .db $27,$8B,$38,$8B,$3D,$8B,$4E,$8B,$04,$FF,$FF,$00,$00,$0F,$FF,$80; 16:8B1F
    .db $00,$FF,$FF,$40,$10,$0F,$FF,$C0,$10,$01,$FF,$FE,$00,$00,$04,$03; 16:8B2F
    .db $00,$00,$04,$03,$01,$00,$0C,$0B,$02,$00,$04,$0B,$03,$00,$0C,$09; 16:8B3F
    .db $FF,$00,$00,$00,$FF,$01,$00,$08,$FF,$02,$00,$10,$07,$03,$00,$00; 16:8B4F
    .db $07,$04,$00,$08,$07,$05,$00,$10,$0F,$06,$00,$00,$0F,$07,$00,$08; 16:8B5F
    .db $0F,$08,$00,$10,$00,$00,$00,$18,$00,$30,$00,$48,$18,$00,$18,$18; 16:8B6F
    .db $18,$30,$18,$48,$30,$00,$30,$18,$30,$30,$30,$48,$48,$00,$48,$18; 16:8B7F
    .db $48,$30,$48,$48,$00,$00,$00,$18,$00,$30,$00,$48,$00,$60,$00,$78; 16:8B8F
    .db $00,$90,$00,$A8,$18,$00,$18,$18,$18,$30,$18,$48,$18,$60,$18,$78; 16:8B9F
    .db $18,$90,$18,$A8,$30,$00,$30,$18,$30,$30,$30,$48,$30,$60,$30,$78; 16:8BAF
    .db $30,$90,$30,$A8,$48,$00,$48,$18,$48,$30,$48,$48,$48,$60,$48,$78; 16:8BBF
    .db $48,$90,$48,$A8,$FF,$1E,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 16:8BCF
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 16:8BDF
    .db $20,$20,$20,$1F,$07,$0A,$0D,$10,$13,$16,$19,$1C,$0F,$20,$19,$03; 16:8BEF
    .db $0F,$20,$27,$16,$0F,$20,$2C,$12,$0F,$20,$37,$0C,$0F,$20,$2A,$03; 16:8BFF
    .db $0F,$20,$27,$05,$0F,$20,$2C,$12,$0F,$20,$28,$00; 16:8C0F
MenuStrings:  ; 59 x 4-byte records: PPU addr lo/hi + string ptr (bank 16)
    .db $E3,$28,$07,$8D,$43,$29,$0B,$8D,$A3,$29,$0F,$8D,$03,$2A,$13,$8D; 16:8C1B
    .db $E3,$28,$17,$8D,$03,$2A,$1B,$8D,$6A,$2A,$1F,$8D,$65,$2A,$21,$8D; 16:8C2B
    .db $67,$2A,$36,$8D,$65,$2A,$47,$8D,$65,$2A,$47,$8D,$67,$2A,$54,$8D; 16:8C3B
    .db $67,$2A,$5C,$8D,$67,$2A,$6B,$8D,$98,$2A,$BA,$8E,$63,$2A,$C1,$8E; 16:8C4B
    .db $A4,$28,$7D,$8D,$44,$2A,$7F,$8D,$60,$28,$FD,$8D,$83,$28,$9F,$8D; 16:8C5B
    .db $68,$29,$81,$8D,$C8,$29,$8F,$8D,$67,$2A,$20,$8F,$A3,$28,$9F,$8D; 16:8C6B
    .db $43,$28,$AB,$8D,$43,$28,$B6,$8D,$43,$28,$C0,$8D,$43,$28,$C4,$8D; 16:8C7B
    .db $E5,$28,$CE,$8D,$E4,$28,$1F,$8D,$E5,$28,$EE,$8D,$E5,$28,$5C,$8D; 16:8C8B
    .db $85,$28,$FF,$8D,$E5,$28,$12,$8E,$E5,$28,$2D,$8E,$85,$28,$47,$8E; 16:8C9B
    .db $E5,$28,$62,$8E,$85,$28,$75,$8E,$E5,$28,$FF,$8D,$85,$28,$8A,$8E; 16:8CAB
    .db $C3,$28,$C1,$8E,$18,$29,$BA,$8E,$E3,$28,$C1,$8E,$83,$28,$C1,$8E; 16:8CBB
    .db $67,$2A,$FF,$8D,$6C,$2A,$1F,$8D,$E3,$28,$49,$8F,$85,$28,$2D,$8E; 16:8CCB
    .db $E7,$28,$5C,$8D,$67,$2A,$5C,$8D,$F8,$28,$FD,$8D,$98,$2A,$FD,$8D; 16:8CDB
    .db $98,$2A,$88,$8E,$44,$2A,$9D,$8D,$87,$2A,$00,$8F,$87,$2A,$10,$8F; 16:8CEB
    .db $28,$2A,$81,$8D,$28,$2A,$2D,$8F,$28,$2A,$3B,$8F,$0A,$0B,$0C,$FF; 16:8CFB
    .db $7B,$11,$12,$FF,$0D,$0E,$0F,$FF,$13,$14,$15,$FF,$16,$17,$5E,$FF; 16:8D0B
    .db $23,$25,$28,$FF,$FC,$FF,$44,$6F,$20,$79,$6F,$75,$20,$77,$69,$73; 16:8D1B
    .db $68,$20,$74,$6F,$20,$64,$72,$6F,$70,$3F,$FF,$59,$6F,$75,$20,$63; 16:8D2B
    .db $61,$6E,$6E,$6F,$74,$20,$64,$72,$6F,$70,$21,$FF,$4E,$6F,$77,$20; 16:8D3B
    .db $75,$73,$69,$6E,$67,$20,$FC,$2E,$FF,$53,$65,$6C,$6C,$20,$FC,$3F; 16:8D4B
    .db $FF,$41,$6E,$79,$74,$68,$69,$6E,$67,$20,$65,$6C,$73,$65,$3F,$FF; 16:8D5B
    .db $49,$20,$77,$6F,$6E,$27,$74,$20,$62,$75,$79,$20,$74,$68,$61,$74; 16:8D6B
    .db $2E,$FF,$60,$FF,$40,$FF,$20,$20,$20,$31,$3A,$FE,$00,$20,$4C,$56; 16:8D7B
    .db $3A,$FD,$00,$FF,$20,$20,$20,$32,$3A,$FE,$01,$20,$4C,$56,$3A,$FD; 16:8D8B
    .db $01,$FF,$20,$FF,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$FF; 16:8D9B
    .db $41,$52,$4D,$4F,$52,$20,$53,$48,$4F,$50,$FF,$54,$4F,$4F,$4C,$20; 16:8DAB
    .db $53,$48,$4F,$50,$FF,$49,$4E,$4E,$FF,$50,$41,$57,$4E,$20,$53,$48; 16:8DBB
    .db $4F,$50,$FF,$44,$6F,$20,$79,$6F,$75,$20,$77,$69,$73,$68,$20,$74; 16:8DCB
    .db $6F,$20,$62,$75,$79,$3F,$FF,$54,$68,$69,$73,$20,$6F,$6E,$65,$3F; 16:8DDB
    .db $20,$FC,$FF,$59,$6F,$75,$20,$44,$75,$72,$63,$68,$61,$64,$20,$FC; 16:8DEB
    .db $2E,$FF,$24,$FF,$50,$6C,$65,$61,$73,$65,$20,$63,$6F,$6D,$65,$20; 16:8DFB
    .db $61,$67,$61,$69,$6E,$2E,$FF,$59,$6F,$75,$20,$68,$61,$76,$65,$20; 16:8E0B
    .db $74,$6F,$6F,$20,$6D,$61,$6E,$79,$20,$61,$6C,$72,$65,$61,$64,$79; 16:8E1B
    .db $2E,$FF,$59,$6F,$75,$20,$68,$61,$76,$65,$6E,$27,$74,$20,$65,$6E; 16:8E2B
    .db $6F,$75,$67,$68,$20,$6D,$6F,$6E,$65,$79,$2E,$FF,$4E,$65,$65,$64; 16:8E3B
    .db $20,$74,$6F,$20,$72,$65,$73,$74,$3F,$20,$50,$6C,$65,$61,$73,$65; 16:8E4B
    .db $20,$73,$74,$61,$79,$2E,$FF,$49,$74,$20,$77,$69,$6C,$6C,$20,$63; 16:8E5B
    .db $6F,$73,$74,$20,$79,$6F,$75,$20,$24,$FF,$48,$6F,$77,$20,$64,$6F; 16:8E6B
    .db $20,$66,$65,$65,$6C,$20,$74,$6F,$64,$61,$79,$3F,$FF,$24,$FF,$49; 16:8E7B
    .db $73,$20,$74,$68,$65,$72,$65,$20,$61,$6E,$79,$74,$68,$69,$6E,$67; 16:8E8B
    .db $20,$79,$6F,$75,$20,$20,$20,$20,$20,$1F,$1E,$1E,$20,$20,$20,$77; 16:8E9B
    .db $69,$73,$68,$20,$74,$6F,$20,$73,$65,$6C,$6C,$3F,$FF,$20,$FF,$59; 16:8EAB
    .db $45,$53,$20,$4E,$4F,$FF,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 16:8EBB
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 16:8ECB
    .db $20,$20,$1F,$1E,$1E,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 16:8EDB
    .db $20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20,$20; 16:8EEB
    .db $20,$20,$FF,$7A,$FF,$4C,$6F,$61,$64,$20,$74,$68,$69,$73,$20,$67; 16:8EFB
    .db $61,$6D,$65,$3F,$FF,$53,$61,$76,$65,$20,$74,$68,$69,$73,$20,$67; 16:8F0B
    .db $61,$6D,$65,$3F,$FF,$43,$61,$6E,$6E,$6F,$74,$20,$73,$61,$76,$65; 16:8F1B
    .db $21,$FF,$47,$41,$4D,$45,$3A,$FE,$00,$20,$4C,$56,$3A,$FD,$00,$FF; 16:8F2B
    .db $47,$41,$4D,$45,$3A,$FE,$01,$20,$4C,$56,$3A,$FD,$01,$FF,$59,$6F; 16:8F3B
    .db $75,$20,$70,$75,$72,$63,$68,$61,$73,$65,$64,$20,$FC,$2E,$FF,$80; 16:8F4B
    .db $89,$92,$9B,$A4,$E3,$BF,$E3,$BF,$E3,$BF,$E3,$BF,$DA,$DA,$EC,$E3; 16:8F5B
    .db $E3,$E3,$E3,$EC,$B6,$B6,$B6,$BF,$BF,$C8,$D1,$AD,$80,$80,$80,$89; 16:8F6B
    .db $89,$DA,$89,$AD,$D1,$D1,$9B,$9B,$B6,$DA,$DA,$C8,$C8,$AD,$AD,$DA; 16:8F7B
    .db $9B,$A4,$A4,$A4,$92,$9B,$B6,$D1,$92,$D1,$C8,$80,$D1,$EC,$EC,$EC; 16:8F8B
    .db $A0,$A9,$B2,$BB,$C4,$CD,$D6,$DF,$E8,$E8,$48,$51,$5A,$63,$6C,$F3; 16:8F9B
    .db $E7,$F3,$E7,$F3,$E7,$F3,$E7,$87,$8B,$8F,$97,$9F,$93,$9F,$9B,$A7; 16:8FAB
    .db $AB,$AF,$B3,$B7,$BB,$BF,$A3,$C3,$C3,$C3,$C7,$C7,$EF,$C7,$DF,$DB; 16:8FBB
    .db $DB,$D3,$D3,$E3,$EF,$EF,$EB,$EB,$DF,$DF,$EF,$D3,$D7,$D7,$D7,$CB; 16:8FCB
    .db $D3,$E3,$CF,$CB,$DB,$EB,$C3,$DB,$F7,$F7,$F7,$00,$09,$12,$1B,$24; 16:8FDB
    .db $2D,$36,$3F,$75,$7E,$E0,$E1,$E2,$E3,$E3,$60,$60,$61,$61,$62,$62; 16:8FEB
    .db $63,$63,$20,$20,$22,$22,$21,$20,$20,$61,$20,$22,$20,$22,$21,$21; 16:8FFB
    .db $60,$61,$00,$03,$01,$00,$01,$03,$02,$02,$41,$00,$40,$42,$60,$61; 16:900B
    .db $62,$60,$63,$61,$60,$60,$03,$41,$42,$40,$40,$41,$41,$42,$43,$42; 16:901B
    .db $41,$42,$43,$43,$41,$42,$E2,$E1,$E3,$E3,$E2,$E2,$E1,$E2,$C0,$C1; 16:902B
    .db $01,$02,$03,$04,$05,$01,$05,$02,$06,$03,$07,$04,$08,$01,$02,$03; 16:903B
    .db $04,$05,$06,$07,$08,$01,$02,$03,$04,$05,$06,$07,$08,$01,$02,$03; 16:904B
    .db $04,$05,$06,$07,$08,$09,$0A,$0B,$0C,$0D,$0E,$0F,$10,$11,$12,$13; 16:905B
    .db $14,$15,$16,$17,$18,$19,$1A,$1B,$1C,$1D,$1E,$1F,$20,$21,$22,$23; 16:906B
    .db $24,$01,$02,$03,$04,$05,$06,$07,$08,$01,$02; 16:907B
MenuItemNames:  ; 74 ptrs to FF-terminated item names (menu font; $49 blank)
    .db $1A,$91,$24,$91,$2E,$91,$39,$91,$46,$91,$50,$91,$80,$91,$5B,$91; 16:9086
    .db $8D,$91,$66,$91,$98,$91,$72,$91,$A6,$91,$B1,$91,$BE,$91,$C9,$91; 16:9096
    .db $D6,$91,$E3,$91,$EF,$91,$FA,$91,$05,$92,$10,$92,$1C,$92,$28,$92; 16:90A6
    .db $33,$92,$40,$92,$4D,$92,$5A,$92,$65,$92,$70,$92,$7D,$92,$86,$92; 16:90B6
    .db $92,$92,$9E,$92,$AB,$92,$B6,$92,$C3,$92,$CE,$92,$DB,$92,$E7,$92; 16:90C6
    .db $F4,$92,$00,$93,$09,$93,$14,$93,$21,$93,$2F,$93,$3D,$93,$4A,$93; 16:90D6
    .db $58,$93,$64,$93,$70,$93,$7D,$93,$8B,$93,$97,$93,$A0,$93,$AC,$93; 16:90E6
    .db $B8,$93,$C6,$93,$D3,$93,$E0,$93,$ED,$93,$FA,$93,$07,$94,$11,$94; 16:90F6
    .db $1A,$94,$25,$94,$2D,$94,$37,$94,$41,$94,$4A,$94,$52,$94,$5A,$94; 16:9106
    .db $61,$94,$68,$94,$0A,$0B,$0C,$5C,$5D,$57,$69,$6E,$64,$FF,$0A,$0B; 16:9116
    .db $0C,$5C,$5D,$46,$69,$72,$65,$FF,$0A,$0B,$0C,$5C,$5D,$57,$61,$74; 16:9126
    .db $65,$72,$FF,$0A,$0B,$0C,$5C,$5D,$54,$68,$75,$6E,$64,$65,$72,$FF; 16:9136
    .db $43,$72,$79,$73,$74,$61,$6C,$69,$73,$FF,$42,$61,$6C,$6C,$5C,$5D; 16:9146
    .db $57,$69,$6E,$64,$FF,$42,$61,$6C,$6C,$5C,$5D,$46,$69,$72,$65,$FF; 16:9156
    .db $42,$61,$6C,$6C,$5C,$5D,$57,$61,$74,$65,$72,$FF,$42,$61,$6C,$6C; 16:9166
    .db $5C,$5D,$54,$68,$75,$6E,$64,$65,$72,$FF,$54,$6F,$72,$6E,$61,$64; 16:9176
    .db $6F,$20,$3C,$3D,$3E,$5B,$FF,$46,$6C,$61,$6D,$65,$20,$3C,$3D,$3E; 16:9186
    .db $5B,$FF,$42,$6C,$69,$7A,$7A,$61,$72,$64,$20,$3C,$3D,$3E,$5B,$FF; 16:9196
    .db $53,$74,$6F,$72,$6D,$20,$3C,$3D,$3E,$5B,$FF,$43,$61,$72,$61,$70; 16:91A6
    .db $61,$63,$65,$20,$0D,$0E,$0F,$FF,$42,$72,$6F,$6E,$7A,$65,$20,$0D; 16:91B6
    .db $0E,$0F,$FF,$50,$6C,$61,$74,$69,$6E,$75,$6D,$20,$0D,$0E,$0F,$FF; 16:91C6
    .db $4D,$69,$72,$72,$6F,$72,$65,$64,$20,$0D,$0E,$0F,$FF,$43,$65,$72; 16:91D6
    .db $61,$6D,$69,$63,$20,$0D,$0E,$0F,$FF,$53,$61,$63,$72,$65,$64,$20; 16:91E6
    .db $0D,$0E,$0F,$FF,$42,$61,$74,$74,$6C,$65,$20,$0D,$0E,$0F,$FF,$50; 16:91F6
    .db $73,$79,$63,$68,$6F,$20,$0D,$0E,$0F,$FF,$54,$61,$6E,$6E,$65,$64; 16:9206
    .db $20,$48,$69,$64,$65,$FF,$4C,$65,$61,$74,$68,$65,$72,$20,$7B,$11; 16:9216
    .db $12,$FF,$42,$72,$6F,$6E,$7A,$65,$20,$7B,$11,$12,$FF,$50,$6C,$61; 16:9226
    .db $74,$69,$6E,$75,$6D,$20,$7B,$11,$12,$FF,$53,$6F,$6C,$64,$69,$65; 16:9236
    .db $72,$20,$53,$75,$69,$74,$FF,$43,$65,$72,$61,$6D,$69,$63,$20,$53; 16:9246
    .db $75,$69,$74,$FF,$42,$61,$74,$74,$6C,$65,$20,$7B,$11,$12,$FF,$50; 16:9256
    .db $73,$79,$63,$68,$6F,$20,$7B,$11,$12,$FF,$4D,$65,$64,$69,$63,$61; 16:9266
    .db $6C,$20,$48,$65,$72,$62,$FF,$41,$6E,$74,$69,$64,$6F,$74,$65,$FF; 16:9276
    .db $4C,$79,$73,$69,$73,$20,$50,$6C,$61,$6E,$74,$FF,$46,$72,$75,$69; 16:9286
    .db $74,$5C,$5D,$4C,$69,$6D,$65,$FF,$46,$72,$75,$69,$74,$5C,$5D,$50; 16:9296
    .db $6F,$77,$65,$72,$FF,$4D,$61,$67,$69,$63,$20,$52,$69,$6E,$67,$FF; 16:92A6
    .db $46,$72,$75,$69,$74,$5C,$5D,$52,$65,$70,$75,$6E,$FF,$57,$61,$72; 16:92B6
    .db $70,$20,$42,$6F,$6F,$74,$73,$FF,$53,$74,$61,$74,$75,$65,$5C,$5D; 16:92C6
    .db $4F,$6E,$79,$78,$FF,$4F,$70,$65,$6C,$20,$53,$74,$61,$74,$75,$65; 16:92D6
    .db $FF,$49,$6E,$73,$65,$63,$74,$20,$46,$6C,$75,$74,$65,$FF,$46,$6C; 16:92E6
    .db $75,$74,$65,$5C,$5D,$4C,$69,$6D,$65,$FF,$47,$61,$73,$20,$4D,$61; 16:92F6
    .db $73,$6B,$FF,$50,$6F,$77,$65,$72,$20,$52,$69,$6E,$67,$FF,$57,$61; 16:9306
    .db $72,$72,$69,$6F,$72,$20,$52,$69,$6E,$67,$FF,$49,$72,$6F,$6E,$20; 16:9316
    .db $4E,$65,$63,$6B,$6C,$61,$63,$65,$FF,$44,$65,$6F,$27,$73,$20,$50; 16:9326
    .db $65,$6E,$64,$61,$6E,$74,$FF,$52,$61,$62,$62,$69,$74,$20,$42,$6F; 16:9336
    .db $6F,$74,$73,$FF,$4C,$65,$61,$74,$68,$65,$72,$20,$42,$6F,$6F,$74; 16:9346
    .db $73,$FF,$53,$68,$69,$65,$6C,$64,$20,$52,$69,$6E,$67,$FF,$41,$6C; 16:9356
    .db $61,$72,$6D,$20,$46,$6C,$75,$74,$65,$FF,$57,$69,$6E,$64,$6D,$69; 16:9366
    .db $6C,$6C,$20,$4B,$65,$79,$FF,$4B,$65,$79,$20,$74,$6F,$20,$50,$72; 16:9376
    .db $69,$73,$6F,$6E,$FF,$4B,$65,$79,$20,$74,$6F,$20,$53,$74,$78,$79; 16:9386
    .db $FF,$46,$6F,$67,$20,$4C,$61,$6D,$70,$FF,$53,$68,$65,$6C,$6C,$20; 16:9396
    .db $46,$6C,$75,$74,$65,$FF,$45,$79,$65,$20,$47,$6C,$61,$73,$73,$65; 16:93A6
    .db $73,$FF,$42,$72,$6F,$6B,$65,$6E,$20,$53,$74,$61,$74,$75,$65,$FF; 16:93B6
    .db $47,$6C,$6F,$77,$69,$6E,$67,$20,$4C,$61,$6D,$70,$FF,$53,$74,$61; 16:93C6
    .db $74,$75,$65,$5C,$5D,$47,$6F,$6C,$64,$FF,$4C,$6F,$76,$65,$20,$50; 16:93D6
    .db $65,$6E,$64,$61,$6E,$74,$FF,$4B,$69,$72,$69,$73,$61,$20,$50,$6C; 16:93E6
    .db $61,$6E,$74,$FF,$49,$76,$6F,$72,$79,$20,$53,$74,$61,$74,$75,$65; 16:93F6
    .db $FF,$42,$6F,$77,$5C,$5D,$4D,$6F,$6F,$6E,$FF,$42,$6F,$77,$5C,$5D; 16:9406
    .db $53,$75,$6E,$FF,$42,$6F,$77,$5C,$5D,$54,$72,$75,$74,$68,$FF,$52; 16:9416
    .db $65,$66,$72,$65,$73,$68,$FF,$50,$61,$72,$61,$6C,$79,$73,$69,$73; 16:9426
    .db $FF,$54,$65,$6C,$65,$70,$61,$74,$68,$79,$FF,$54,$65,$6C,$65,$70; 16:9436
    .db $6F,$72,$74,$FF,$52,$65,$63,$6F,$76,$65,$72,$FF,$42,$61,$72,$72; 16:9446
    .db $69,$65,$72,$FF,$43,$68,$61,$6E,$67,$65,$FF,$46,$6C,$69,$67,$68; 16:9456
    .db $74,$FF,$20,$20,$20,$20,$20,$20,$20,$20,$FF,$6B,$29,$15,$16,$99; 16:9466
    .db $A3,$0D,$FB,$FE,$01,$2C,$38,$25,$10,$20,$20,$16,$12,$3D,$10,$1C; 16:9476
    .db $12,$37,$3A,$23,$15,$39,$0F,$FC,$19,$96,$26,$3E,$33,$10,$11,$24; 16:9486
    .db $3D,$20,$20,$9D,$10,$16,$3E,$9A,$29,$23,$33,$FC,$15,$12,$22,$16; 16:9496
    .db $25,$12,$10,$11,$38,$1B,$2F,$88,$0D,$FB,$FE,$01,$F9,$36,$90,$89; 16:94A6
    .db $2A,$10,$23,$17,$29,$10,$FC,$2F,$A1,$16,$26,$8C,$90,$17,$3D,$2B; 16:94B6
    .db $17,$1C,$32,$23,$35,$19,$0B,$FF,$FE,$0A,$89,$A3,$10,$1C,$40,$97; 16:94C6
    .db $41,$13,$3D,$10,$1C,$23,$12,$39,$10,$7D,$2A,$FC,$92,$10,$2B,$36; 16:94D6
    .db $42,$1A,$25,$3E,$A0,$0F,$FC,$2C,$11,$3E,$2A,$10,$8C,$19,$A4,$10; 16:94E6
    .db $25,$3E,$24,$16,$25,$38,$2F,$1D,$36,$0F; 16:94F6
ShopEntryJumps:  ; s60: JMP block armor 9558 / tool 957A / inn 959C / pawn 967A
    JMP loc_16_9558                     ; 9500  4c 58 95
sub_16_9503:
    JMP loc_16_957A                     ; 9503  4c 7a 95
sub_16_9506:
    JMP loc_16_959C                     ; 9506  4c 9c 95
sub_16_9509:
    JMP loc_16_967A                     ; 9509  4c 7a 96
sub_16_950C:
    JSR sub_16_9A62                     ; 950C  20 62 9a
    JSR sub_16_8534                     ; 950F  20 34 85
    JSR sub_16_9A91                     ; 9512  20 91 9a
    JSR WaitFrame                       ; 9515  20 69 c1
    LDY #$58                            ; 9518  a0 58
    STY chr_r2_alt                      ; 951A  84 5a
    INY                                 ; 951C  c8
    STY chr_r3_alt                      ; 951D  84 5b
    INY                                 ; 951F  c8
    STY chr_r4_alt                      ; 9520  84 5c
    INY                                 ; 9522  c8
    STY chr_r5_alt                      ; 9523  84 5d
    LDA #$3C                            ; 9525  a9 3c
    STA zp_5E                           ; 9527  85 5e
    LDA #$38                            ; 9529  a9 38
    STA zp_5F                           ; 952B  85 5f
    JSR sub_16_9912                     ; 952D  20 12 99
    JSR WaitFrame                       ; 9530  20 69 c1
    LDA #$01                            ; 9533  a9 01
    STA irq_mode                        ; 9535  85 51
    LDA #$00                            ; 9537  a9 00
    STA wram_6424                       ; 9539  8d 24 64
    STA $6425                           ; 953C  8d 25 64
    STA wram_6426                       ; 953F  8d 26 64
    STA wram_6427                       ; 9542  8d 27 64
    STA wram_646E                       ; 9545  8d 6e 64
    STA $646F                           ; 9548  8d 6f 64
    LDA #$FF                            ; 954B  a9 ff
    STA ram_07DD                        ; 954D  8d dd 07
    LDA #$00                            ; 9550  a9 00
    JSR sub_16_9A12                     ; 9552  20 12 9a
    JMP sub_16_8534                     ; 9555  4c 34 85
loc_16_9558:
    JSR sub_16_950C                     ; 9558  20 0c 95
    JSR WaitFrame                       ; 955B  20 69 c1
    LDA #$3E                            ; 955E  a9 3e
    STA chr_r1_alt                      ; 9560  85 59
    LDA #$18                            ; 9562  a9 18
    JSR MenuPrint                       ; 9564  20 33 89
    LDA #$1C                            ; 9567  a9 1c
    JSR MenuPrint                       ; 9569  20 33 89
    LDA #$29                            ; 956C  a9 29
    JSR MenuPrint                       ; 956E  20 33 89
    JSR ShopSetupArmor                  ; 9571  20 8a 98
    JSR sub_16_96CF                     ; 9574  20 cf 96
    JMP loc_16_96C6                     ; 9577  4c c6 96
loc_16_957A:
    JSR sub_16_950C                     ; 957A  20 0c 95
    JSR WaitFrame                       ; 957D  20 69 c1
    LDA #$3A                            ; 9580  a9 3a
    STA chr_r1_alt                      ; 9582  85 59
    LDA #$19                            ; 9584  a9 19
    JSR MenuPrint                       ; 9586  20 33 89
    LDA #$1C                            ; 9589  a9 1c
    JSR MenuPrint                       ; 958B  20 33 89
    LDA #$29                            ; 958E  a9 29
    JSR MenuPrint                       ; 9590  20 33 89
    JSR ShopSetupTool                   ; 9593  20 e3 98
    JSR sub_16_96CF                     ; 9596  20 cf 96
    JMP loc_16_96C6                     ; 9599  4c c6 96
loc_16_959C:
    JSR sub_16_9A62                     ; 959C  20 62 9a
    JSR sub_16_8534                     ; 959F  20 34 85
    JSR sub_16_9A91                     ; 95A2  20 91 9a
    JSR sub_16_950C                     ; 95A5  20 0c 95
    JSR ShopIdForArea                   ; 95A8  20 53 99
    LDA #$00                            ; 95AB  a9 00
    STA shop_price_sel                  ; 95AD  8d 74 64
    STA shop_price_sel_hi               ; 95B0  8d 75 64
    LDA #$38                            ; 95B3  a9 38
    STA chr_r1_alt                      ; 95B5  85 59
    LDA #$1A                            ; 95B7  a9 1a
    JSR MenuPrint                       ; 95B9  20 33 89
    LDA #$23                            ; 95BC  a9 23
    JSR MenuPrint                       ; 95BE  20 33 89
    LDA #$24                            ; 95C1  a9 24
    JSR MenuPrint                       ; 95C3  20 33 89
    LDA #$29                            ; 95C6  a9 29
    JSR MenuPrint                       ; 95C8  20 33 89
    LDA shop_id                         ; 95CB  ad 6d 64
    ASL A                               ; 95CE  0a
    TAX                                 ; 95CF  aa
    LDA $9EAC,X                         ; 95D0  bd ac 9e
    STA shop_price_sel                  ; 95D3  8d 74 64
    LDA $9EAD,X                         ; 95D6  bd ad 9e
    STA shop_price_sel_hi               ; 95D9  8d 75 64
    LDA #$0C                            ; 95DC  a9 0c
    JSR sub_16_8613                     ; 95DE  20 13 86
    LDA #$41                            ; 95E1  a9 41
    JSR sub_16_84DA                     ; 95E3  20 da 84
    BCC loc_16_95F0                     ; 95E6  90 08
    LDA #$4C                            ; 95E8  a9 4c
    JSR sub_16_8A9F                     ; 95EA  20 9f 8a
    JMP loc_16_9665                     ; 95ED  4c 65 96
loc_16_95F0:
    LDA shop_id                         ; 95F0  ad 6d 64
    ASL A                               ; 95F3  0a
    TAX                                 ; 95F4  aa
    SEC                                 ; 95F5  38
    LDA gold                            ; 95F6  ad 02 07
    SBC shop_price_sel                  ; 95F9  ed 74 64
    STA shop_price_sel                  ; 95FC  8d 74 64
    LDA gold_hi                         ; 95FF  ad 03 07
    SBC shop_price_sel_hi               ; 9602  ed 75 64
    STA shop_price_sel_hi               ; 9605  8d 75 64
    BCC loc_16_964E                     ; 9608  90 44
    LDA #$55                            ; 960A  a9 55
    JSR sub_16_8A9F                     ; 960C  20 9f 8a
    JSR FadeSaveZpToWram                ; 960F  20 d6 c7
    LDA #$2A                            ; 9612  a9 2a
    JSR MenuPrint                       ; 9614  20 33 89
    LDA #$2B                            ; 9617  a9 2b
    JSR MenuPrint                       ; 9619  20 33 89
    LDA #$25                            ; 961C  a9 25
    JSR MenuPrint                       ; 961E  20 33 89
    LDA #$26                            ; 9621  a9 26
    JSR MenuPrint                       ; 9623  20 33 89
    LDA player_hp_max                   ; 9626  ad c0 03
    STA player_hp                       ; 9629  8d c1 03
    LDA mp_max                          ; 962C  ad 09 07
    STA mp_cur                          ; 962F  8d 08 07
    LDA obj1_flash                      ; 9632  ad 10 07
    AND #$F0                            ; 9635  29 f0
    STA obj1_flash                      ; 9637  8d 10 07
    LDA shop_price_sel                  ; 963A  ad 74 64
    STA gold                            ; 963D  8d 02 07
    LDA shop_price_sel_hi               ; 9640  ad 75 64
    STA gold_hi                         ; 9643  8d 03 07
    LDA #$01                            ; 9646  a9 01
    JSR sub_16_8613                     ; 9648  20 13 86
    JMP loc_16_9674                     ; 964B  4c 74 96
loc_16_964E:
    LDA #$2A                            ; 964E  a9 2a
    JSR MenuPrint                       ; 9650  20 33 89
    LDA #$2B                            ; 9653  a9 2b
    JSR MenuPrint                       ; 9655  20 33 89
    LDA #$2F                            ; 9658  a9 2f
    JSR MenuPrint                       ; 965A  20 33 89
    LDA #$01                            ; 965D  a9 01
    JSR sub_16_8613                     ; 965F  20 13 86
    JMP loc_16_9674                     ; 9662  4c 74 96
loc_16_9665:
    LDA #$2B                            ; 9665  a9 2b
    JSR MenuPrint                       ; 9667  20 33 89
    LDA #$2A                            ; 966A  a9 2a
    JSR MenuPrint                       ; 966C  20 33 89
    LDA #$20                            ; 966F  a9 20
    JSR MenuPrint                       ; 9671  20 33 89
loc_16_9674:
    JSR sub_16_9878                     ; 9674  20 78 98
    JMP loc_16_96C6                     ; 9677  4c c6 96
loc_16_967A:
    JSR sub_16_9A62                     ; 967A  20 62 9a
    JSR sub_16_8534                     ; 967D  20 34 85
    JSR sub_16_9A91                     ; 9680  20 91 9a
    JSR sub_16_950C                     ; 9683  20 0c 95
    JSR ShopIdForArea                   ; 9686  20 53 99
    LDA #$1B                            ; 9689  a9 1b
    JSR MenuPrint                       ; 968B  20 33 89
    LDA #$27                            ; 968E  a9 27
    JSR MenuPrint                       ; 9690  20 33 89
    LDA #$28                            ; 9693  a9 28
    JSR MenuPrint                       ; 9695  20 33 89
    LDA #$29                            ; 9698  a9 29
    JSR MenuPrint                       ; 969A  20 33 89
    LDA #$41                            ; 969D  a9 41
    JSR sub_16_84DA                     ; 969F  20 da 84
    BCC loc_16_96BE                     ; 96A2  90 1a
    LDA #$4C                            ; 96A4  a9 4c
    JSR sub_16_8A9F                     ; 96A6  20 9f 8a
    LDA #$2B                            ; 96A9  a9 2b
    JSR MenuPrint                       ; 96AB  20 33 89
    LDA #$2A                            ; 96AE  a9 2a
    JSR MenuPrint                       ; 96B0  20 33 89
    LDA #$20                            ; 96B3  a9 20
    JSR MenuPrint                       ; 96B5  20 33 89
    JSR sub_16_9878                     ; 96B8  20 78 98
    JMP loc_16_96C6                     ; 96BB  4c c6 96
loc_16_96BE:
    LDA #$4C                            ; 96BE  a9 4c
    JSR sub_16_8A9F                     ; 96C0  20 9f 8a
    JSR sub_16_8000                     ; 96C3  20 00 80
loc_16_96C6:
    JSR sub_16_8605                     ; 96C6  20 05 86
    LDA #$00                            ; 96C9  a9 00
    STA ram_07DD                        ; 96CB  8d dd 07
    RTS                                 ; 96CE  60
sub_16_96CF:
    LDA #$41                            ; 96CF  a9 41
    JSR sub_16_84DA                     ; 96D1  20 da 84
    BCC loc_16_96D9                     ; 96D4  90 03
    JMP loc_16_97DD                     ; 96D6  4c dd 97
loc_16_96D9:
    LDA #$4C                            ; 96D9  a9 4c
    JSR sub_16_8A9F                     ; 96DB  20 9f 8a
    JSR sub_16_9815                     ; 96DE  20 15 98
    JSR sub_16_9837                     ; 96E1  20 37 98
    LDA #$2A                            ; 96E4  a9 2a
    JSR MenuPrint                       ; 96E6  20 33 89
    LDA shop_goods                      ; 96E9  ad 70 64
    STA tmp_19                          ; 96EC  85 19
    LDA #$1D                            ; 96EE  a9 1d
    JSR MenuPrint                       ; 96F0  20 33 89
    LDA #$32                            ; 96F3  a9 32
    JSR MenuPrint                       ; 96F5  20 33 89
loc_16_96F8:
    JSR sub_16_9884                     ; 96F8  20 84 98
    LDA wram_6427                       ; 96FB  ad 27 64
    TAX                                 ; 96FE  aa
    LDA shop_goods,X                    ; 96FF  bd 70 64
    STA wram_646C                       ; 9702  8d 6c 64
    LDA pad_new                         ; 9705  a5 4b
    AND #$02                            ; 9707  29 02
    BEQ loc_16_9719                     ; 9709  f0 0e
    DEC wram_6427                       ; 970B  ce 27 64
    LDA wram_6427                       ; 970E  ad 27 64
    AND #$03                            ; 9711  29 03
    STA wram_6427                       ; 9713  8d 27 64
    JMP loc_16_97EF                     ; 9716  4c ef 97
loc_16_9719:
    LDA pad_new                         ; 9719  a5 4b
    AND #$01                            ; 971B  29 01
    BEQ loc_16_972D                     ; 971D  f0 0e
    INC wram_6427                       ; 971F  ee 27 64
    LDA wram_6427                       ; 9722  ad 27 64
    AND #$03                            ; 9725  29 03
    STA wram_6427                       ; 9727  8d 27 64
    JMP loc_16_97EF                     ; 972A  4c ef 97
loc_16_972D:
    LDA pad_new                         ; 972D  a5 4b
    AND #$20                            ; 972F  29 20
    BEQ loc_16_9734                     ; 9731  f0 01
    RTS                                 ; 9733  60
loc_16_9734:
    LDA pad_new                         ; 9734  a5 4b
    AND #$40                            ; 9736  29 40
    BEQ loc_16_974C                     ; 9738  f0 12
loc_16_973A:
    LDA #$2A                            ; 973A  a9 2a
    JSR MenuPrint                       ; 973C  20 33 89
    LDA #$30                            ; 973F  a9 30
    JSR MenuPrint                       ; 9741  20 33 89
    LDA #$29                            ; 9744  a9 29
    JSR MenuPrint                       ; 9746  20 33 89
    JMP loc_16_97B3                     ; 9749  4c b3 97
loc_16_974C:
    LDA pad_new                         ; 974C  a5 4b
    AND #$80                            ; 974E  29 80
    BNE loc_16_9755                     ; 9750  d0 03
    JMP loc_16_97CD                     ; 9752  4c cd 97
loc_16_9755:
    LDA #$4C                            ; 9755  a9 4c
    JSR sub_16_8A9F                     ; 9757  20 9f 8a
    JSR sub_16_9970                     ; 975A  20 70 99
    TAX                                 ; 975D  aa
    LDA shop_price_sel                  ; 975E  ad 74 64
    ORA shop_price_sel_hi               ; 9761  0d 75 64
    BNE loc_16_9769                     ; 9764  d0 03
    JMP loc_16_973A                     ; 9766  4c 3a 97
loc_16_9769:
    TXA                                 ; 9769  8a
    CMP #$01                            ; 976A  c9 01
    BNE loc_16_977E                     ; 976C  d0 10
    LDA #$2A                            ; 976E  a9 2a
    JSR MenuPrint                       ; 9770  20 33 89
    LDA #$22                            ; 9773  a9 22
    JSR MenuPrint                       ; 9775  20 33 89
    JSR sub_16_9878                     ; 9778  20 78 98
    JMP loc_16_97A4                     ; 977B  4c a4 97
loc_16_977E:
    CMP #$02                            ; 977E  c9 02
    BNE loc_16_9792                     ; 9780  d0 10
    LDA #$2A                            ; 9782  a9 2a
    JSR MenuPrint                       ; 9784  20 33 89
    LDA #$21                            ; 9787  a9 21
    JSR MenuPrint                       ; 9789  20 33 89
    JSR sub_16_9878                     ; 978C  20 78 98
    JMP loc_16_97A4                     ; 978F  4c a4 97
loc_16_9792:
    LDA #$2A                            ; 9792  a9 2a
    JSR MenuPrint                       ; 9794  20 33 89
    LDA #$2E                            ; 9797  a9 2e
    JSR MenuPrint                       ; 9799  20 33 89
    LDA #$01                            ; 979C  a9 01
    JSR sub_16_8613                     ; 979E  20 13 86
    JSR sub_16_9878                     ; 97A1  20 78 98
loc_16_97A4:
    LDA #$2A                            ; 97A4  a9 2a
    JSR MenuPrint                       ; 97A6  20 33 89
    LDA #$1F                            ; 97A9  a9 1f
    JSR MenuPrint                       ; 97AB  20 33 89
    LDA #$29                            ; 97AE  a9 29
    JSR MenuPrint                       ; 97B0  20 33 89
loc_16_97B3:
    LDA #$41                            ; 97B3  a9 41
    JSR sub_16_84DA                     ; 97B5  20 da 84
    BCS loc_16_97DD                     ; 97B8  b0 23
    LDA #$4C                            ; 97BA  a9 4c
    JSR sub_16_8A9F                     ; 97BC  20 9f 8a
    LDA #$2A                            ; 97BF  a9 2a
    JSR MenuPrint                       ; 97C1  20 33 89
    LDA #$1D                            ; 97C4  a9 1d
    JSR MenuPrint                       ; 97C6  20 33 89
    LDA #$00                            ; 97C9  a9 00
    STA pad_new                         ; 97CB  85 4b
loc_16_97CD:
    JSR ShopPriceSel                    ; 97CD  20 37 9a
    LDA #$0B                            ; 97D0  a9 0b
    JSR sub_16_8613                     ; 97D2  20 13 86
    LDA #$32                            ; 97D5  a9 32
    JSR MenuPrint                       ; 97D7  20 33 89
    JMP loc_16_96F8                     ; 97DA  4c f8 96
loc_16_97DD:
    LDA #$4C                            ; 97DD  a9 4c
    JSR sub_16_8A9F                     ; 97DF  20 9f 8a
    LDA #$2A                            ; 97E2  a9 2a
    JSR MenuPrint                       ; 97E4  20 33 89
    LDA #$26                            ; 97E7  a9 26
    JSR MenuPrint                       ; 97E9  20 33 89
    JMP sub_16_9878                     ; 97EC  4c 78 98
loc_16_97EF:
    LDA #$4B                            ; 97EF  a9 4b
    JSR sub_16_8A9F                     ; 97F1  20 9f 8a
    JSR sub_16_9815                     ; 97F4  20 15 98
    JSR sub_16_9837                     ; 97F7  20 37 98
    LDA #$2A                            ; 97FA  a9 2a
    JSR MenuPrint                       ; 97FC  20 33 89
    LDA wram_6427                       ; 97FF  ad 27 64
    TAX                                 ; 9802  aa
    LDA shop_goods,X                    ; 9803  bd 70 64
    STA tmp_19                          ; 9806  85 19
    LDA #$1D                            ; 9808  a9 1d
    JSR MenuPrint                       ; 980A  20 33 89
    LDA #$32                            ; 980D  a9 32
    JSR MenuPrint                       ; 980F  20 33 89
    JMP loc_16_96F8                     ; 9812  4c f8 96
sub_16_9815:
    LDA wram_6427                       ; 9815  ad 27 64
    TAX                                 ; 9818  aa
    LDA $9874,X                         ; 9819  bd 74 98
    STA wram_60FB                       ; 981C  8d fb 60
    LDA #$21                            ; 981F  a9 21
    STA wram_60F8                       ; 9821  8d f8 60
    LDA #$00                            ; 9824  a9 00
    STA tmp_12                          ; 9826  85 12
    STA wram_60F9                       ; 9828  8d f9 60
    LDA #$01                            ; 982B  a9 01
    STA wram_60FA                       ; 982D  8d fa 60
    LDA #$3B                            ; 9830  a9 3b
    STA tmp_2E                          ; 9832  85 2e
    JMP MenuSprDraw                     ; 9834  4c d3 86
sub_16_9837:
    LDA wram_6427                       ; 9837  ad 27 64
    TAX                                 ; 983A  aa
    LDA shop_goods,X                    ; 983B  bd 70 64
    CMP #$FF                            ; 983E  c9 ff
    BNE loc_16_9843                     ; 9840  d0 01
    RTS                                 ; 9842  60
loc_16_9843:
    PHA                                 ; 9843  48
    TAX                                 ; 9844  aa
    LDA $8FA5,X                         ; 9845  bd a5 8f
    STA wram_60F9                       ; 9848  8d f9 60
    LDA wram_6427                       ; 984B  ad 27 64
    TAX                                 ; 984E  aa
    LDA $9874,X                         ; 984F  bd 74 98
    STA wram_60FB                       ; 9852  8d fb 60
    LDA #$21                            ; 9855  a9 21
    STA wram_60F8                       ; 9857  8d f8 60
    LDA #$01                            ; 985A  a9 01
    STA wram_60FA                       ; 985C  8d fa 60
    LDA #$02                            ; 985F  a9 02
    STA tmp_12                          ; 9861  85 12
    LDA #$37                            ; 9863  a9 37
    STA tmp_2E                          ; 9865  85 2e
    JSR MenuSprDraw                     ; 9867  20 d3 86
    PLA                                 ; 986A  68
    TAX                                 ; 986B  aa
    LDA $8FF0,X                         ; 986C  bd f0 8f
    AND #$03                            ; 986F  29 03
    JMP sub_16_9A12                     ; 9871  4c 12 9a
    .db $40,$68,$90,$B8                 ; 16:9874
sub_16_9878:
    JSR sub_16_9884                     ; 9878  20 84 98
    LDA pad_new                         ; 987B  a5 4b
    BEQ sub_16_9878                     ; 987D  f0 f9
    LDA #$4B                            ; 987F  a9 4b
    JMP sub_16_8A9F                     ; 9881  4c 9f 8a
sub_16_9884:
    JSR WaitFrame                       ; 9884  20 69 c1
    JMP ReadPadsMenu                    ; 9887  4c e0 fe
ShopSetupArmor:  ; s71: ShopIdForArea then goods 16:9DA4 -> $6470 and prices 16:9DD0 -> $6478 for shop_id
    JSR ShopIdForArea                   ; 988A  20 53 99
    LDA shop_id                         ; 988D  ad 6d 64
    ASL A                               ; 9890  0a
    ASL A                               ; 9891  0a
    TAX                                 ; 9892  aa
    LDY #$00                            ; 9893  a0 00
loc_16_9895:
    LDA ShopArmorGoods,X                ; 9895  bd a4 9d
    STA shop_goods,Y                    ; 9898  99 70 64
    INX                                 ; 989B  e8
    INY                                 ; 989C  c8
    CPY #$04                            ; 989D  c0 04
    BNE loc_16_9895                     ; 989F  d0 f4
    LDA shop_id                         ; 98A1  ad 6d 64
    ASL A                               ; 98A4  0a
    ASL A                               ; 98A5  0a
    ASL A                               ; 98A6  0a
    TAX                                 ; 98A7  aa
    LDY #$00                            ; 98A8  a0 00
loc_16_98AA:
    LDA ShopArmorPrices,X               ; 98AA  bd d0 9d
    STA shop_prices,Y                   ; 98AD  99 78 64
    INX                                 ; 98B0  e8
    INY                                 ; 98B1  c8
    CPY #$08                            ; 98B2  c0 08
    BNE loc_16_98AA                     ; 98B4  d0 f4
loc_16_98B6:
    LDA #$00                            ; 98B6  a9 00
    STA tmp_19                          ; 98B8  85 19
loc_16_98BA:
    LDX tmp_19                          ; 98BA  a6 19
    LDA $9F96,X                         ; 98BC  bd 96 9f
    STA tmp_26                          ; 98BF  85 26
    LDA #$03                            ; 98C1  a9 03
    STA tmp_27                          ; 98C3  85 27
    LDA shop_goods,X                    ; 98C5  bd 70 64
    CMP #$FF                            ; 98C8  c9 ff
    BNE loc_16_98D1                     ; 98CA  d0 05
    STA tmp_29                          ; 98CC  85 29
    JMP loc_16_98D7                     ; 98CE  4c d7 98
loc_16_98D1:
    TAX                                 ; 98D1  aa
    LDA $8F5A,X                         ; 98D2  bd 5a 8f
    STA tmp_29                          ; 98D5  85 29
loc_16_98D7:
    JSR sub_16_88A8                     ; 98D7  20 a8 88
    INC tmp_19                          ; 98DA  e6 19
    LDA tmp_19                          ; 98DC  a5 19
    CMP #$04                            ; 98DE  c9 04
    BNE loc_16_98BA                     ; 98E0  d0 d8
    RTS                                 ; 98E2  60
ShopSetupTool:  ; s71: ShopIdForArea then goods 16:9E28 -> $6470 and prices 16:9E54 -> $6478 for shop_id
    JSR ShopIdForArea                   ; 98E3  20 53 99
    LDA shop_id                         ; 98E6  ad 6d 64
    ASL A                               ; 98E9  0a
    ASL A                               ; 98EA  0a
    TAX                                 ; 98EB  aa
    LDY #$00                            ; 98EC  a0 00
loc_16_98EE:
    LDA ShopToolGoods,X                 ; 98EE  bd 28 9e
    STA shop_goods,Y                    ; 98F1  99 70 64
    INX                                 ; 98F4  e8
    INY                                 ; 98F5  c8
    CPY #$04                            ; 98F6  c0 04
    BNE loc_16_98EE                     ; 98F8  d0 f4
    LDA shop_id                         ; 98FA  ad 6d 64
    ASL A                               ; 98FD  0a
    ASL A                               ; 98FE  0a
    ASL A                               ; 98FF  0a
    TAX                                 ; 9900  aa
    LDY #$00                            ; 9901  a0 00
loc_16_9903:
    LDA ShopToolPrices,X                ; 9903  bd 54 9e
    STA shop_prices,Y                   ; 9906  99 78 64
    INX                                 ; 9909  e8
    INY                                 ; 990A  c8
    CPY #$08                            ; 990B  c0 08
    BNE loc_16_9903                     ; 990D  d0 f4
    JMP loc_16_98B6                     ; 990F  4c b6 98
sub_16_9912:
    LDA #$00                            ; 9912  a9 00
    STA tmp_20                          ; 9914  85 20
    STA tmp_25                          ; 9916  85 25
    LDA #$28                            ; 9918  a9 28
    STA tmp_21                          ; 991A  85 21
    LDA #$40                            ; 991C  a9 40
    STA tmp_24                          ; 991E  85 24
    LDX #$06                            ; 9920  a2 06
    JSR sub_16_8864                     ; 9922  20 64 88
    LDA #$03                            ; 9925  a9 03
    STA tmp_19                          ; 9927  85 19
loc_16_9929:
    LDX #$09                            ; 9929  a2 09
    JSR sub_16_8864                     ; 992B  20 64 88
    DEC tmp_19                          ; 992E  c6 19
    BNE loc_16_9929                     ; 9930  d0 f7
    LDX #$0C                            ; 9932  a2 0c
    JSR sub_16_8864                     ; 9934  20 64 88
    JSR WaitPpuQueue                    ; 9937  20 76 c6
    LDX #$41                            ; 993A  a2 41
    LDA #$FF                            ; 993C  a9 ff
loc_16_993E:
    STA wram_buf,X                      ; 993E  9d 00 60
    DEX                                 ; 9941  ca
    BNE loc_16_993E                     ; 9942  d0 fa
    LDA #$2B                            ; 9944  a9 2b
    STA tmp_21                          ; 9946  85 21
    LDA #$C0                            ; 9948  a9 c0
    STA tmp_20                          ; 994A  85 20
    LDA #$01                            ; 994C  a9 01
    STA tmp_25                          ; 994E  85 25
    JMP MenuFlush                       ; 9950  4c b0 8a
ShopIdForArea:  ; s71: area_id lookup in ShopAreaTable (33) -> shop_id $646D via 16:9F75
    LDX #$00                            ; 9953  a2 00
    LDA a:area_id                       ; 9955  ad 6c 00
loc_16_9958:
    CMP ShopAreaTable,X                 ; 9958  dd 54 9f
    BEQ loc_16_9965                     ; 995B  f0 08
    INX                                 ; 995D  e8
    CPX #$21                            ; 995E  e0 21
    BNE loc_16_9958                     ; 9960  d0 f6
    JMP loc_16_996C                     ; 9962  4c 6c 99
loc_16_9965:
    LDA ShopAreaIds,X                   ; 9965  bd 75 9f
    STA shop_id                         ; 9968  8d 6d 64
    RTS                                 ; 996B  60
loc_16_996C:
    STX shop_id                         ; 996C  8e 6d 64
    RTS                                 ; 996F  60
sub_16_9970:
    SEC                                 ; 9970  38
    LDA gold                            ; 9971  ad 02 07
    SBC shop_price_sel                  ; 9974  ed 74 64
    STA $6476                           ; 9977  8d 76 64
    LDA gold_hi                         ; 997A  ad 03 07
    SBC shop_price_sel_hi               ; 997D  ed 75 64
    STA $6477                           ; 9980  8d 77 64
    BCS loc_16_9988                     ; 9983  b0 03
    LDA #$01                            ; 9985  a9 01
    RTS                                 ; 9987  60
loc_16_9988:
    LDA sub_state                       ; 9988  a5 41
    CMP #$21                            ; 998A  c9 21
    BEQ loc_16_99BF                     ; 998C  f0 31
    LDA wram_646C                       ; 998E  ad 6c 64
    CMP #$15                            ; 9991  c9 15
    BPL loc_16_99AA                     ; 9993  10 15
    LDA #$08                            ; 9995  a9 08
    STA tmp_2E                          ; 9997  85 2e
    LDX #$00                            ; 9999  a2 00
loc_16_999B:
    LDA $6438,X                         ; 999B  bd 38 64
    CMP #$FF                            ; 999E  c9 ff
    BEQ loc_16_99DE                     ; 99A0  f0 3c
    INX                                 ; 99A2  e8
    CPX #$04                            ; 99A3  e0 04
    BNE loc_16_999B                     ; 99A5  d0 f4
    LDA #$02                            ; 99A7  a9 02
    RTS                                 ; 99A9  60
loc_16_99AA:
    LDA #$04                            ; 99AA  a9 04
    STA tmp_2E                          ; 99AC  85 2e
    LDX #$00                            ; 99AE  a2 00
loc_16_99B0:
    LDA $6434,X                         ; 99B0  bd 34 64
    CMP #$FF                            ; 99B3  c9 ff
    BEQ loc_16_99DE                     ; 99B5  f0 27
    INX                                 ; 99B7  e8
    CPX #$04                            ; 99B8  e0 04
    BNE loc_16_99B0                     ; 99BA  d0 f4
    LDA #$02                            ; 99BC  a9 02
    RTS                                 ; 99BE  60
loc_16_99BF:
    LDA wram_646C                       ; 99BF  ad 6c 64
    CMP #$29                            ; 99C2  c9 29
    BNE loc_16_99C9                     ; 99C4  d0 03
    JMP loc_16_99FB                     ; 99C6  4c fb 99
loc_16_99C9:
    LDA #$10                            ; 99C9  a9 10
    STA tmp_2E                          ; 99CB  85 2e
    LDX #$00                            ; 99CD  a2 00
loc_16_99CF:
    LDA $6440,X                         ; 99CF  bd 40 64
    CMP #$FF                            ; 99D2  c9 ff
    BEQ loc_16_99DE                     ; 99D4  f0 08
    INX                                 ; 99D6  e8
    CPX #$08                            ; 99D7  e0 08
    BNE loc_16_99CF                     ; 99D9  d0 f4
    LDA #$02                            ; 99DB  a9 02
    RTS                                 ; 99DD  60
loc_16_99DE:
    STX tmp_2F                          ; 99DE  86 2f
    LDA $6476                           ; 99E0  ad 76 64
    STA gold                            ; 99E3  8d 02 07
    LDA $6477                           ; 99E6  ad 77 64
    STA gold_hi                         ; 99E9  8d 03 07
    CLC                                 ; 99EC  18
    LDA tmp_2E                          ; 99ED  a5 2e
    ADC tmp_2F                          ; 99EF  65 2f
    TAX                                 ; 99F1  aa
    LDA wram_646C                       ; 99F2  ad 6c 64
    STA inventory,X                     ; 99F5  9d 30 64
    LDA #$00                            ; 99F8  a9 00
    RTS                                 ; 99FA  60
loc_16_99FB:
    LDA #$18                            ; 99FB  a9 18
    STA tmp_2E                          ; 99FD  85 2e
    LDX #$00                            ; 99FF  a2 00
loc_16_9A01:
    LDA $6448,X                         ; 9A01  bd 48 64
    CMP #$FF                            ; 9A04  c9 ff
    BEQ loc_16_9A0F                     ; 9A06  f0 07
    INX                                 ; 9A08  e8
    CPX #$08                            ; 9A09  e0 08
    BNE loc_16_9A01                     ; 9A0B  d0 f4
    LDX #$07                            ; 9A0D  a2 07
loc_16_9A0F:
    JMP loc_16_99DE                     ; 9A0F  4c de 99
sub_16_9A12:
    ASL A                               ; 9A12  0a
    ASL A                               ; 9A13  0a
    TAX                                 ; 9A14  aa
    LDY #$00                            ; 9A15  a0 00
    LDA sub_state                       ; 9A17  a5 41
    CMP #$21                            ; 9A19  c9 21
    BEQ loc_16_9A2A                     ; 9A1B  f0 0d
loc_16_9A1D:
    LDA $8BFB,X                         ; 9A1D  bd fb 8b
    STA wram_6154,Y                     ; 9A20  99 54 61
    INX                                 ; 9A23  e8
    INY                                 ; 9A24  c8
    CPY #$04                            ; 9A25  c0 04
    BNE loc_16_9A1D                     ; 9A27  d0 f4
    RTS                                 ; 9A29  60
loc_16_9A2A:
    LDA $8C0B,X                         ; 9A2A  bd 0b 8c
    STA wram_6154,Y                     ; 9A2D  99 54 61
    INX                                 ; 9A30  e8
    INY                                 ; 9A31  c8
    CPY #$04                            ; 9A32  c0 04
    BNE loc_16_9A2A                     ; 9A34  d0 f4
    RTS                                 ; 9A36  60
ShopPriceSel:  ; s71: $6474/75 = shop_prices[$6427] (0 if the shop_goods slot is $FF)
    LDA wram_6427                       ; 9A37  ad 27 64
    TAX                                 ; 9A3A  aa
    LDA shop_goods,X                    ; 9A3B  bd 70 64
    CMP #$FF                            ; 9A3E  c9 ff
    BEQ loc_16_9A54                     ; 9A40  f0 12
    LDA wram_6427                       ; 9A42  ad 27 64
    ASL A                               ; 9A45  0a
    TAX                                 ; 9A46  aa
    LDA shop_prices,X                   ; 9A47  bd 78 64
    STA shop_price_sel                  ; 9A4A  8d 74 64
    LDA $6479,X                         ; 9A4D  bd 79 64
    STA shop_price_sel_hi               ; 9A50  8d 75 64
    RTS                                 ; 9A53  60
loc_16_9A54:
    LDA wram_6427                       ; 9A54  ad 27 64
    ASL A                               ; 9A57  0a
    TAX                                 ; 9A58  aa
    LDA #$00                            ; 9A59  a9 00
    STA shop_price_sel                  ; 9A5B  8d 74 64
    STA shop_price_sel_hi               ; 9A5E  8d 75 64
    RTS                                 ; 9A61  60
sub_16_9A62:
    LDA #$00                            ; 9A62  a9 00
    STA tmp_18                          ; 9A64  85 18
loc_16_9A66:
    LDA tmp_18                          ; 9A66  a5 18
    ASL A                               ; 9A68  0a
    TAX                                 ; 9A69  aa
    LDA $9ADE,X                         ; 9A6A  bd de 9a
    STA tmp_28                          ; 9A6D  85 28
    LDA $9ADF,X                         ; 9A6F  bd df 9a
    STA tmp_29                          ; 9A72  85 29
    LDX tmp_18                          ; 9A74  a6 18
    LDA inv_select,X                    ; 9A76  bd 28 64
    BPL loc_16_9A80                     ; 9A79  10 05
    LDA #$FF                            ; 9A7B  a9 ff
    JMP loc_16_9A83                     ; 9A7D  4c 83 9a
loc_16_9A80:
    TAY                                 ; 9A80  a8
    LDA (tmp_28),Y                      ; 9A81  b1 28
loc_16_9A83:
    LDX tmp_18                          ; 9A83  a6 18
    STA wram_buf,X                      ; 9A85  9d 00 60
    INC tmp_18                          ; 9A88  e6 18
    LDA tmp_18                          ; 9A8A  a5 18
    CMP #$08                            ; 9A8C  c9 08
    BNE loc_16_9A66                     ; 9A8E  d0 d6
    RTS                                 ; 9A90  60
sub_16_9A91:
    LDA #$00                            ; 9A91  a9 00
    STA tmp_18                          ; 9A93  85 18
loc_16_9A95:
    LDA tmp_18                          ; 9A95  a5 18
    ASL A                               ; 9A97  0a
    TAX                                 ; 9A98  aa
    LDA $9ADE,X                         ; 9A99  bd de 9a
    STA tmp_28                          ; 9A9C  85 28
    LDA $9ADF,X                         ; 9A9E  bd df 9a
    STA tmp_29                          ; 9AA1  85 29
    LDX tmp_18                          ; 9AA3  a6 18
    LDA wram_buf,X                      ; 9AA5  bd 00 60
    CMP #$FF                            ; 9AA8  c9 ff
    BEQ loc_16_9AAF                     ; 9AAA  f0 03
    JSR sub_16_9ABD                     ; 9AAC  20 bd 9a
loc_16_9AAF:
    LDX tmp_18                          ; 9AAF  a6 18
    STA inv_select,X                    ; 9AB1  9d 28 64
    INC tmp_18                          ; 9AB4  e6 18
    LDA tmp_18                          ; 9AB6  a5 18
    CMP #$08                            ; 9AB8  c9 08
    BNE loc_16_9A95                     ; 9ABA  d0 d9
    RTS                                 ; 9ABC  60
sub_16_9ABD:
    STA tmp_22                          ; 9ABD  85 22
    LDA #$08                            ; 9ABF  a9 08
    STA tmp_19                          ; 9AC1  85 19
    LDA tmp_18                          ; 9AC3  a5 18
    CMP #$04                            ; 9AC5  c9 04
    BPL loc_16_9ACD                     ; 9AC7  10 04
    LDA #$04                            ; 9AC9  a9 04
    STA tmp_19                          ; 9ACB  85 19
loc_16_9ACD:
    LDY #$00                            ; 9ACD  a0 00
loc_16_9ACF:
    LDA tmp_22                          ; 9ACF  a5 22
    CMP (tmp_28),Y                      ; 9AD1  d1 28
    BEQ loc_16_9ADC                     ; 9AD3  f0 07
    INY                                 ; 9AD5  c8
    CPY tmp_19                          ; 9AD6  c4 19
    BNE loc_16_9ACF                     ; 9AD8  d0 f5
    LDY #$FF                            ; 9ADA  a0 ff
loc_16_9ADC:
    TYA                                 ; 9ADC  98
    RTS                                 ; 9ADD  60
    .db $30,$64,$34,$64,$38,$64,$3C,$64,$40,$64,$48,$64,$50,$64,$58,$64; 16:9ADE
SaveLoadPage:  ; s43: START in the SELECT menu (16:8149 when $07DD=0); clears $7000/$646A/$646B and prints GAME + slot rows
    LDA #$00                            ; 9AEE  a9 00
    STA menu_save_state                 ; 9AF0  8d 00 70
    STA wram_646A                       ; 9AF3  8d 6a 64
    STA save_slot_sel                   ; 9AF6  8d 6b 64
    JSR sub_16_9D52                     ; 9AF9  20 52 9d
    LDX #$00                            ; 9AFC  a2 00
    JSR sub_16_9C85                     ; 9AFE  20 85 9c
    LDX #$00                            ; 9B01  a2 00
    JSR sub_16_9CAE                     ; 9B03  20 ae 9c
    LDA #$39                            ; 9B06  a9 39
    JSR MenuPrint                       ; 9B08  20 33 89
    LDA #$17                            ; 9B0B  a9 17
    JSR MenuPrint                       ; 9B0D  20 33 89
    LDA #$00                            ; 9B10  a9 00
    STA tmp_19                          ; 9B12  85 19
loc_16_9B14:
    LDX tmp_19                          ; 9B14  a6 19
    LDA $9D8D,X                         ; 9B16  bd 8d 9d
    STA tmp_26                          ; 9B19  85 26
    LDA $9D92,X                         ; 9B1B  bd 92 9d
    STA tmp_27                          ; 9B1E  85 27
    LDA $9D97,X                         ; 9B20  bd 97 9d
    STA tmp_29                          ; 9B23  85 29
    JSR sub_16_88A8                     ; 9B25  20 a8 88
    INC tmp_19                          ; 9B28  e6 19
    LDA tmp_19                          ; 9B2A  a5 19
    CMP #$02                            ; 9B2C  c9 02
    BMI loc_16_9B14                     ; 9B2E  30 e4
    LDA #$14                            ; 9B30  a9 14
    JSR MenuPrint                       ; 9B32  20 33 89
    LDA #$15                            ; 9B35  a9 15
    JSR MenuPrint                       ; 9B37  20 33 89
SaveLoadLoop:  ; s43: per frame: SELECT/START exit; L/R toggle $646A b0 (save/load); B exit ($7000=$AB); A -> MenuSave or Load prompt
    LDA #$00                            ; 9B3A  a9 00
    STA menu_save_state                 ; 9B3C  8d 00 70
    JSR WaitFrame                       ; 9B3F  20 69 c1
    JSR ReadPadsMenu                    ; 9B42  20 e0 fe
    LDA pad_new                         ; 9B45  a5 4b
    AND #$30                            ; 9B47  29 30
    BEQ loc_16_9B4E                     ; 9B49  f0 03
    JMP SaveLoadExit                    ; 9B4B  4c 7a 9c
loc_16_9B4E:
    LDA pad_new                         ; 9B4E  a5 4b
    AND #$03                            ; 9B50  29 03
    BEQ loc_16_9B63                     ; 9B52  f0 0f
    INC wram_646A                       ; 9B54  ee 6a 64
    LDA wram_646A                       ; 9B57  ad 6a 64
    AND #$01                            ; 9B5A  29 01
    TAX                                 ; 9B5C  aa
    JSR sub_16_9C85                     ; 9B5D  20 85 9c
    JMP SaveLoadLoop                    ; 9B60  4c 3a 9b
loc_16_9B63:
    LDA pad_new                         ; 9B63  a5 4b
    AND #$0C                            ; 9B65  29 0c
    BEQ loc_16_9B86                     ; 9B67  f0 1d
    LDY #$3A                            ; 9B69  a0 3a
    LDA save_slot_sel                   ; 9B6B  ad 6b 64
    LSR A                               ; 9B6E  4a
    BCC loc_16_9B73                     ; 9B6F  90 02
    LDY #$39                            ; 9B71  a0 39
loc_16_9B73:
    TYA                                 ; 9B73  98
    JSR MenuPrint                       ; 9B74  20 33 89
    INC save_slot_sel                   ; 9B77  ee 6b 64
    LDA save_slot_sel                   ; 9B7A  ad 6b 64
    AND #$01                            ; 9B7D  29 01
    TAX                                 ; 9B7F  aa
    JSR sub_16_9CAE                     ; 9B80  20 ae 9c
    JMP SaveLoadLoop                    ; 9B83  4c 3a 9b
loc_16_9B86:
    LDA pad_new                         ; 9B86  a5 4b
    AND #$40                            ; 9B88  29 40
    BEQ loc_16_9B94                     ; 9B8A  f0 08
    LDA #$AB                            ; 9B8C  a9 ab
    STA menu_save_state                 ; 9B8E  8d 00 70
    JMP SaveLoadExit                    ; 9B91  4c 7a 9c
loc_16_9B94:
    LDA pad_new                         ; 9B94  a5 4b
    AND #$80                            ; 9B96  29 80
    BEQ SaveLoadLoop                    ; 9B98  f0 a0
    LDA wram_646A                       ; 9B9A  ad 6a 64
    AND #$01                            ; 9B9D  29 01
    BEQ MenuSave                        ; 9B9F  f0 40
    LDA #$36                            ; 9BA1  a9 36
    JSR MenuPrint                       ; 9BA3  20 33 89
    LDA #$0E                            ; 9BA6  a9 0e
    JSR MenuPrint                       ; 9BA8  20 33 89
    LDA #$A1                            ; 9BAB  a9 a1
    JSR sub_16_84DA                     ; 9BAD  20 da 84
    BCC loc_16_9BBA                     ; 9BB0  90 08
    LDA #$0F                            ; 9BB2  a9 0f
    JSR MenuPrint                       ; 9BB4  20 33 89
    JMP SaveLoadLoop                    ; 9BB7  4c 3a 9b
loc_16_9BBA:
    LDA save_slot_sel                   ; 9BBA  ad 6b 64
    AND #$01                            ; 9BBD  29 01
    BNE loc_16_9BD1                     ; 9BBF  d0 10
    LDA #$00                            ; 9BC1  a9 00
    JSR SaveSlotCopy                    ; 9BC3  20 e0 9c
    JSR sub_23_BC03                     ; 9BC6  20 03 bc
    LDA #$FA                            ; 9BC9  a9 fa
    STA menu_save_state                 ; 9BCB  8d 00 70
    JMP SaveLoadExit                    ; 9BCE  4c 7a 9c
loc_16_9BD1:
    LDA #$02                            ; 9BD1  a9 02
    JSR SaveSlotCopy                    ; 9BD3  20 e0 9c
    JSR sub_23_BC03                     ; 9BD6  20 03 bc
    LDA #$FA                            ; 9BD9  a9 fa
    STA menu_save_state                 ; 9BDB  8d 00 70
    JMP SaveLoadExit                    ; 9BDE  4c 7a 9c
MenuSave:  ; menu Save command: checks AreaSaveFlags bit0 and $0710; slot by $646B bit0
    LDA #$FB                            ; 9BE1  a9 fb
    STA menu_save_state                 ; 9BE3  8d 00 70
    LDX area_id                         ; 9BE6  a6 6c
    LDA AreaSaveFlags,X                 ; 9BE8  bd 00 bf
    LSR A                               ; 9BEB  4a
    BCS loc_16_9C12                     ; 9BEC  b0 24
    LDA #$4E                            ; 9BEE  a9 4e
    JSR sub_16_8A9F                     ; 9BF0  20 9f 8a
    LDA #$0F                            ; 9BF3  a9 0f
    JSR MenuPrint                       ; 9BF5  20 33 89
    LDA #$16                            ; 9BF8  a9 16
    JSR MenuPrint                       ; 9BFA  20 33 89
loc_16_9BFD:
    LDX #$30                            ; 9BFD  a2 30
loc_16_9BFF:
    LSR frame_ready                     ; 9BFF  46 09
    BCC loc_16_9BFF                     ; 9C01  90 fc
    DEX                                 ; 9C03  ca
    BNE loc_16_9BFF                     ; 9C04  d0 f9
    LDA #$00                            ; 9C06  a9 00
    STA pad_new                         ; 9C08  85 4b
    LDA #$AB                            ; 9C0A  a9 ab
    STA menu_save_state                 ; 9C0C  8d 00 70
    JMP SaveLoadExit                    ; 9C0F  4c 7a 9c
loc_16_9C12:
    LDA obj1_flash                      ; 9C12  ad 10 07
    AND #$0F                            ; 9C15  29 0f
    BEQ loc_16_9C2B                     ; 9C17  f0 12
    LDA #$4E                            ; 9C19  a9 4e
    JSR sub_16_8A9F                     ; 9C1B  20 9f 8a
    LDA #$0F                            ; 9C1E  a9 0f
    JSR MenuPrint                       ; 9C20  20 33 89
    LDA #$38                            ; 9C23  a9 38
    JSR MenuPrint                       ; 9C25  20 33 89
    JMP loc_16_9BFD                     ; 9C28  4c fd 9b
loc_16_9C2B:
    LDA #$37                            ; 9C2B  a9 37
    JSR MenuPrint                       ; 9C2D  20 33 89
    LDA #$0E                            ; 9C30  a9 0e
    JSR MenuPrint                       ; 9C32  20 33 89
    LDA #$A1                            ; 9C35  a9 a1
    JSR sub_16_84DA                     ; 9C37  20 da 84
    BCC loc_16_9C44                     ; 9C3A  90 08
    LDA #$0F                            ; 9C3C  a9 0f
    JSR MenuPrint                       ; 9C3E  20 33 89
    JMP SaveLoadLoop                    ; 9C41  4c 3a 9b
loc_16_9C44:
    LDA save_slot_sel                   ; 9C44  ad 6b 64
    AND #$01                            ; 9C47  29 01
    BNE loc_16_9C64                     ; 9C49  d0 19
    JSR SaveJumpTable                   ; 9C4B  20 00 bc
    LDA #$04                            ; 9C4E  a9 04
    JSR SaveSlotCopy                    ; 9C50  20 e0 9c
    LDA #$05                            ; 9C53  a9 05
    JSR SaveSlotCopy                    ; 9C55  20 e0 9c
    LDA save_checksum                   ; 9C58  ad f5 70
    STA save_csum0_a                    ; 9C5B  8d f0 70
    STA save_csum0_b                    ; 9C5E  8d f1 70
    JMP SaveLoadExit                    ; 9C61  4c 7a 9c
loc_16_9C64:
    JSR SaveJumpTable                   ; 9C64  20 00 bc
    LDA #$06                            ; 9C67  a9 06
    JSR SaveSlotCopy                    ; 9C69  20 e0 9c
    LDA #$07                            ; 9C6C  a9 07
    JSR SaveSlotCopy                    ; 9C6E  20 e0 9c
    LDA save_checksum                   ; 9C71  ad f5 70
    STA save_csum1_a                    ; 9C74  8d f2 70
    STA save_csum1_b                    ; 9C77  8d f3 70
SaveLoadExit:  ; s43: back to the SELECT menu (JMP 16:807E if $6424 else 16:807A)
    LDA wram_6424                       ; 9C7A  ad 24 64
    BEQ loc_16_9C82                     ; 9C7D  f0 03
    JMP loc_16_807E                     ; 9C7F  4c 7e 80
loc_16_9C82:
    JMP loc_16_807A                     ; 9C82  4c 7a 80
sub_16_9C85:
    LDA $9D9C,X                         ; 9C85  bd 9c 9d
    STA wram_60FB                       ; 9C88  8d fb 60
    LDA $9D9E,X                         ; 9C8B  bd 9e 9d
    STA wram_60F8                       ; 9C8E  8d f8 60
    LDA $9DA2,X                         ; 9C91  bd a2 9d
    STA wram_60F9                       ; 9C94  8d f9 60
    LDA $9DA0,X                         ; 9C97  bd a0 9d
    STA wram_60FA                       ; 9C9A  8d fa 60
    LDA #$03                            ; 9C9D  a9 03
    STA a:tmp_12                        ; 9C9F  8d 12 00
    LDA #$39                            ; 9CA2  a9 39
    STA tmp_2E                          ; 9CA4  85 2e
    JSR MenuSprDraw                     ; 9CA6  20 d3 86
    LDA #$4B                            ; 9CA9  a9 4b
    JMP sub_16_8A9F                     ; 9CAB  4c 9f 8a
sub_16_9CAE:
    LDA save_slot_sel                   ; 9CAE  ad 6b 64
    AND #$01                            ; 9CB1  29 01
    TAX                                 ; 9CB3  aa
    LDA $9CDE,X                         ; 9CB4  bd de 9c
    STA $0280                           ; 9CB7  8d 80 02
    STA $027C                           ; 9CBA  8d 7c 02
    LDA #$FE                            ; 9CBD  a9 fe
    STA $0281                           ; 9CBF  8d 81 02
    STA $027D                           ; 9CC2  8d 7d 02
    LDA #$00                            ; 9CC5  a9 00
    STA $0282                           ; 9CC7  8d 82 02
    LDA #$40                            ; 9CCA  a9 40
    STA $027E                           ; 9CCC  8d 7e 02
    LDA #$30                            ; 9CCF  a9 30
    STA $0283                           ; 9CD1  8d 83 02
    LDA #$D0                            ; 9CD4  a9 d0
    STA $027F                           ; 9CD6  8d 7f 02
    LDA #$4B                            ; 9CD9  a9 4b
    JMP sub_16_8A9F                     ; 9CDB  4c 9f 8a
    .db $57,$6F                         ; 16:9CDE
SaveSlotCopy:  ; A=descriptor 16:9D32: copy $300 bytes between slot image and $7D00
    TAY                                 ; 9CE0  a8
    LDX #$00                            ; 9CE1  a2 00
loc_16_9CE3:
    LDA jmp_trampoline,X                ; 9CE3  b5 10
    STA $60C0,X                         ; 9CE5  9d c0 60
    INX                                 ; 9CE8  e8
    CPX #$06                            ; 9CE9  e0 06
    BNE loc_16_9CE3                     ; 9CEB  d0 f6
    TYA                                 ; 9CED  98
    ASL A                               ; 9CEE  0a
    ASL A                               ; 9CEF  0a
    TAX                                 ; 9CF0  aa
    LDY #$00                            ; 9CF1  a0 00
loc_16_9CF3:
    LDA SaveSlotDescs,X                 ; 9CF3  bd 32 9d
    STA a:jmp_trampoline,Y              ; 9CF6  99 10 00
    INX                                 ; 9CF9  e8
    INY                                 ; 9CFA  c8
    CPY #$04                            ; 9CFB  c0 04
    BNE loc_16_9CF3                     ; 9CFD  d0 f4
    LDA #$FF                            ; 9CFF  a9 ff
    STA tmp_14                          ; 9D01  85 14
    LDA #$02                            ; 9D03  a9 02
    STA tmp_15                          ; 9D05  85 15
    LDY #$00                            ; 9D07  a0 00
loc_16_9D09:
    LDA (jmp_trampoline),Y              ; 9D09  b1 10
    STA (tmp_12),Y                      ; 9D0B  91 12
    INC jmp_trampoline                  ; 9D0D  e6 10
    BNE loc_16_9D13                     ; 9D0F  d0 02
    INC tmp_11                          ; 9D11  e6 11
loc_16_9D13:
    INC tmp_12                          ; 9D13  e6 12
    BNE loc_16_9D19                     ; 9D15  d0 02
    INC tmp_13                          ; 9D17  e6 13
loc_16_9D19:
    DEC tmp_14                          ; 9D19  c6 14
    BNE loc_16_9D09                     ; 9D1B  d0 ec
    DEC tmp_15                          ; 9D1D  c6 15
    LDA tmp_15                          ; 9D1F  a5 15
    CMP #$FF                            ; 9D21  c9 ff
    BNE loc_16_9D09                     ; 9D23  d0 e4
    LDX #$00                            ; 9D25  a2 00
loc_16_9D27:
    LDA $60C0,X                         ; 9D27  bd c0 60
    STA jmp_trampoline,X                ; 9D2A  95 10
    INX                                 ; 9D2C  e8
    CPX #$06                            ; 9D2D  e0 06
    BNE loc_16_9D27                     ; 9D2F  d0 f6
    RTS                                 ; 9D31  60
SaveSlotDescs:  ; 8 x (src dst): $7100/$7400/$7700/$7A00 <-> $7D00
    .db $00,$71,$00,$7D,$00,$74,$00,$7D,$00,$77,$00,$7D,$00,$7A,$00,$7D; 16:9D32
    .db $00,$7D,$00,$71,$00,$7D,$00,$74,$00,$7D,$00,$77,$00,$7D,$00,$7A; 16:9D42
sub_16_9D52:
    JSR sub_16_8605                     ; 9D52  20 05 86
    LDA #$0F                            ; 9D55  a9 0f
    JSR MenuPrint                       ; 9D57  20 33 89
    LDA #$13                            ; 9D5A  a9 13
    JSR MenuPrint                       ; 9D5C  20 33 89
    LDA #$35                            ; 9D5F  a9 35
    JSR MenuPrint                       ; 9D61  20 33 89
    JSR WaitFrame                       ; 9D64  20 69 c1
    LDA #$3C                            ; 9D67  a9 3c
    STA chr_r0_alt                      ; 9D69  85 58
    LDA #$38                            ; 9D6B  a9 38
    STA chr_r1_alt                      ; 9D6D  85 59
    LDA #$28                            ; 9D6F  a9 28
    STA tmp_21                          ; 9D71  85 21
    LDA #$C0                            ; 9D73  a9 c0
    STA tmp_20                          ; 9D75  85 20
    LDA #$00                            ; 9D77  a9 00
    STA tmp_25                          ; 9D79  85 25
    LDA #$40                            ; 9D7B  a9 40
    STA tmp_24                          ; 9D7D  85 24
    LDA #$06                            ; 9D7F  a9 06
    STA tmp_19                          ; 9D81  85 19
loc_16_9D83:
    LDX #$09                            ; 9D83  a2 09
    JSR sub_16_8864                     ; 9D85  20 64 88
    DEC tmp_19                          ; 9D88  c6 19
    BNE loc_16_9D83                     ; 9D8A  d0 f7
    RTS                                 ; 9D8C  60
    .db $07,$0A,$07,$07,$07,$06,$06,$09,$0C,$0F,$E8,$E8,$FF,$FF,$FF,$38; 16:9D8D
    .db $50,$30,$30,$00,$01,$75,$7E     ; 16:9D9D
ShopArmorGoods:  ; s71: 11 x 4 goods ids ($FF empty)
    .db $15,$0D,$FF,$FF,$16,$0D,$0E,$FF,$00,$01,$02,$03,$18,$0F,$10,$12; 16:9DA4
    .db $05,$1C,$14,$FF,$17,$18,$0F,$FF,$FF,$FF,$FF,$FF,$19,$1A,$11,$13; 16:9DB4
    .db $FF,$FF,$FF,$FF,$1A,$12,$13,$FF,$FF,$FF,$FF,$FF; 16:9DC4
ShopArmorPrices:  ; s71: 11 x 4 u16 prices
    .db $64,$00,$50,$00,$00,$00,$00,$00,$8C,$00,$46,$00,$DC,$00,$00,$00; 16:9DD0
    .db $00,$00,$00,$00,$00,$00,$00,$00,$08,$07,$14,$05,$D0,$07,$28,$23; 16:9DE0
    .db $00,$00,$00,$00,$00,$00,$00,$00,$58,$02,$D0,$07,$DC,$05,$00,$00; 16:9DF0
    .db $00,$00,$00,$00,$00,$00,$00,$00,$B8,$0B,$64,$19,$C4,$09,$70,$17; 16:9E00
    .db $00,$00,$00,$00,$00,$00,$00,$00,$7C,$15,$70,$17,$88,$13,$00,$00; 16:9E10
    .db $00,$00,$00,$00,$00,$00,$00,$00 ; 16:9E20
ShopToolGoods:  ; s71: 11 x 4 goods ids
    .db $1D,$1E,$24,$31,$1D,$1E,$24,$FF,$1D,$1E,$21,$FF,$24,$1F,$21,$FF; 16:9E28
    .db $1D,$1E,$21,$24,$1D,$24,$1F,$20,$1D,$1E,$21,$31,$1D,$1E,$24,$21; 16:9E38
    .db $1D,$1E,$1F,$24,$1D,$1E,$20,$22,$1E,$22,$23,$24; 16:9E48
ShopToolPrices:  ; s71: 11 x 4 u16 prices (shop 0 = 30 40 60 50
    .db $1E,$00,$28,$00,$3C,$00,$32,$00,$23,$00,$2D,$00,$41,$00,$00,$00; 16:9E54
    .db $32,$00,$3C,$00,$50,$00,$00,$00,$64,$00,$96,$00,$96,$00,$00,$00; 16:9E64
    .db $3C,$00,$46,$00,$64,$00,$50,$00,$5A,$00,$78,$00,$C8,$00,$B4,$00; 16:9E74
    .db $78,$00,$96,$00,$B4,$00,$2C,$01,$B4,$00,$C8,$00,$2C,$01,$5E,$01; 16:9E84
    .db $F4,$01,$58,$02,$BC,$02,$20,$03,$B4,$00,$C8,$00,$2C,$01,$20,$03; 16:9E94
    .db $E8,$03,$A0,$0F,$B8,$0B,$DC,$05,$10,$00,$14,$00,$28,$00,$50,$00; 16:9EA4
    .db $32,$00,$64,$00,$78,$00,$96,$00,$2C,$01,$96,$00,$F4,$01,$00,$00; 16:9EB4
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 16:9EC4
    .db $00,$00,$00,$00,$00,$00,$00,$00,$28,$00,$6E,$00,$EE,$02,$EE,$02; 16:9ED4
    .db $E2,$04,$B8,$0B,$B8,$0B,$00,$00,$32,$00,$46,$00,$2C,$01,$E8,$03; 16:9EE4
    .db $DC,$05,$B2,$0C,$00,$00,$00,$00,$0A,$00,$14,$00,$5F,$00,$5A,$00; 16:9EF4
    .db $41,$00,$F4,$01,$E8,$03,$1E,$00,$00,$00,$2C,$01,$00,$00,$00,$00; 16:9F04
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 16:9F14
    .db $19,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 16:9F24
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 16:9F34
    .db $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00; 16:9F44
ShopAreaTable:  ; s71: 33 area ids that have a shop
    .db $C4,$C3,$C2,$C5,$C9,$CB,$C8,$C7,$CF,$D0,$D3,$D2,$D1,$3D,$3C,$DA; 16:9F54
    .db $DD,$DC,$D9,$E5,$E7,$EE,$EB,$ED,$F0,$BC,$BE,$F5,$F6,$F7,$FA,$F9; 16:9F64
    .db $F8                             ; 16:9F74
ShopAreaIds:  ; s71: 33 shop ids (0-10) parallel to ShopAreaTable
    .db $00,$00,$00,$00,$01,$01,$01,$01,$02,$02,$03,$03,$03,$04,$04,$05; 16:9F75
    .db $05,$05,$05,$06,$06,$07,$07,$07,$07,$08,$08,$09,$09,$09,$0A,$0A; 16:9F85
    .db $0A,$08,$0D,$12,$17,$0F,$FC,$36,$30,$29,$18,$26,$A3,$10,$35,$42; 16:9F95
    .db $18,$38,$10,$34,$1D,$31,$96,$12,$12,$0F,$FF; 16:9FA5
StaleSnd16_9FB0:  ; dead filler to 9FFF: copy of 24:9FB0 sound data
    .db $62,$2B,$34,$2D,$64,$42,$64,$22,$2E,$66,$2D,$A2,$02,$66,$34,$21; 16:9FB0
    .db $61,$41,$21,$21,$21,$21,$21,$66,$22,$61,$41,$22,$22,$22,$22,$22; 16:9FC0
    .db $A8,$0C,$93,$8C,$E6,$F1,$60,$32,$2F,$2F,$2F,$2F,$20,$20,$20,$20; 16:9FD0
    .db $24,$24,$24,$24,$2F,$2F,$2F,$2F,$66,$77,$77,$60,$32,$25,$25,$62; 16:9FE0
    .db $42,$60,$33,$2C,$2C,$62,$42,$60,$2F,$2F,$62,$42,$60,$25,$25,$62; 16:9FF0
