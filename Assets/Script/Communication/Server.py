import string
import cherrypy
import json
import numpy as np
import tensorflow as tf
import time

class Network():
	def __init__(self):
		self.num_actions = 4
		#4 adjacent blocks + 4 reward point directions
		self.num_inputs = 4 + 4
		self.batchsize = 16
		self.discount_rate = 0.75
		self.lr = 0.01
		self.gamma = 0.99
		self.optimizer = tf.optimizers.Adam(self.lr)
		
		self.experience = {'s': [], 'a': [], 'r': [], 'ns': [], 'done': []}
		self.counter = 0

		self.policyNetwork = self.ModelTemplate()
		# self.policyNetwork.compile(optimizer=self.optimizer,
		# 		loss=['mae'],
		# 		metrics=['mae'])

		self.targetNetwork = self.ModelTemplate()
		self.copyNN()
		print(self.policyNetwork.summary())

	def ModelTemplate(self):
			model = tf.keras.Sequential([
							tf.keras.layers.Flatten(),
							# tf.keras.layers.Dense(64, activation='relu'),
							# tf.keras.layers.Dense(64, activation='relu'),
							tf.keras.layers.Dense(128, activation='tanh', kernel_initializer='RandomNormal'),
							tf.keras.layers.Dense(self.num_actions, activation='softmax', kernel_initializer='RandomNormal')
						])
			model.build(input_shape=(1,self.num_inputs))			
			return model

	def fit(self, exp):
		self.counter += 1
		for key, value in exp.items():
			self.experience[key].append(value)
		
		if(self.counter==self.batchsize):
			states = tf.convert_to_tensor(self.experience['s'], dtype=tf.float32) #The current state
			# print('states', states.shape, states)
			actions = np.asarray(self.experience['a']) #The action performed (chosen randomly or by net)
			# print('actions', actions.shape, actions)
			rewards = np.asarray(self.experience['r']) #The reward got
			# print('rewards', rewards.shape, rewards)
			next_states = tf.convert_to_tensor(self.experience['ns']) #The next state
			# print('next_states', next_states.shape, next_states)
			dones = np.asarray(self.experience['done']) #State of the game (ended or not)
			# print('dones', dones.shape, dones)
			value_next = np.max(self.targetNetwork(next_states),axis=1) #Max next values according to the Target Network
			# print('value_next', value_next.shape, value_next)
			actual_values = np.where(dones, rewards, rewards+self.gamma*value_next)
			# print('actual_values', actual_values.shape, actual_values)

			with tf.GradientTape() as tape:
				tape.watch(states)
				
				a = self.policyNetwork(states) * tf.one_hot(actions, self.num_actions)
				selected_action_values = tf.math.reduce_sum(a , axis=1)

				loss = tf.math.reduce_mean(tf.square(actual_values - selected_action_values))
				tape.watch(loss)


			variables = self.policyNetwork.trainable_variables
			gradients = tape.gradient(loss, variables)
			print('\n#####\tloss\t#####', loss)
			# print('variables', type(variables), np.array(variables).shape)
			# print('wv', tape.watched_variables())
			# print('gradients', type(gradients), np.array(gradients).shape)
			# print(gradients)
			# time.sleep(3)

			self.optimizer.apply_gradients(zip(gradients, variables))

			self.experience = {'s': [], 'a': [], 'r': [], 'ns': [], 'done': []}
			self.counter=0
			return loss		

		if(self.counter>=self.batchsize):
			self.experience = {'s': [], 'a': [], 'r': [], 'ns': [], 'done': []}
			self.counter=0

	def predictPN(self, input):
		return self.policyNetwork(np.atleast_2d(input))

	def predictTN(self, input):
		return self.targetNetwork(np.atleast_2d(input))
	
	def copyNN(self):
		self.targetNetwork.set_weights(self.policyNetwork.get_weights())




class CalculatorWebService(object):
	#Required to be accessable online
	exposed=True

	def __init__(self):
		self.net = Network()

	def FIT(self):
		msg = cherrypy.request.body.readline().decode("utf-8")
		exp = self.decodeMsg(msg)
		self.net.fit(exp)

	def PREDICT_PN(self):
		msg = cherrypy.request.body.readline().decode("utf-8")
		input = np.array(msg.split('.')).astype(int)
		# print('PREDICT')
		# print(input)
		output = self.net.predictPN(input)
		# print('pred net', output)
		# print('target net', self.net.predictTN(input))
		out = '/'.join([str(o) for o in output.numpy()[0]])
		return out

	def PREDICT_TN(self):
		output = self.net.predictTN(values[0])
		out = '.'.join([str(o) for o in output])
		#print('tn', out, np.shape(out))
		return out

	def COPY_NN(self):
		self.net.copyNN()

	def decodeMsg(self, msg):
		values = msg.split(',')

		currentstate = tf.Variable([np.array(values[0].split('.'), dtype=np.int8)])
		action = int(values[1])
		nextstate = tf.Variable([np.array(values[2].split('.'), dtype=np.int8)])
		reward = float(values[3])
		endstate = True if values[4]=='True' else False

		exp = {'s': currentstate, 'a': action, 'r': reward, 'ns': nextstate, 'done': endstate}
		return exp

	def RESET(self):
		self.net = Network()
		print('Reset')

#'request.dispatch': cherrypy.dispatch.MethodDispatcher() => switch from default URL to HTTP compliant approch
conf = { '/': {	'request.dispatch': cherrypy.dispatch.MethodDispatcher(),
								'tools.sessions.on':True} 
					}
cherrypy.tree.mount(CalculatorWebService(), '/', conf)

cherrypy.config.update({'servet.socket_host':'0.0.0.0'})
cherrypy.config.update({'servet.socket_port':'8080'})

cherrypy.engine.start()
cherrypy.engine.block()
