"""Standard controllers on $4016/$4017 (Ph5.5 emu 3/6).

Write $4016 bit 0 = strobe: while 1 the shift registers reload continuously (reads return A);
on 1->0 the current buttons are latched. Each read returns bit 0 = next button in the order
A, B, Select, Start, Up, Down, Left, Right, then 1s (official pads). The bus ORs the open-bus
upper bits.
"""

BUTTONS = ('A', 'B', 'SELECT', 'START', 'UP', 'DOWN', 'LEFT', 'RIGHT')
BIT = {n: 1 << i for i, n in enumerate(BUTTONS)}


def parse_buttons(s):
    """'A+RIGHT' -> bitmask (case-insensitive; '' / 'NONE' -> 0)."""
    m = 0
    for part in s.replace(',', '+').split('+'):
        part = part.strip().upper()
        if part and part != 'NONE':
            m |= BIT[part]
    return m


class Pads:
    def __init__(self):
        self.buttons = [0, 0]
        self.shift = [0, 0]
        self.strobe = 0

    def set(self, port, mask):
        self.buttons[port] = mask & 0xFF

    def write_strobe(self, v):
        self.strobe = v & 1
        if self.strobe:
            self.shift = list(self.buttons)

    def read(self, port):
        if self.strobe:
            return self.buttons[port] & 1
        s = self.shift[port]
        self.shift[port] = (s >> 1) | 0x80
        return s & 1

    def state(self):
        return {'buttons': list(self.buttons), 'shift': list(self.shift), 'strobe': self.strobe}

    def load_state(self, d):
        self.buttons = list(d['buttons'])
        self.shift = list(d['shift'])
        self.strobe = d['strobe']
