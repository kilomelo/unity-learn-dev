import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation

loss_values = []
accuracy_values = []
predict_acc_values = []

fig, ax1 = plt.subplots()

color = 'tab:red'
ax1.set_xlabel('epochs')
ax1.set_ylabel('loss', color=color)
ax1.set_ylim(0, 1)
ax1.set_xlim(0, 10)
line_loss, = ax1.plot(loss_values, color=color, label='loss')
ax1.tick_params(axis='y', labelcolor=color)

# ax2 = ax1.twinx()  # instantiate a second axes that shares the same x-axis
# color = 'tab:blue'
# ax2.set_ylabel('acc', color=color)  # we already handled the x-label with ax1
# ax2.set_ylim(0, 1)
# line_running_acc = ax2.plot(accuracy_values, color=color, label='running acc')
# line_predict_acc = ax2.plot(predict_acc_values, color='tab:orange', label='predict acc')
# ax2.tick_params(axis='y', labelcolor=color)

fig.tight_layout()  # otherwise the right y-label is slightly clipped
plt.title('accuracy and loss')
plt.legend(loc="center right")
%matplotlib qt

# def init():
#     ax1.set_ylim(-1.1, 1.1)
#     ax1.set_xlim(0, 1)
#     del xdata[:]
#     del ydata[:]
#     line_loss.set_data(xdata, ydata)
#     return line_loss,

def update_plot(data):
    epoch_cnt = len(loss_values)
    xmin, xmax = ax.get_xlim()
    # 更新x轴范围
    if epoch_cnt >= xmax:
        ax1.set_xlim(xmin, 2*xmax)
        ax1.figure.canvas.draw()
    line_loss.set_data(range(epoch_cnt), loss_values)
    return line_loss,
# create animation using the animate() function
ani = animation.FuncAnimation(fig, update_plot, interval=100, cache_frame_data=False)
plt.show()
