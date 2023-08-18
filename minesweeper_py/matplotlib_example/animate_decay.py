"""
=====
Decay
=====

This example showcases:

- using a generator to drive an animation,
- changing axes limits during an animation.

Output generated via `matplotlib.animation.Animation.to_jshtml`.
"""
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation
from datetime import datetime

loss_list = []

fig, ax = plt.subplots()
line, = ax.plot([], [], lw=2)
ax.grid()

def init():
    ax.set_ylim(0, 10)
    ax.set_xlim(0, 1)
    line.set_data([], [])
    return line,

def update_plot(data):
    xmin, xmax = ax.get_xlim()
    # 更新x轴范围
    if len(loss_list) >= xmax:
        ax.set_xlim(xmin, 2*xmax)
        ax.figure.canvas.draw()
    line.set_data(range(len(loss_list)), loss_list)

    return line,

def simulate_data_gen():
    loss_list.append(datetime.now().microsecond % 10)

# Only save last 100 frames, but run forever
ani = animation.FuncAnimation(fig, update_plot, interval=100, init_func=init, cache_frame_data=False)

timer = fig.canvas.new_timer(interval=30)
timer.add_callback(simulate_data_gen)
timer.start()



plt.show()
